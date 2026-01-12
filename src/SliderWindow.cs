using System.Linq;
using UnityEngine;

public class SliderWindow : MonoBehaviour
{
    public Rect mainWindowRect = new Rect(100,100,500,400);
    private Rect sliderRect = new Rect(0,0,300,400);
    private bool isVisible = false; // start hidden
    private float slideSpeed =12f;

    // Konfiguracja
    public float minDistanceFromEdge =20f;
    public bool allowDragging = false; // sztywno przylaczony, bez dragowania
    public float buttonOffset =0f; // opcjonalny offset od srodka (Y)

    private float buttonWidth =20f; // szerokosc paska (stala, z tekstury)
    private float buttonHeight =20f; // wysokosc paska (TERAZ stala 300)
    private Texture2D alarmBarTexture;
    private float barFixedWidth = 18f;

    // Animacja (0 = hidden,1 = visible)
    private float slideT =0f;
    private const float SNAP_EPSILON =0.001f; // precyzyjny snap, brak plywania

    private float hiddenPanelX;
    private float visiblePanelX;

    private Vector2 screenSize;
    private KerbNote mainWindow;
    private bool isModActive = false;

    private int panelWindowID; // wlasny ID okna panelu
    private int buttonWindowID; // wlasny ID okna przycisku Alarm_bar

    private const string TEXTURE_ALARM_BAR = "KerbNoteLite/Textures/Alarm_bar";

    // --- Alarm Selector embed ---
    private AlarmSelector selector;

    // Click-through prevention
    private const string INPUT_LOCK_ID = "KerbNote_AlarmBar_ClickBlock";
    private bool inputLockSet = false;
    private Rect lastPanelWinRect;
    private Rect lastButtonWinRect;

    // Cache for checking MiniNote visibility - avoids expensive FindObjectsOfType every frame
    private static bool anyMiniNoteVisible = false;
    public static void NotifyMiniNoteVisibilityChanged(bool visible)
    {
        anyMiniNoteVisible = visible;
    }

    // Cache GUIStyle for alarm bar button to avoid recreation every frame
    private GUIStyle cachedAlarmButtonStyle;

    void Start()
    {
        // Spróbuj znalezc glówne okno, jesli nie zostalo ustawione z zewnatrz
        if (mainWindow == null)
            mainWindow = FindObjectOfType<KerbNote>();

        if (mainWindow == null)
        {
            Debug.LogError("[SliderWindow] Nie znaleziono glównego okna KerbNote!");
            enabled = false;
            return;
        }

        mainWindowRect = mainWindow.WindowRect;
        screenSize = new Vector2(Screen.width, Screen.height);

        // Ladowanie tekstur (z aktywnego skina)
        KerbalUIBackground.LoadTexture();
        ReloadAlarmBarTexture();
        if (alarmBarTexture == null)
        {
            Debug.LogWarning("[SliderWindow] Alarm_bar texture not found, using fallback sizes.");
        }
        // Ustal stala szerokosc paska (nie skalujemy w poziomie)
        if (alarmBarTexture != null && alarmBarTexture.width > 0)
        {
            barFixedWidth = Mathf.Clamp(alarmBarTexture.width, 8f, 64f);
        }
        else
        {
            barFixedWidth = 18f;
        }

        // Stala wysokosc paska: 300 px
        buttonHeight = 300f;

        // Start hidden, bez rysowania panelu
        isVisible = false;
        slideT =0f;

        // Zmniejsz szerokosc panelu o25%
        sliderRect.width = Mathf.Round(sliderRect.width *0.75f);

        panelWindowID = GetInstanceID() ^0x5A5A5A5A; // stabilny, unikalny ID okna panelu
        buttonWindowID = GetInstanceID() ^0x3C3C3C3C; // stabilny, unikalny ID okna przycisku

        // Utwórz i skonfiguruj selektor
        selector = gameObject.AddComponent<AlarmSelector>();
        selector.Init(mainWindow, /*tabIndex*/ mainWindow.ActiveTabIndex);
        selector.OnRequestClosePanel = () => { isVisible = false; };

        UpdateSizesAndAnchors();
        UpdateAnchoredPosition();
    }

    public void ReloadAlarmBarTexture()
    {
        alarmBarTexture = SkinAssets.Get("Alarm_bar");
        if (alarmBarTexture == null)
        {
            // fallback to lower-case or default pack URL (handled in SkinAssets)
            alarmBarTexture = SkinAssets.Get("alarm_bar") ?? GameDatabase.Instance.GetTexture(TEXTURE_ALARM_BAR, false);
        }
    }

