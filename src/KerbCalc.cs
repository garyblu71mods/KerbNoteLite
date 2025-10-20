using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
public class KerbNote : MonoBehaviour
{
    // --- KONFIGURACJA ELEMENTÓW UI MODA ---
    // Nazwa pliku tekstury, wysokość, szerokość, pozycja X, pozycja Y // Opis zastosowania
    public const string TEXTURE_ICON_ON = "KerbCalcProject/Textures/IconOn"; // 38x38, x:0, y:0 // Ikona aktywna w launcherze
    public const string TEXTURE_ICON_OFF = "KerbCalcProject/Textures/IconOff"; // 38x38, x:0, y:0 // Ikona nieaktywna w launcherze
    public const string TEXTURE_TAB = "KerbCalcProject/Textures/Tab"; // 28x80, x:dynamic, y:topBarY+topBarHeight // Tło zakładki
    public const string TEXTURE_TAB_HOVER = "KerbCalcProject/Textures/TabHover"; // 28x80, x:dynamic, y:topBarY+topBarHeight // Tło zakładki hover
    public const string TEXTURE_TAB_CLICK = "KerbCalcProject/Textures/TabClick"; // 28x80, x:dynamic, y:topBarY+topBarHeight // Tło zakładki klikniętej
    public const string TEXTURE_BUTTON = "KerbCalcProject/Textures/Button"; // 24x80, x:dynamic, y:topBarY // Tło przycisku
    public const string TEXTURE_BUTTON_HOVER = "KerbCalcProject/Textures/ButtonHover"; // 24x80, x:dynamic, y:topBarY // Tło przycisku hover
    public const string TEXTURE_BUTTON_CLICK = "KerbCalcProject/Textures/ButtonClick"; // 24x80, x:dynamic, y:topBarY // Tło przycisku klikniętego
    public const string TEXTURE_BUTTON_RED = "KerbCalcProject/Textures/red_Button"; // 24x80, x:dynamic, y:topBarY // Tło przycisku czerwonego
    public const string TEXTURE_AAA = "KerbCalcProject/Textures/AAA"; // 24x24, x:noteX, y:areaRect.yMax+10 // Ikona zoom pod notatką
    public const string TEXTURE_NOTE_WINDOW = "KerbCalcProject/Textures/NoteWindow"; // dynamic, x:noteX, y:topMargin // Tło obszaru notatki
    public const string TEXTURE_BACKGROUND_WINDOW = "KerbCalcProject/Textures/BackgroundWindow"; // dynamic, x:0, y:0 // Tło całego okna moda
   // public const string TEXTURE_BACKGROUND_UNDO = "KerbCalcProject/Textures/BackgroundUndo"; // dynamic, x:0, y:0 // Tło przycisku undo delete

    public const float WINDOW_DEFAULT_X = 200f; // Pozycja X okna głównego
    public const float WINDOW_DEFAULT_Y = 200f; // Pozycja Y okna głównego
    public const float WINDOW_DEFAULT_WIDTH = 460f; // Szerokość okna głównego
    public const float WINDOW_DEFAULT_HEIGHT = 410f; // Wysokość okna głównego

    public const float TOP_BAR_HEIGHT = 24f; // Wysokość paska górnego
    public const float TOP_BAR_Y = 10f; // Pozycja Y paska górnego
    public const float TOP_BAR_BUTTON_PADDING = 24f; // Padding przycisków na pasku górnym

    public const float TAB_BAR_Y = TOP_BAR_Y + TOP_BAR_HEIGHT + 3f; // Pozycja Y paska zakładek (3px pod topbarem)
    public const float TAB_BARHEIGHT = 20f; // Wysokość paska zakładek (zmniejszona)
    public const float TAB_BAR_MARGIN = 12f; // Margines paska zakładek
    public const float TAB_MIN_WIDTH = 80f; // Minimalna szerokość zakładki
    public const float TAB_MAX_WIDTH = 340f; // Maksymalna szerokość zakładki
    public const float TAB_PADDING = 28f; // Padding tekstu zakładki

    public const float NOTE_BG_MARGIN = 10f; // Margines tła notatki
    public const float NOTE_TOP_MARGIN = TAB_BAR_Y + TAB_BARHEIGHT; // Pozycja Y notatki (bezpośrednio pod tabbarem)
    public const float NOTE_BOTTOM_MARGIN = 100f; // Margines dolny notatki (zmniejszony)
    public const float NOTE_WIDTH = WINDOW_DEFAULT_WIDTH - 2 * NOTE_BG_MARGIN - 2f; // Szerokość notatki
    public const float NOTE_X = NOTE_BG_MARGIN + 1f; // Pozycja X notatki

    public const float AAA_BTN_WIDTH = TOP_BAR_HEIGHT + TOP_BAR_BUTTON_PADDING; // Szerokość przycisku AAA
    public const float AAA_BTN_HEIGHT = TOP_BAR_HEIGHT; // Wysokość przycisku AAA
    public const float AAA_BTN_X = NOTE_X; // Pozycja X przycisku AAA
    public const float AAA_BTN_Y_OFFSET = 10f; // Offset Y przycisku AAA względem notatki

    public const float CALC_DISPLAY_X = 15f; // Pozycja X wyświetlacza kalkulatora
    public const float CALC_DISPLAY_Y = 50f; // Pozycja Y wyświetlacza kalkulatora
    public const float CALC_DISPLAY_WIDTH = WINDOW_DEFAULT_WIDTH - 90f; // Szerokość wyświetlacza kalkulatora (skrócone z prawej)
    public const float CALC_DISPLAY_HEIGHT = 40f; // Wysokość wyświetlacza kalkulatora

    public const float CALC_HISTORY_TOP_MARGIN = 100f; // Margines górny historii kalkulatora
    public const float CALC_HISTORY_BOTTOM_MARGIN = 70f; // Margines dolny historii kalkulatora
    public const float CALC_HISTORY_SIDE_MARGIN = 12f; // Margines boczny historii kalkulatora
    public const float CALC_KEYPAD_X = WINDOW_DEFAULT_WIDTH - 230f; // Pozycja X klawiatury kalkulatora
    public const float CALC_KEYPAD_Y = 100f; // Pozycja Y klawiatury kalkulatora
    public const float CALC_KEYPAD_WIDTH = 210f; // Szerokość klawiatury kalkulatora
    public const float CALC_KEYPAD_HEIGHT = 300f; // Wysokość klawiatury kalkulatora
    // --- KONIEC KONFIGURACJI ---

    // Dragging state
    private bool isDraggingWindow = false;
    private Vector2 dragStartMouse;
    private Rect dragStartRect;

    private GUIStyle renameFieldStyle;
    private bool renameFocusRequested = false;
    private bool showRenamePopup = false;
    private bool showDeleteButton = false;
    float topBarButtonHeight = 24f;
    float topBarButtonPadding = 12f;

    private string tabRenameBuffer = "";
    private int windowID = 123456;
    private Rect windowRect = new Rect(200, 200, WINDOW_DEFAULT_WIDTH, WINDOW_DEFAULT_HEIGHT); // Domyślny rozmiar
    private bool showWindow = false;
    private string notesPath;
    private ApplicationLauncherButton btn;
    private Texture2D iconOn;
    private Texture2D iconOff;