    void Update()
    {
        if (!enabled) return;

        if (mainWindow == null)
        {
            enabled = false;
            return;
        }

        // Zaczytuj aktualny rect okna glównego i jego widocznosc
        mainWindowRect = mainWindow.WindowRect;
        isModActive = mainWindow.IsWindowVisible;

        if (!isModActive)
        {
            isVisible = false;
            slideT =0f; // wymus pelne schowanie gdy mod nieaktywny
            UpdateSizesAndAnchors();
            UpdateAnchoredPosition();
            ClearInputLock();
            return;
        }

        // Aktualizuj rozmiary zalezne od okna i zakotwicz panel na tej samej wysokosci co okno
        UpdateSizesAndAnchors();

        // Reakcja na zmiane rozdzielczosci (jesli potrzeba dodatkowej logiki)
        if (screenSize.x != Screen.width || screenSize.y != Screen.height)
        {
            screenSize = new Vector2(Screen.width, Screen.height);
        }

        // Animacja (0..1) z domknieciem (snap) dla braku plywania
        float targetT = isVisible ?1f :0f;
        float nextT = Mathf.Lerp(slideT, targetT, Time.deltaTime * slideSpeed);
        if (Mathf.Abs(nextT - targetT) <0.0005f)
        {
            nextT = targetT;
        }
        slideT = nextT;

        UpdateAnchoredPosition();
    }

    private void UpdateSizesAndAnchors()
    {
        // Panel wysokosc nadal zalezna od okna, ale przycisk (pasek) ma stala wysokosc 350
        float reducedHeight = Mathf.Max(0f, mainWindowRect.height -35f);
        sliderRect.height = reducedHeight;
        sliderRect.y = mainWindowRect.y + (mainWindowRect.height - reducedHeight) /2f;

        // Szerokosc paska stala (bez skalowania poziomego)
        buttonWidth = barFixedWidth;

        // Pozycje X panelu:
        // Hidden: lewa krawedz panelu równa prawej krawedzi okna minus szerokosc panelu (schowany pod oknem)
        hiddenPanelX = mainWindowRect.xMax - sliderRect.width;
        // Visible: lewa krawedz panelu równa prawej krawedzi okna (caly panel po prawej)
        visiblePanelX = mainWindowRect.xMax;
    }

    private void UpdateAnchoredPosition()
    {
        // Ustaw pozycje X panelu wedlug animacji (0..1)
        sliderRect.x = Mathf.Lerp(hiddenPanelX, visiblePanelX, slideT);
    }

    void OnGUI()
    {
        if (!isModActive) { ClearInputLock(); return; }

        // Ukryj caly Alarm_bar (poziomy i panel) kiedy About/Help jest otwarte
        if (SettingWindow.IsAboutVisible)
        {
            ClearInputLock();
            return;
        }

        // Update sizes ONCE per frame (called from Update already, but needed here for window rect changes)
        // Only update if window rect actually changed
        Rect currentMainRect = mainWindow.WindowRect;
        if (currentMainRect.x != mainWindowRect.x || currentMainRect.y != mainWindowRect.y || 
            currentMainRect.width != mainWindowRect.width || currentMainRect.height != mainWindowRect.height)
        {
            mainWindowRect = currentMainRect;
            UpdateSizesAndAnchors();
            UpdateAnchoredPosition();
        }

        // Oblicz widoczna szerokosc „wyciagnietego" fragmentu panelu poza okno glówne
        float protrusion = Mathf.Clamp(sliderRect.x + sliderRect.width - mainWindowRect.xMax,0f, sliderRect.width);

        // RYSUJ panel jako GUI.Window w obszarze wystajacym po PRAWEJ (ta sama warstwa co okno moda)
        if (protrusion > SNAP_EPSILON)
        {
            Rect panelWinRect = new Rect(mainWindowRect.xMax, sliderRect.y, protrusion, sliderRect.height);
            lastPanelWinRect = panelWinRect;
            GUI.Window(panelWindowID, panelWinRect, DrawPanelWindow, string.Empty, GUIStyle.none);
        }
        else
        {
            lastPanelWinRect = Rect.zero;
        }

        // RYSUJ Alarm_bar jako osobne okno, zawsze widocne (ta sama warstwa co panel/okno)
        float btnX = mainWindowRect.xMax + protrusion; // lewa krawedz przycisku = prawa krawedz okna/panelu
        float btnY = mainWindowRect.y + (mainWindowRect.height - buttonHeight) /2f + buttonOffset;
        Rect btnWinRect = new Rect(btnX, btnY, buttonWidth, buttonHeight);
        lastButtonWinRect = btnWinRect;
        GUI.Window(buttonWindowID, btnWinRect, DrawAlarmBarWindow, string.Empty, GUIStyle.none);

        // Only bring to front if no MiniNote is visible (use cached value instead of FindObjectsOfType!)
        if (!anyMiniNoteVisible)
        {
            GUI.BringWindowToFront(panelWindowID);
            GUI.BringWindowToFront(buttonWindowID);
        }

        // Manage input lock for hover over our windows
        UpdateInputLock();
    }

    private void DrawPanelWindow(int id)
    {
        // Wewnatrz okna rysujemy cala zawartosc panelu przesunieta tak, aby byla widoczna tylko jego prawa czesc
        float protrusion = Mathf.Clamp(sliderRect.x + sliderRect.width - mainWindowRect.xMax,0f, sliderRect.width);
        float localX = -(sliderRect.width - protrusion);
        DrawPanelContents(localX);

        // Consume events to block click-through
        var e = Event.current;
        if (e != null)
        {
            if (e.isMouse || e.type == EventType.ScrollWheel)
            {
                e.Use();
            }
        }
    }

    private void DrawAlarmBarWindow(int id)
    {
        // Cache GUIStyle instead of creating new one every frame
        if (cachedAlarmButtonStyle == null || alarmBarTexture != cachedAlarmButtonStyle.normal.background)
        {
            cachedAlarmButtonStyle = new GUIStyle(GUI.skin.button);
            if (alarmBarTexture != null)
            {
                cachedAlarmButtonStyle.normal.background = alarmBarTexture;
                cachedAlarmButtonStyle.hover.background = alarmBarTexture;
                cachedAlarmButtonStyle.active.background = alarmBarTexture;
                cachedAlarmButtonStyle.border = new RectOffset(0,0,0,0);
                cachedAlarmButtonStyle.padding = new RectOffset(0,0,0,0);
            }
        }
        
        if (GUI.Button(new Rect(0,0, buttonWidth, buttonHeight), GUIContent.none, cachedAlarmButtonStyle))
        {
            isVisible = !isVisible;
        }

        // Consume events to block click-through
        var e = Event.current;
        if (e != null)
        {
            if (e.isMouse || e.type == EventType.ScrollWheel)
            {
                e.Use();
            }
        }
    }

    private void DrawPanelContents(float localX)
    {
        // Tlo panelu - use DrawPanel instead of DrawNoteWindow for side panels
        KerbalUIBackground.DrawPanel(new Rect(localX,0, sliderRect.width, sliderRect.height));

        // Obszar na tresc selektora (z marginesem)
        Rect contentArea = new Rect(localX +8f,8f, sliderRect.width -16f, sliderRect.height -16f);

        if (selector != null)
        {
            // Rysujemy selektor w embedzie: nazwa zakladki na górze, pod spodem lista cial (scroll)
            selector.DrawEmbedded(contentArea, mainWindow, /*TabIndex*/ mainWindow.ActiveTabIndex);
        }
        else
        {
            GUI.Label(new Rect(localX +10,30, sliderRect.width -20,20), "Selector not ready");
        }

        // Brak dragowania (panel przyklejony)
        if (allowDragging)
        {
            // DragWindow dziala tylko w Window, tu pomijamy
        }
    }

    void OnEnable()
    {
        KerbNote.SkinChanged += OnSkinChanged;
    }

    void OnDisable()
    {
        KerbNote.SkinChanged -= OnSkinChanged;
        ClearInputLock();
    }

    private void OnSkinChanged(string skin)
    {
        // Refresh Alarm_bar when skin changes from any source
        ReloadAlarmBarTexture();
        // Recalculate fixed width from new texture (keep horizontal size constant)
        if (alarmBarTexture != null && alarmBarTexture.width > 0)
        {
            barFixedWidth = Mathf.Clamp(alarmBarTexture.width, 8f, 64f);
        }
        else
        {
            barFixedWidth = 18f;
        }
        // Invalidate cached style so it rebuilds with new texture
        cachedAlarmButtonStyle = null;
    }

    // --- API: public helper to force-open panel (uzywane przez MiniNote) ---
    public void ShowPanelImmediate()
    {
        // Wymus pokazanie panelu natychmiast (bez animacji oczekiwania)
        isVisible = true;
        slideT = 1f; // pelne wysuniecie
        UpdateSizesAndAnchors();
        UpdateAnchoredPosition();
    }

    // --- Input lock helpers ---
    private void UpdateInputLock()
    {
        try
        {
            var e = Event.current;
            Vector2 mouse = (e != null) ? e.mousePosition : Vector2.zero;
            bool hover = false;
            if (lastButtonWinRect.width > 0 && lastButtonWinRect.height > 0 && lastButtonWinRect.Contains(mouse)) hover = true;
            if (!hover && lastPanelWinRect.width > 0 && lastPanelWinRect.height > 0 && lastPanelWinRect.Contains(mouse)) hover = true;

            if (hover)
            {
                if (!inputLockSet)
                {
                    InputLockManager.SetControlLock(ControlTypes.All, INPUT_LOCK_ID);
                    inputLockSet = true;
                }
            }
            else
            {
                ClearInputLock();
            }
        }
        catch { }
    }

    private void ClearInputLock()
    {
        if (inputLockSet)
        {
            try { InputLockManager.RemoveControlLock(INPUT_LOCK_ID); } catch { }
            inputLockSet = false;
        }
    }
}