    private List<string> calcHistory = new List<string>();
    private Vector2 historyScroll = Vector2.zero;
    private Texture2D resizeIcon;
    // ✅ Style GUI
    private GUIStyle buttonStyle, buttonStyleRed, noteStyle, textAreaStyle;
    private GUIStyle calcDisplayStyle;

    // ✅ Tekstury zakładek
    private Texture2D TabTexture;
    private Texture2D TabHoverTexture;
    private Texture2D TabClickTexture;

    // Tekstury
    private Texture2D ButtonTexture;
    private Texture2D ButtonHoverTexture;
    private Texture2D ButtonClickTexture;
    private Texture2D AAATexture; // Nowa tekstura dla przycisku zoom
    private Texture2D DropTexture;

    // ✅ Zakładki
    private List<NoteTab> tabs = new List<NoteTab>();
    private int activeTabIndex = 0;

    private Texture2D noteTex; // jeśli chcesz używać osobno

    // ✅ Klasa zakładki

    public class NoteTab
    {
        public string name;
        public string text;
        public string lastSaved;
        public Vector2 scroll;
        public Stack<string> undoStack = new Stack<string>(); // Historia zmian tekstu

        public NoteTab(string name)
        {
            this.name = name;
            this.text = "";
            this.lastSaved = "";
            this.scroll = Vector2.zero;
            this.undoStack = new Stack<string>();
        }
    }
    void Awake()
    {
        GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
    }

    void OnAppLauncherReady()
    {
        if (btn != null) return;

        iconOn = GameDatabase.Instance.GetTexture(TEXTURE_ICON_ON, false);
        iconOff = GameDatabase.Instance.GetTexture(TEXTURE_ICON_OFF, false);

        if (iconOn == null || iconOff == null)
        {
            return;
        }

        btn = ApplicationLauncher.Instance.AddModApplication(
            OnToggleOn, OnToggleOff,
            null, null, null, null,
            ApplicationLauncher.AppScenes.ALWAYS,
            iconOff
        );
    }

    private void InitWindowRect()
    {
        if (windowRect.width <= 0f || windowRect.height <= 0f)
        {
            windowRect = new Rect(100f, 100f, 500f, 400f);
        }
    }
    void Start()
    {
        this.resizeIcon = GameDatabase.Instance.GetTexture("KerbCalcProject/Textures/resize", false);
        if (this.resizeIcon == null)
            Debug.LogWarning("[KerbNote] resize.png not found!");

        // Inicjalizacja tekstur
        backgroundUndoTex = GameDatabase.Instance.GetTexture("KerbCalcProject/Textures/BackgroundUndo", false);
        TabTexture = GameDatabase.Instance.GetTexture(TEXTURE_TAB, false);
        TabHoverTexture = GameDatabase.Instance.GetTexture(TEXTURE_TAB_HOVER, false);
        TabClickTexture = GameDatabase.Instance.GetTexture(TEXTURE_TAB_CLICK, false);
        ButtonTexture = GameDatabase.Instance.GetTexture(TEXTURE_BUTTON, false);
        ButtonHoverTexture = GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_HOVER, false);
        ButtonClickTexture = GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_CLICK, false);
        AAATexture = GameDatabase.Instance.GetTexture(TEXTURE_AAA, false); // Ładowanie tekstury AAA
        noteTex = GameDatabase.Instance.GetTexture(TEXTURE_BACKGROUND_WINDOW, false); // Ładowanie nowego tła okna

        // Logi błędów
        if (Event.current.type == EventType.Repaint)
        {
        }

        if (backgroundUndoTex == null) Debug.LogWarning("[KerbNote] BackgroundUndo.png not found!");
        if (ButtonTexture == null) Debug.LogError("[KerbNote] Button.png not found!");
        if (ButtonHoverTexture == null) Debug.LogError("[KerbNote] ButtonHover.png not found!");
        if (ButtonClickTexture == null) Debug.LogError("[KerbNote] ButtonClick.png not found!");
        if (TabTexture == null) Debug.LogError("[KerbNote] Tab.png not found!");
        if (TabHoverTexture == null) Debug.LogError("[KerbNote] TabHover.png not found!");
        if (TabClickTexture == null) Debug.LogError("[KerbNote] TabClick.png not found!");
        if (AAATexture == null) Debug.LogWarning("[KerbNote] AAA texture not found!");
        if (windowRect.width < 100f || windowRect.height < 100f)
        {
            windowRect = new Rect(200f, 200f, 425f, 400f);
            Debug.Log("[KerbNote] windowRect reset to default in Start()");
        }
        // Inicjalizacja okna
        if (windowRect.width <= 0f || windowRect.height <= 0f)
            windowRect = new Rect(100f, 100f, 500f, 400f);
        else
            windowRect = new Rect(windowRect.x - 19f, windowRect.y + 28f, windowRect.width, windowRect.height);

        // Ładowanie tekstur tła UI
        try { KerbalUIBackground.LoadTexture(); }
        catch (Exception ex) { Debug.LogError("[KerbNote] LoadTexture error: " + ex.Message); }

        // Ładowanie zakładek z pliku
        notesPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/KerbCalcProject/notes.txt");
        try
        {
            if (File.Exists(notesPath))
            {
                string[] lines = File.ReadAllLines(notesPath);
                tabs.Clear();
                NoteTab currentTab = null;
                StringBuilder sb = new StringBuilder();

                foreach (var line in lines)
                {
                    var match = Regex.Match(line, @"^=== (.+) ===$");
                    if (match.Success)
                    {
                        if (currentTab != null)
                        {
                            currentTab.text = sb.ToString().TrimEnd();
                            currentTab.lastSaved = currentTab.text;
                            tabs.Add(currentTab);
                            sb.Clear();
                        }
                        currentTab = new NoteTab(match.Groups[1].Value);
                    }
                    else if (currentTab != null)
                    {
                        sb.AppendLine(line);
                    }
                }
                if (currentTab != null)
                {
                    currentTab.text = sb.ToString().TrimEnd();
                    currentTab.lastSaved = currentTab.text;
                    tabs.Add(currentTab);
                }
                if (tabs.Count == 0)
                    tabs.Add(new NoteTab("Tab 1"));
            }
            else
            {
                tabs.Add(new NoteTab("Tab 1"));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote] notes.txt read error: " + ex.Message);
            tabs.Add(new NoteTab("Tab 1"));
        }
    }

    void OnToggleOn()
    {
        showWindow = true;
        if (btn != null && iconOn != null)
            btn.SetTexture(iconOn);
    }

    void OnToggleOff()
    {
        showWindow = false;
        SaveNotes();
        if (btn != null && iconOff != null)
            btn.SetTexture(iconOff);
    }

    // --- Debug kliknięć myszki ---
    private Vector2 lastClickPos = Vector2.zero;
    private Vector2 clickDistance = Vector2.zero;

    private bool isClickOnAAA = false;

    void OnGUI()
    {
        if (!showWindow) return;

        if (!stylesInitialized)
        {
            InitStyles();
            stylesInitialized = true;
        }

        GUI.skin = HighLogic.Skin;

        Vector2 mouse = Event.current.mousePosition;

        // --- Skalowanie okna ---
        Rect iconRect = new Rect(windowRect.x + windowRect.width + 18f, windowRect.y + windowRect.height - RESIZE_GRIP_SIZE, RESIZE_GRIP_SIZE, RESIZE_GRIP_SIZE);
        Rect resizeHandle = iconRect;
        float maxWidth = WINDOW_MAX_WIDTH;
        float maxHeight = WINDOW_MAX_HEIGHT;
        windowRect.width = Mathf.Min(windowRect.width, maxWidth);
        windowRect.height = Mathf.Min(windowRect.height, maxHeight);
        if (Event.current.type == EventType.MouseDown && resizeHandle.Contains(mouse))
        {
            isResizingWindow = true;
            resizeStartMouse = mouse;
            resizeStartRect = windowRect;
            Event.current.Use();
        }
        if (isResizingWindow && Event.current.type == EventType.MouseDrag)
        {
            Vector2 delta = mouse - resizeStartMouse;
            windowRect.width = Mathf.Clamp(resizeStartRect.width + delta.x, WINDOW_MIN_WIDTH, WINDOW_MAX_WIDTH);
            windowRect.height = Mathf.Clamp(resizeStartRect.height + delta.y, WINDOW_MIN_HEIGHT, WINDOW_MAX_HEIGHT);
            Event.current.Use();
        }
        if (Event.current.type == EventType.MouseUp)
        {
            isResizingWindow = false;
            isClickOnAAA = false; // <-- reset dragowania po puszczeniu myszy
        }
        if (this.resizeIcon != null)
        {
            GUI.DrawTexture(iconRect, this.resizeIcon);
        }

        // Draw window
        windowRect = GUI.Window(windowID, windowRect, DrawWindowContents, "", GUIStyle.none);

        // Debug w lewym górnym rogu okna KSP
      //  GUI.Label(new Rect(10, 10, 320, 40),
         //   $"Mouse X={mouse.x:F1}, Y={mouse.y:F1}\nClick ΔX={clickDistance.x:F1}, ΔY={clickDistance.y:F1}");
    }

    // --- AAA Button below note ---
    void DrawAAABtnBelowNote()
    {
        float btnWidth = AAA_BTN_WIDTH;
        float btnHeight = AAA_BTN_HEIGHT;
        float btnX = NOTE_X + 20f;
        float btnY = aaaBtnY; // Użyj pozycji pod notewindow
        Rect btnRect = new Rect(btnX, btnY, btnWidth, btnHeight);

        // AAA button
        GUIStyle aaaBtnStyle = new GUIStyle(buttonStyle);
        bool pressed = false;
        if (Event.current.type == EventType.MouseDown && btnRect.Contains(Event.current.mousePosition))
        {
            pressed = true;
            isClickOnAAA = true;
            isDraggingWindow = false; // wymuszamy reset dragowania okna
            Event.current.Use();
        }
        if (pressed)
        {
            noteZoomLevel = (noteZoomLevel + 1) % 5;
        }
        if (AAATexture != null)
        {
            float iconMaxWidth = btnWidth - 8f;
            float iconMaxHeight = btnHeight - 8f;
            float iconAspect = 128f / 64f;
            float iconWidth = iconMaxWidth;
            float iconHeight = iconMaxWidth / iconAspect;
            if (iconHeight > iconMaxHeight)
            {
                iconHeight = iconMaxHeight;
                iconWidth = iconMaxHeight * iconAspect;
            }
            float iconX = btnX + (btnWidth - iconWidth) / 2f;
            float iconY = btnY + (btnHeight - iconHeight) / 2f;
            GUI.DrawTexture(new Rect(iconX, iconY, iconWidth, iconHeight), AAATexture);
        }
    }

    private float GetSafeNoteAreaHeight(Rect windowRect, float topMargin, float bottomMargin)
    {
        // Rozmiar oryginalnej tekstury backgroundWindow
        const float backgroundTexHeight = 384f;
        const float logoHeight = 94f;
        float scaledLogoHeight = logoHeight / backgroundTexHeight * windowRect.height;
        float safeBottomY = windowRect.y + windowRect.height - scaledLogoHeight;

        float noteAreaY = windowRect.y + topMargin;
        float maxNoteAreaHeight = safeBottomY - noteAreaY;
        float requestedHeight = windowRect.height - topMargin - bottomMargin;
        return Mathf.Min(requestedHeight, maxNoteAreaHeight > 0 ? maxNoteAreaHeight : 0);
    }

    // Poprawiona metoda DrawNoteArea (tylko jedna w klasie)
    void DrawNoteArea()
    {
        float leftMargin = NOTE_BG_MARGIN + 10f;
        float rightMargin = NOTE_BG_MARGIN + 10f;
        float topMargin = NOTE_TOP_MARGIN;
        float bottomMargin = NOTE_BOTTOM_MARGIN;
        float noteWidth = windowRect.width - leftMargin - rightMargin - 2f;
        float noteX = leftMargin + 1f;
        float usableHeight = GetSafeNoteAreaHeight(windowRect, topMargin, bottomMargin);
        Rect areaRect = new Rect(noteX, topMargin, noteWidth, usableHeight);

        // Zapamiętaj pozycję Y dolnej krawędzi notewindow do AAA
        aaaBtnY = areaRect.yMax + AAA_BTN_Y_OFFSET;

        NoteTab tab = tabs[activeTabIndex];

        int[] zoomFontSizes = { 14, 16, 18, 20, 14 };
        int fontSize = zoomFontSizes[Mathf.Clamp(noteZoomLevel, 0, 4)];

        // Styl przezroczysty dla pola tekstowego
        var transparentTextAreaStyle = new GUIStyle(GUI.skin.textArea);
        transparentTextAreaStyle.fontSize = fontSize;
        transparentTextAreaStyle.fontStyle = FontStyle.Bold;
        transparentTextAreaStyle.wordWrap = true;
        transparentTextAreaStyle.richText = true;
        transparentTextAreaStyle.normal.textColor = new Color(0.95f, 0.95f, 0.85f);
        transparentTextAreaStyle.focused.textColor = new Color(1f, 1f, 1f); // Jaśniejszy tekst gdy aktywne
        transparentTextAreaStyle.normal.background = null;
        transparentTextAreaStyle.focused.background = null;
        transparentTextAreaStyle.active.background = null;
        transparentTextAreaStyle.hover.background = null;
        transparentTextAreaStyle.padding = new RectOffset(16, 16, 16, 16);
        transparentTextAreaStyle.border = new RectOffset(4, 4, 4, 4);
        transparentTextAreaStyle.alignment = TextAnchor.UpperLeft;

        // Tekstura tła pod notatką
        Texture2D noteAreaTex = GameDatabase.Instance.GetTexture(TEXTURE_NOTE_WINDOW, false);
        if (noteAreaTex != null)
            GUI.DrawTexture(areaRect, noteAreaTex);

        // Oblicz wysokość potrzebną dla tekstu
        float scrollViewWidth = noteWidth - 20f;
        float contentHeight = transparentTextAreaStyle.CalcHeight(new GUIContent(tab.text), scrollViewWidth);

        // Rozpocznij obszar scrollowania
        tab.scroll = GUI.BeginScrollView(areaRect, tab.scroll, new Rect(0, 0, scrollViewWidth, contentHeight));

        // Blokuj edycję gdy popup rename
        bool wasEnabled = GUI.enabled;
        if (showRenamePopup)
            GUI.enabled = false;

        GUI.SetNextControlName("NoteField");
        string prevText = tab.text;

        // Pole tekstowe notatki ze scrollowaniem
        tab.text = GUI.TextArea(
            new Rect(0, 0, scrollViewWidth, contentHeight),
            tab.text,
            transparentTextAreaStyle
        );

        // Auto-scroll do kursora podczas pisania
        if (Event.current.type == EventType.KeyDown || 
            (Event.current.type == EventType.Repaint && prevText != tab.text))
        {
            TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (editor != null && GUI.GetNameOfFocusedControl() == "NoteField")
            {
                float cursorY = editor.graphicalCursorPos.y;
                float viewHeight = usableHeight;
                float visibleMargin = fontSize * 2f;
                
                // Jeśli kursor jest poza widocznym obszarem
                if (cursorY > tab.scroll.y + viewHeight - visibleMargin)
                {
                    // Przewiń w dół tak, aby kursor był widoczny
                    float targetY = cursorY - viewHeight + visibleMargin;
                    tab.scroll.y = Mathf.Lerp(tab.scroll.y, targetY, 0.3f);
                }
                else if (cursorY < tab.scroll.y + visibleMargin)
                {
                    // Przewiń w górę tak, aby kursor był widoczny
                    float targetY = cursorY - visibleMargin;
                    tab.scroll.y = Mathf.Lerp(tab.scroll.y, targetY, 0.3f);
                }
                
                // Przewinięcie na sam dół przy wpisywaniu na końcu tekstu
                if (editor.cursorIndex == tab.text.Length && 
                    cursorY > tab.scroll.y + viewHeight * 0.5f)
                {
                    tab.scroll.y = Mathf.Lerp(tab.scroll.y, contentHeight - viewHeight, 0.3f);
                }

                // Wymuszenie repaint aby odświeżyć pole tekstowe
                if (Event.current.type == EventType.KeyDown)
                {
                    GUI.changed = true;
                    Event.current.Use();
                }
            }
        }

        // Utrzymuj fokus na polu tekstowym gdy potrzeba
        if (!showRenamePopup)
        {
            if (Event.current.type == EventType.MouseDown && areaRect.Contains(Event.current.mousePosition))
            {
                GUI.FocusControl("NoteField");
                Event.current.Use();
            }
            else if (Event.current.type == EventType.KeyDown && !isEditActive && 
                     Event.current.keyCode != KeyCode.Escape)
            {
                GUI.FocusControl("NoteField");
            }
        }

        GUI.enabled = wasEnabled;
        GUI.EndScrollView();
    }

    // Prosta metoda DrawWindowContents
    void DrawWindowContents(int id)
    {
        // Tło na całe okno moda
        if (noteTex != null)
            GUI.DrawTexture(new Rect(0, 0, windowRect.width, windowRect.height), noteTex);
        DrawTopBar();
        if (!showCalc)
        {
            DrawTabBar();
            DrawNoteArea();
            DrawAAABtnBelowNote(); // <--- Dodajemy przycisk AAA poniżej notesu
        }
        else
        {
            DrawDisplay();
            DrawCalcHistory();
            DrawKeypad();
        }
        // --- DRAG WINDOW TYLKO GDY NIE kliknięto AAA ---
        if (!isClickOnAAA)
        {
            GUI.DragWindow();
        }
    }

    // Poprawiona metoda DrawTabBar
    void DrawTabBar()
    {
        float tabBarHeight = TAB_BARHEIGHT;
        float tabBarY = TAB_BAR_Y;
        float tabBarMargin = TAB_BAR_MARGIN;
        float tabBarWidth = windowRect.width - 2 * tabBarMargin;
        Rect tabBarRect = new Rect(tabBarMargin, tabBarY, tabBarWidth, tabBarHeight);

        // --- SCROLL WIDTH CALC ---
        float totalTabsWidth = 0f;
        float tabMinWidth = 80f;
        float tabMaxWidth = 340f;
        float padding = 26f; // 13px lewy + 13px prawy
        for (int i = 0; i < tabs.Count; i++)
        {
            string name = tabs[i].name;
            GUIStyle tabStyle = new GUIStyle(GUI.skin.button);
            tabStyle.fontStyle = FontStyle.Bold;
            tabStyle.padding = new RectOffset(13, 13, 2, 2);
            tabStyle.alignment = (i == activeTabIndex) ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            float textWidth = tabStyle.CalcSize(new GUIContent(name)).x;
            float tabWidth = i == activeTabIndex ? Mathf.Clamp(textWidth + padding, tabMinWidth, tabMaxWidth) : tabMinWidth;
            totalTabsWidth += tabWidth + 3f;
        }
        tabBarScrollMax = Mathf.Max(0f, totalTabsWidth - tabBarWidth);
        tabBarScrollOffset = Mathf.Clamp(tabBarScrollOffset, tabBarScrollMin, tabBarScrollMax);

        // --- MOUSE EVENTS ---
        Event e = Event.current;
        Vector2 mouse = e.mousePosition;
        bool mouseInTabBar = tabBarRect.Contains(mouse);
        float scrollThreshold = 2f;
        float debugDeltaX = 0f;

        // --- MOUSE MASK NAD TABBAR ---
        Rect maskRect = tabBarRect;
        bool maskMouseInTabBar = maskRect.Contains(Event.current.mousePosition);
        if (Event.current.type == EventType.MouseDown && maskMouseInTabBar && !isDraggingTab)
        {
            isScrollingTabs = false;
            tabBarClickCandidate = true;
            scrollStartPos = Event.current.mousePosition;
            lastScrollMouseX = Event.current.mousePosition.x;
            tabBarClickedIndex = -1;
            float tabXDrawLocal = -tabBarScrollOffset;
            for (int i = 0; i < tabs.Count; i++)
            {
                float textWidth = new GUIStyle(GUI.skin.button).CalcSize(new GUIContent(tabs[i].name)).x;
                float tabWidth = i == activeTabIndex ? Mathf.Clamp(textWidth + padding, tabMinWidth, tabMaxWidth) : tabMinWidth;
                Rect tabRect = new Rect(tabXDrawLocal, 0, tabWidth, tabBarHeight);
                if (tabRect.Contains(Event.current.mousePosition - new Vector2(tabBarRect.x, tabBarRect.y)))
                {
                    tabBarClickedIndex = i;
                    activeTabIndex = i;
                    tabRenameBuffer = tabs[i].name;
                }
                tabXDrawLocal += tabWidth + 3f;
            }
            Event.current.Use();
        }
        if (Event.current.type == EventType.MouseDrag && (tabBarClickCandidate || isScrollingTabs))
        {
            float deltaX = Event.current.mousePosition.x - lastScrollMouseX;
            if (!isScrollingTabs && Mathf.Abs(Event.current.mousePosition.x - scrollStartPos.x) >= 1f)
            {
                isScrollingTabs = true;
                tabBarClickCandidate = false;
            }
            if (isScrollingTabs)
            {
                tabBarScrollOffset = Mathf.Clamp(tabBarScrollOffset - deltaX, tabBarScrollMin, tabBarScrollMax);
                lastScrollMouseX = Event.current.mousePosition.x;
            }
            Event.current.Use();
        }
        if (Event.current.type == EventType.MouseUp)
        {
            isScrollingTabs = false;
            tabBarClickCandidate = false;
            tabBarClickedIndex = -1;
            Event.current.Use();
        }

        // --- TŁO PASKA ZAKŁADEK ---
        GUIStyle tabBarStyle = new GUIStyle(GUI.skin.box);
        tabBarStyle.normal.background = MakeSolidTexture(1, 1, new Color(0f, 0f, 0f, 0.0f));
        tabBarStyle.border = new RectOffset(4, 4, 4, 4);
        tabBarStyle.padding = new RectOffset(6, 6, 6, 6);
        GUI.Box(tabBarRect, GUIContent.none, tabBarStyle);

        // --- RYSOWANIE ZAKŁADEK Z CLIPPINGIEM ---
        GUI.BeginGroup(tabBarRect);
        float tabXDraw = -tabBarScrollOffset;
        for (int i = 0; i < tabs.Count; i++)
        {
            string name = tabs[i].name;
            GUIStyle tabStyle = new GUIStyle(GUI.skin.button);
            tabStyle.fontStyle = FontStyle.Bold;
            tabStyle.clipping = TextClipping.Clip;
            tabStyle.normal.background = TabTexture;
            tabStyle.hover.background = TabHoverTexture;
            tabStyle.active.background = TabClickTexture;
            tabStyle.padding = new RectOffset(13, 13, 2, 2);
            tabStyle.alignment = (i == activeTabIndex) ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;

            tabStyle.normal.textColor = (i == activeTabIndex) ? new Color(0.8f, 0.6f, 0.2f) : new Color(0.6f, 0.4f, 0.2f);
            tabStyle.hover.textColor = (i == activeTabIndex) ? new Color(0.9f, 0.7f, 0.3f) : new Color(0.7f, 0.5f, 0.3f);
            tabStyle.active.textColor = (i == activeTabIndex) ? new Color(1f, 0.8f, 0.4f) : new Color(0.8f, 0.6f, 0.4f);

            float textWidth = tabStyle.CalcSize(new GUIContent(name)).x;
            float tabWidth = i == activeTabIndex ? Mathf.Clamp(textWidth + padding, tabMinWidth, tabMaxWidth) : tabMinWidth;
            string displayName = name;
            if (i != activeTabIndex && textWidth > tabMinWidth)
            {
                int maxChars = Mathf.Max(1, Mathf.FloorToInt((tabMinWidth - 26f) / 8.5f)); // -26 bo padding 13+13
                if (name.Length > maxChars)
                    displayName = name.Substring(0, maxChars) + "…";
                else
                    displayName = name;
            }

            Rect tabRect = new Rect(tabXDraw, 0, tabWidth, tabBarHeight);
            if (tabRect.x + tabWidth > 0 && tabRect.x < tabBarWidth)
            {
                if (GUI.Button(tabRect, displayName, tabStyle))
                {
                    activeTabIndex = i;
                    tabRenameBuffer = tabs[i].name;
                }
            }
            tabXDraw += tabWidth + 3f;
        }
        GUI.EndGroup();

        // --- DEBUG OVERLAY W LEWYM DOLNYM ROGU OKNA KSP ---
       // float debugX = windowRect.x + 12f;
       // float debugY = windowRect.y + windowRect.height - 20f;
       // GUI.Label(new Rect(debugX, debugY, 400, 20),
           //$"scrollOffset={tabBarScrollOffset:F1}, deltaX={debugDeltaX:F1}, isScrollingTabs={isScrollingTabs}");
        GUI.enabled = true;
    }

    // --- pozostałe metody bez zmian ---

    private bool stylesInitialized = false;


    private void CancelRenamePopup()
    {
        showRenamePopup = false;
        renameFocusRequested = false;
        tabRenameBuffer = tabs[activeTabIndex].name;
        isEditActive = false;
    }
    void DrawDisplay()
    {
        float displayX = CALC_DISPLAY_X;
        float displayY = CALC_DISPLAY_Y;
        float keypadX = windowRect.width - 230f;
        float backspaceOffset = 163f;
        float backspaceWidth = 46f;
        float backspaceX = keypadX + backspaceOffset;
        float displayWidth = backspaceX - displayX; // wyświetlacz kończy się tuż przed przyciskiem
        float displayHeight = CALC_DISPLAY_HEIGHT;

        // Wyświetlacz
        GUILayout.BeginArea(new Rect(displayX, displayY, displayWidth, displayHeight));
        calcInput = GUILayout.TextField(calcInput, calcDisplayStyle, GUILayout.Height(30), GUILayout.ExpandWidth(true));
        GUILayout.EndArea();

        // Przycisk backspace
        GUILayout.BeginArea(new Rect(backspaceX, displayY, backspaceWidth, displayHeight));
        if (GUILayout.Button("←", buttonStyle, GUILayout.Width(backspaceWidth), GUILayout.Height(30)))
        {
            if (!string.IsNullOrEmpty(calcInput))
                calcInput = calcInput.Substring(0, calcInput.Length - 1);
        }
        GUILayout.EndArea();
    }
    void DrawCalcHistory()
    {
        float topMargin = 100f;
        float bottomMargin = 70f;
        float sideMargin = 12f;

        float historyHeight = windowRect.height - topMargin - bottomMargin;

        // Przelicz 30 mm na piksele
        float minWidthMM = 30f;
        float dpi = Screen.dpi > 0 ? Screen.dpi : 96f;
        float minWidthPx = minWidthMM / 25.4f * dpi;

        // Dynamiczna szerokość: np. 25% szerokości okna, ale nie mniej niż 30 mm
        float dynamicWidth = Mathf.Max(windowRect.width * 0.25f, minWidthPx);

        Rect historyAreaRect = new Rect(sideMargin, topMargin, dynamicWidth, historyHeight);

        GUIStyle historyStyle = new GUIStyle(GUI.skin.box);
        historyStyle.normal.background = MakeSolidTexture(1, 1, new Color(0f, 0f, 0f, 0.3f));
        historyStyle.normal.textColor = new Color(0.9f, 1f, 0.9f);
        historyStyle.fontSize = 13;
        historyStyle.padding = new RectOffset(6, 6, 6, 6);
        historyStyle.wordWrap = true;

        GUILayout.BeginArea(historyAreaRect);
        historyScroll = GUILayout.BeginScrollView(historyScroll);
        foreach (string entry in calcHistory)
        {
            GUILayout.Label(entry, historyStyle);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawKeypad()
    {
        GUILayout.BeginArea(new Rect(windowRect.width - 230, 100, 210, 300));
        GUILayout.BeginVertical();

        // Zamiana miejscami: "C", "0", "."
        string[] keys = { "7", "8", "9", "/", "4", "5", "6", "*", "1", "2", "3", "-", "C", "0", ".", "+" };
        float keyWidth = 46f; // szersze
        float keyHeight = 38f; // wyższe
        float keyPadSpacing = 3f;
        for (int row = 0; row < 4; row++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(keyPadSpacing);
            for (int col = 0; col < 4; col++)
            {
                string key = keys[row * 4 + col];
                if (GUILayout.Button(key, buttonStyle, GUILayout.Width(keyWidth), GUILayout.Height(keyHeight)))
                {
                    if (key == "C") calcInput = "";
                    else calcInput += key;
                }
                GUILayout.Space(keyPadSpacing);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(keyPadSpacing);
        }

        GUILayout.Space(3);
        if (GUILayout.Button("💥 Boom", buttonStyleRed, GUILayout.Width(202), GUILayout.Height(44))) // wyższy przycisk
        {
            try
            {
                double result = EvaluateExpression(calcInput);
                string formatted = result.ToString("G");
                calcHistory.Add($"{calcInput} = {formatted}");
                if (calcHistory.Count > 10) calcHistory.RemoveAt(0);
                calcInput = formatted;
            }
            catch
            {
                calcHistory.Add($"{calcInput} = Error");
                if (calcHistory.Count > 10) calcHistory.RemoveAt(0);
                calcInput = "Error";
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    double EvaluateExpression(string expression)
    {
        CultureInfo culture = CultureInfo.InvariantCulture;
        var tokens = Regex.Matches(expression.Replace(" ", ""), @"(\d+(\.\d+)?|[+\-*/()])");
        var output = new Stack<string>();
        var ops = new Stack<string>();
        var precedence = new Dictionary<string, int> { { "+", 1 }, { "-", 1 }, { "*", 2 }, { "/", 2 } };

        foreach (Match token in tokens)
        {
            string t = token.Value;
            if (double.TryParse(t, NumberStyles.Any, culture, out double num)) output.Push(t);
            else if (t == "(") ops.Push(t);
            else if (t == ")")
            {
                while (ops.Count > 0 && ops.Peek() != "(") output.Push(ops.Pop());
                if (ops.Count > 0) ops.Pop();
            }
            else
            {
                while (ops.Count > 0 && ops.Peek() != "(" && precedence[ops.Peek()] >= precedence[t])
                    output.Push(ops.Pop());
                ops.Push(t);
            }
        }

        while (ops.Count > 0) output.Push(ops.Pop());

        var eval = new Stack<double>();
        var reversed = new List<string>(output); reversed.Reverse();

        foreach (var token in reversed)
        {
            if (double.TryParse(token, NumberStyles.Any, culture, out double val)) eval.Push(val);
            else
            {
                double b = eval.Pop(), a = eval.Pop();
                switch (token)
                {
                    case "+": eval.Push(a + b); break;
                    case "-": eval.Push(a - b); break;
                    case "*": eval.Push(a * b); break;
                    case "/": eval.Push(a / b); break;
                }
            }
        }

        return eval.Pop();
    }

    void SaveNotes()
    {
        try
        {
            StringBuilder sb = new StringBuilder();

            foreach (var tab in tabs)
            {
                sb.AppendLine($"=== {tab.name} ===");
                sb.AppendLine(tab.text);
                sb.AppendLine();
                tab.lastSaved = tab.text; // aktualizacja stanu
            }

            File.WriteAllText(notesPath, sb.ToString());
        }
        catch (IOException ex)
        {
        }
    }

    void OnDestroy()
    {
        if (btn != null)
            ApplicationLauncher.Instance.RemoveModApplication(btn);

        GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
        SaveNotes();
    }

    Texture2D MakeSolidTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void InitStyles()
    {
        if (stylesInitialized) return;
        stylesInitialized = true;

        // 🔧 Ładowanie tekstury tła okna
        noteTex = GameDatabase.Instance.GetTexture(TEXTURE_BACKGROUND_WINDOW, false);
        if (noteTex == null)
        {
            Debug.LogWarning("[KerbNote] BackgroundWindow.png not found — używam fallback");
            noteTex = MakeSolidTexture(1, 1, new Color(0.1f, 0.1f, 0.1f, 0.0f)); // całkowicie przezroczyste
        }

        // 🔧 Styl pola rename
        renameFieldStyle = new GUIStyle(GUI.skin.textField);
        renameFieldStyle.fontSize = 14;
        renameFieldStyle.normal.textColor = Color.white;
        renameFieldStyle.normal.background = MakeSolidTexture(1, 1, new Color(0f, 0f, 0f, 0.0f)); // całkowicie przezroczyste

        // 🔧 Styl przycisków
        Texture2D buttonTex = GameDatabase.Instance.GetTexture(TEXTURE_BUTTON, false);
        buttonStyle = new GUIStyle(GUI.skin.button);
        if (buttonTex != null)
        {
            buttonStyle.normal.background = buttonTex;
            buttonStyle.hover.background = buttonTex;
            buttonStyle.active.background = buttonTex;
        }
        buttonStyle.fontSize = 14;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.normal.textColor = Color.white;

        // 🔧 Styl przycisków czerwonych
        buttonStyleRed = new GUIStyle(GUI.skin.button);
        Texture2D redButtonTex = GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_RED, false);
        if (redButtonTex != null)
        {
            buttonStyleRed.normal.background = redButtonTex;
            buttonStyleRed.hover.background = redButtonTex;
            buttonStyleRed.active.background = redButtonTex;
        }
        buttonStyleRed.fontSize = 16; // większa czcionka
        buttonStyleRed.fontStyle = FontStyle.Bold;
        buttonStyleRed.alignment = TextAnchor.MiddleCenter;
        buttonStyleRed.normal.textColor = new Color(1f, 0.4f, 0.4f); // jasnoczerwony

        // 🔧 Styl notatki
        noteStyle = new GUIStyle(GUI.skin.box);
        noteStyle.padding = new RectOffset(12, 12, 12, 12);
        noteStyle.border = new RectOffset(8, 8, 8, 8);
        noteStyle.normal.textColor = Color.white;
        noteStyle.fontSize = 15;
        noteStyle.alignment = TextAnchor.UpperLeft;
        noteStyle.normal.background = MakeSolidTexture(1, 1, new Color(0f, 0f, 0f, 0f)); // całkowicie przezroczyste

        // 🔧 Styl pola kalkulatora
        calcDisplayStyle = new GUIStyle(GUI.skin.textField);
        calcDisplayStyle.fontSize = 20; // większa czcionka
        calcDisplayStyle.fontStyle = FontStyle.Bold;
        calcDisplayStyle.alignment = TextAnchor.MiddleRight;
        calcDisplayStyle.padding = new RectOffset(8, 8, 6, 6);
        calcDisplayStyle.normal.textColor = new Color(1f, 0.95f, 0.3f); // żółty, czytelny
        calcDisplayStyle.normal.background = MakeSolidTexture(1, 1, new Color(0.12f, 0.12f, 0.12f, 0.92f)); // całkowicie przezroczyste
        calcDisplayStyle.hover.background = calcDisplayStyle.normal.background;
        calcDisplayStyle.focused.background = calcDisplayStyle.normal.background;
        calcDisplayStyle.active.background = calcDisplayStyle.normal.background;

        // 🔧 Styl pola tekstowego
        textAreaStyle = new GUIStyle(GUI.skin.textArea);
        textAreaStyle.fontSize = 18; // stały rozmiar czcionki
        textAreaStyle.fontStyle = FontStyle.Bold;
        textAreaStyle.wordWrap = true;
        textAreaStyle.richText = true;
        textAreaStyle.normal.textColor = new Color(0.95f, 0.95f, 0.85f);
        textAreaStyle.normal.background = MakeSolidTexture(1, 1, new Color(0f, 0f, 0f, 0.0f)); // całkowicie przezroczyste
        textAreaStyle.padding = new RectOffset(6, 6, 6, 6);
        textAreaStyle.border = new RectOffset(4, 4, 4, 4);
        textAreaStyle.alignment = TextAnchor.UpperLeft;
    }

    void DrawTopBar()
    {
        float topBarY = 10f;
        float topBarHeight = 24f;
        float buttonPadding = 24f;
        Rect topBarRect = new Rect(10f, topBarY, windowRect.width - 20f, topBarHeight);

        GUILayout.BeginArea(topBarRect);
        GUILayout.BeginHorizontal();

        GUIStyle topButtonStyle = new GUIStyle(GUI.skin.button);
        topButtonStyle.normal.background = ButtonTexture;
        topButtonStyle.hover.background = ButtonHoverTexture;
        topButtonStyle.active.background = ButtonClickTexture;
        topButtonStyle.fontStyle = FontStyle.Bold;
        topButtonStyle.alignment = TextAnchor.MiddleCenter;
        topButtonStyle.normal.textColor = new Color(0.6f, 0.4f, 0.2f);

        if (!showCalc)
        {
            string minusLabel = "-";
            float minusWidth = topButtonStyle.CalcSize(new GUIContent(minusLabel)).x + buttonPadding;

            // ZABLOKUJ przyciski + i - gdy aktywny popup edycji
            if (showRenamePopup)
                GUI.enabled = false;

            if (!showDeleteButton)
            {
                if (GUILayout.Button(minusLabel, topButtonStyle, GUILayout.Width(minusWidth), GUILayout.Height(topBarButtonHeight)))
                {
                    if (tabs.Count > 1)
                    {
                        recentlyDeletedTab = null;
                        recentlyDeletedTabIndex = -1;
                        undoDeleteTime = -1f;
                        pendingPermanentDelete = false;
                        showDeleteButton = true;
                    }
                }

                string plusLabel = "+";
                float plusWidth = topButtonStyle.CalcSize(new GUIContent(plusLabel)).x + buttonPadding;
                if (GUILayout.Button(plusLabel, topButtonStyle, GUILayout.Width(plusWidth), GUILayout.Height(topBarButtonHeight)))
                {
                    string newName = $"Tab {tabs.Count + 1}";
                    tabs.Add(new NoteTab(newName));
                    activeTabIndex = tabs.Count - 1;
                }

                // Przywróć GUI.enabled po przyciskach + i - 
                if (showRenamePopup)
                    GUI.enabled = true;

                string editLabel = "edit";
                float editWidth = topButtonStyle.CalcSize(new GUIContent(editLabel)).x + buttonPadding;
                if (!showRenamePopup && GUILayout.Button(editLabel, topButtonStyle, GUILayout.Width(editWidth), GUILayout.Height(topBarButtonHeight)))
                {
                    Debug.Log("[KerbNote] Edit popup aktywowane");
                    showRenamePopup = true;
                    renameFocusRequested = true;
                    tabRenameBuffer = tabs[activeTabIndex].name;
                    isEditActive = true;
                }
            }
            else
            {
                // Migający, nieaktywne "Delete tab?"
                float pulse = Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * 6f));
                Color baseColor = new Color(1f, 0.2f, 0.2f);
                Color pulseColor = Color.Lerp(baseColor, Color.white, pulse);

                GUIStyle pulseStyle = new GUIStyle(buttonStyleRed);
                pulseStyle.fontSize = 14;
                pulseStyle.alignment = TextAnchor.MiddleCenter;
                pulseStyle.normal.textColor = pulseColor;

                float delWidth = pulseStyle.CalcSize(new GUIContent("Delete 1tab?")).x + buttonPadding;
                GUI.enabled = false;
                GUILayout.Button("Delete tab?", pulseStyle, GUILayout.Width(delWidth), GUILayout.Height(topBarButtonHeight));
                GUI.enabled = true;

                // Przycisk "No"
                string noLabel = "No";
                float noWidth = topButtonStyle.CalcSize(new GUIContent(noLabel)).x + buttonPadding;
                if (GUILayout.Button(noLabel, topButtonStyle, GUILayout.Width(noWidth), GUILayout.Height(topBarButtonHeight)))
                {
                    showDeleteButton = false;
                }

                // Przycisk "Yes"
                string yesLabel = "Yes";
                float yesWidth = buttonStyleRed.CalcSize(new GUIContent(yesLabel)).x + buttonPadding;
                if (GUILayout.Button(yesLabel, buttonStyleRed, GUILayout.Width(yesWidth), GUILayout.Height(topBarButtonHeight)))
                {
                    recentlyDeletedTab = tabs[activeTabIndex];
                    recentlyDeletedTabIndex = activeTabIndex;
                    undoDeleteTime = Time.realtimeSinceStartup;
                    pendingPermanentDelete = true;
                    tabs.RemoveAt(activeTabIndex);
                    if (tabs.Count == 0)
                    {
                        tabs.Add(new NoteTab("Tab 1"));
                        activeTabIndex = 0;
                    }
                    else
                    {
                        activeTabIndex = Mathf.Clamp(activeTabIndex, 0, tabs.Count - 1);
                    }
                    tabRenameBuffer = tabs[activeTabIndex].name;
                    showDeleteButton = false;
                    isUndoHovered = false;
                    undoResumeTime = -1f;
                }
            }

            // --- Przycisk Undo Delete na toparze ---
            if (recentlyDeletedTab != null && pendingPermanentDelete)
            {
                // Sprawdzanie czasu wyświetlania Undo Delete
                float elapsed = Time.realtimeSinceStartup - undoDeleteTime;
                if (elapsed > undoDeleteDuration)
                {
                    recentlyDeletedTab = null;
                    recentlyDeletedTabIndex = -1;
                    undoDeleteTime = -1f;
                    pendingPermanentDelete = false;
                }
                else
                {
                    // Wyblakły styl Undo Delete
                    GUIStyle fadedUndoStyleLocal = new GUIStyle(buttonStyle);
                    fadedUndoStyleLocal.fontStyle = FontStyle.Bold;
                    fadedUndoStyleLocal.normal.textColor = new Color(0.7f, 0.6f, 0.3f, 0.7f);
                    fadedUndoStyleLocal.hover.textColor = new Color(0.8f, 0.7f, 0.4f, 0.8f);
                    fadedUndoStyleLocal.active.textColor = new Color(1f, 0.8f, 0.4f, 0.9f);
                    fadedUndoStyleLocal.normal.background = ButtonTexture;
                    fadedUndoStyleLocal.hover.background = ButtonHoverTexture;
                    fadedUndoStyleLocal.active.background = ButtonClickTexture;
                    float undoWidth = fadedUndoStyleLocal.CalcSize(new GUIContent("Undo Delete")).x + buttonPadding;
                    if (GUILayout.Button("Undo Delete", fadedUndoStyleLocal, GUILayout.Width(undoWidth), GUILayout.Height(topBarButtonHeight)))
                    {
                        tabs.Insert(Mathf.Clamp(recentlyDeletedTabIndex, 0, tabs.Count), recentlyDeletedTab);
                        activeTabIndex = recentlyDeletedTabIndex;
                        tabRenameBuffer = recentlyDeletedTab.name;
                        recentlyDeletedTab = null;
                        recentlyDeletedTabIndex = -1;
                        undoDeleteTime = -1f;
                        pendingPermanentDelete = false;
                    }
                }
            }

            // Przycisk "edit" popup
            if (showRenamePopup)
            {
                // Zapamiętaj pozycję zakładki przed popupem
                if (!editTabOriginalIndex.HasValue)
                    editTabOriginalIndex = activeTabIndex;

                Rect renameButtonRect = GUILayoutUtility.GetLastRect();
                float popupY = renameButtonRect.y; // wyrównanie do przycisku edit
                float popupX = renameButtonRect.x + renameButtonRect.width + 4f + 44f;
                float popupWidth = 216f; // 260f - 44f
                float popupHeight = topBarButtonHeight; // dopasuj do wysokości przycisków
                float arrowBtnWidth = topBarButtonHeight; // kwadratowe
                float arrowBtnHeight = topBarButtonHeight;

                GUI.BeginGroup(new Rect(popupX - arrowBtnWidth * 2, popupY, popupWidth + arrowBtnWidth * 2, popupHeight));
                // < strzałka
                GUIStyle arrowStyleL = new GUIStyle(topButtonStyle);
                bool leftEdge = (activeTabIndex == 0);
                if (leftEdge)
                {
                    Texture2D redBtn = GameDatabase.Instance.GetTexture("KerbCalcProject/red_Button", false);
                    if (redBtn != null)
                    {
                        arrowStyleL.normal.background = redBtn;
                        arrowStyleL.hover.background = redBtn;
                        arrowStyleL.active.background = redBtn;
                    }
                }
                else
                {
                    arrowStyleL.normal.background = ButtonTexture;
                    arrowStyleL.hover.background = ButtonHoverTexture;
                    arrowStyleL.active.background = ButtonClickTexture;
                }
                if (GUI.Button(new Rect(0, 0, arrowBtnWidth, arrowBtnHeight), "<", arrowStyleL) && !leftEdge)
                {
                    var tmp = tabs[activeTabIndex - 1];
                    tabs[activeTabIndex - 1] = tabs[activeTabIndex];
                    tabs[activeTabIndex] = tmp;
                    activeTabIndex--;
                }
                // > strzałka
                GUIStyle arrowStyleR = new GUIStyle(topButtonStyle);
                bool rightEdge = (activeTabIndex == tabs.Count - 1);
                if (rightEdge)
                {
                    Texture2D redBtn = GameDatabase.Instance.GetTexture("KerbCalcProject/red_Button", false);
                    if (redBtn != null)
                    {
                        arrowStyleR.normal.background = redBtn;
                        arrowStyleR.hover.background = redBtn;
                        arrowStyleR.active.background = redBtn;
                    }
                }
                else
                {
                    arrowStyleR.normal.background = ButtonTexture;
                    arrowStyleR.hover.background = ButtonHoverTexture;
                    arrowStyleR.active.background = ButtonClickTexture;
                }
                if (GUI.Button(new Rect(arrowBtnWidth, 0, arrowBtnWidth, arrowBtnHeight), ">", arrowStyleR) && !rightEdge)
                {
                    var tmp = tabs[activeTabIndex + 1];
                    tabs[activeTabIndex + 1] = tabs[activeTabIndex];
                    tabs[activeTabIndex] = tmp;
                    activeTabIndex++;
                }
                // Pole tekstowe i przyciski akceptacji popupu
                GUI.SetNextControlName("RenameField");
                tabRenameBuffer = GUI.TextField(new Rect(arrowBtnWidth * 2, 0, 150f, arrowBtnHeight), tabRenameBuffer, renameFieldStyle);

                if (renameFocusRequested && Event.current.type == EventType.Repaint)
                {
                    GUI.FocusControl("RenameField");
                    renameFocusRequested = false;
                }

                tabRenameBuffer = tabRenameBuffer.Length <= 40 ? tabRenameBuffer : tabRenameBuffer.Substring(0, 40);

                // --- OBSŁUGA ENTER ---
                if (Event.current.type == EventType.KeyDown &&
                    (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) &&
                    GUI.GetNameOfFocusedControl() == "RenameField")
                {
                    tabs[activeTabIndex].name = tabRenameBuffer;
                    CancelRenamePopup();
                    isEditActive = false;
                    editTabOriginalIndex = null;
                    Event.current.Use();
                }

                if (GUI.Button(new Rect(arrowBtnWidth * 2 + 150f, 0f, 30f, arrowBtnHeight), "✔", topButtonStyle))
                {
                    tabs[activeTabIndex].name = tabRenameBuffer;
                    CancelRenamePopup();
                    isEditActive = false;
                    editTabOriginalIndex = null;
                }

                if (GUI.Button(new Rect(arrowBtnWidth * 2 + 180f, 0f, 24f, arrowBtnHeight), "✖", topButtonStyle))
                {
                    // Przywróć zakładkę na oryginalne miejsce jeśli była przesuwana
                    if (editTabOriginalIndex.HasValue && editTabOriginalIndex.Value != activeTabIndex)
                    {
                        var tab = tabs[activeTabIndex];
                        tabs.RemoveAt(activeTabIndex);
                        tabs.Insert(editTabOriginalIndex.Value, tab);
                        activeTabIndex = editTabOriginalIndex.Value;
                    }
                    CancelRenamePopup();
                    isEditActive = false;
                    editTabOriginalIndex = null;
                }

                GUI.EndGroup();
            }
            else
            {
                editTabOriginalIndex = null;
            }
        }

        GUILayout.FlexibleSpace();

        // Przycisk "Note/Calc"
        string calcLabel = showCalc ? "Note" : "Calc";
        float calcWidth = buttonStyle.CalcSize(new GUIContent(calcLabel)).x + buttonPadding;
        if (GUILayout.Button(calcLabel, buttonStyle, GUILayout.Width(calcWidth), GUILayout.Height(topBarButtonHeight)))
            showCalc = !showCalc;

        // Przycisk "X"
        string closeLabel = "X";
        float closeWidth = buttonStyleRed.CalcSize(new GUIContent(closeLabel)).x + buttonPadding;
        if (GUILayout.Button(closeLabel, buttonStyleRed, GUILayout.Width(closeWidth), GUILayout.Height(topBarButtonHeight)))
        {
            showWindow = false;
            btn.SetFalse(true);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        GUI.enabled = true;
    }

    // --- Scroll tab bar ---
    private float tabBarScrollOffset = 0f;
    private const float tabBarScrollMin = 0f;
    private float tabBarScrollMax = 0f;
    private bool isScrollingTabs = false;
    private Vector2 scrollStartPos = Vector2.zero;
    private bool isDraggingTab = false; // na przyszłość, scroll działa tylko gdy dragging == false
    private float lastScrollMouseX = 0f;
    private bool tabBarClickCandidate = false;
    private int tabBarClickedIndex = -1;
    private NoteTab recentlyDeletedTab = null;
    private int recentlyDeletedTabIndex = -1;
    private float undoDeleteTime = -1f;
    private const float undoDeleteDuration = 5f;
    private bool pendingPermanentDelete = false;
    private Texture2D backgroundUndoTex;
    private bool isUndoHovered = false;
    private float undoResumeTime = -1f;
    // --- END ---
    // --- Edit tab original index ---
    private int? editTabOriginalIndex = null;

    private bool showCalc = false;
    private string calcInput = "";
    private bool isEditActive = false;
    private int noteZoomLevel = 0; // 0-4, poziom powiększenia notatki

    // --- Skalowanie okna ---
    private bool isResizingWindow = false;
    private Vector2 resizeStartMouse;
    private Rect resizeStartRect;
    private const float WINDOW_MIN_WIDTH = WINDOW_DEFAULT_WIDTH;
    private const float WINDOW_MIN_HEIGHT = WINDOW_DEFAULT_HEIGHT;
    private const float WINDOW_MAX_WIDTH = 1000f;
    private const float WINDOW_MAX_HEIGHT = 800f;
    private const float RESIZE_GRIP_SIZE = 18f;

    // --- AAA Button Y (dynamic position below note window) ---
    private float aaaBtnY = 0f;
}