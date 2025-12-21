using KSP.UI.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// Statyczna klasa do przechowywania historii kalkulatora miedzy scenami
// Version: 1.2.4 - Force Rebuild
public static class CalcHistoryStore
{
    private static List<string> history = new List<string>();

    public static List<string> History
    {
        get { return history; }
    }

    public static void Add(string entry)
    {
        history.Add(entry);
        if (history.Count > 10) history.RemoveAt(0);
    }

    public static void Clear()
    {
        history.Clear();
    }
}

[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
public partial class KerbNote : MonoBehaviour
{
    // --- Global skin change event so other components can refresh their textures (SliderWindow, Settings, MiniNote, etc.) ---
    public static event Action<string> SkinChanged;

    private void NotifySkinChanged(string skinName)
    {
        try { var ev = SkinChanged; if (ev != null) ev(skinName); }
        catch (Exception ex) { Debug.LogWarning("[KerbNote] SkinChanged notify failed: " + ex.Message); }
    }

    // Readiness gating
    private bool uiPrewarmed = false;
    private bool uiReady = false;
    private Coroutine openWhenReadyCoro;

    // Ensure textures/styles are ready before first show to avoid fallback frame
    private void ForceRebuildStyles()
    {
        Debug.Log("[KerbNote] ForceRebuildStyles");
        // drop cached styles so next InitStyles builds from current textures
        stylesInitialized = false;
        buttonStyle = null;
        buttonStyleRed = null;
        noteStyle = null;
        textAreaStyle = null;
        calcDisplayStyle = null;
    }
    private void PrewarmUI()
    {
        try
        {
            Debug.Log("[KerbNote] PrewarmUI start");
            SkinReload.Reload(this);
            ForceRebuildStyles();
            // Don't call InitStyles() here - GUI functions can only be called from OnGUI()
            // InitStyles will be called automatically on first OnGUI when stylesInitialized is false
            uiPrewarmed = true;
            uiReady = true;
            Debug.Log("[KerbNote] PrewarmUI done -> uiReady=true");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KerbNote] PrewarmUI failed: " + ex.Message);
        }
    }

    private void EnsureUiReady()
    {
        if (uiReady) return;
        try
        {
            Debug.Log("[KerbNote] EnsureUiReady tick: ButtonTex=" + (ButtonTexture!=null) + ", stylesInit=" + stylesInitialized);
            // Only reload skin if textures are actually missing, not every frame
            if (ButtonTexture == null || ButtonHoverTexture == null || ButtonClickTexture == null || TabTexture == null)
            {
                SkinReload.Reload(this);
            }
            // Only rebuild styles if they're not initialized - but don't call InitStyles() here
            // InitStyles() will be called from OnGUI() when stylesInitialized is false
            if (!stylesInitialized)
            {
                ForceRebuildStyles();
            }
            // Mark ready only if core textures exist
            uiReady = (ButtonTexture != null && ButtonHoverTexture != null && ButtonClickTexture != null && TabTexture != null);
            Debug.Log("[KerbNote] EnsureUiReady -> uiReady=" + uiReady);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KerbNote] EnsureUiReady failed: " + ex.Message);
        }
    }

    private IEnumerator OpenWhenReady()
    {
        // Continuously ensure readiness until ready
        while (!uiReady)
        {
            EnsureUiReady();
            yield return null; // wait a frame
        }
        Debug.Log("[KerbNote] OpenWhenReady -> showWindow=true");
        showWindow = true;
        if (btn != null && iconOn != null)
            btn.SetTexture(iconOn);
        openWhenReadyCoro = null;
    }

    // --- KONFIGURACJA ELEMENTÓW UI MODA ---
    // Nazwa pliku tekstury, wysokosc, szerokosc, pozycja X, pozycja Y // Opis zastosowania
    public const string TEXTURE_ICON_ON = "KerbNoteLite/Textures/IconOn"; //38x38, x:0, y:0 // Ikona aktywna w launcherze
    public const string TEXTURE_ICON_OFF = "KerbNoteLite/Textures/IconOff"; //38x38, x:0, y:0 // Ikona nieaktywna w launcherze
    public const string TEXTURE_TAB = "KerbNoteLite/Textures/Tab"; //28x80, x:dynamic, y:topBarY+topBarHeight // Tlo zakladki
    public const string TEXTURE_TAB_HOVER = "KerbNoteLite/Textures/TabHover"; //28x80, x:dynamic, y:topBarY+topBarHeight // Tlo zakladki hover
    public const string TEXTURE_TAB_CLICK = "KerbNoteLite/Textures/TabClick"; //28x80, x:dynamic, y:topBarY+topBarHeight // Tlo zakladki kliknietej
    public const string TEXTURE_BUTTON = "KerbNoteLite/Textures/Button"; //24x80, x:dynamic, y:topBarY // Tlo przycisku
    public const string TEXTURE_BUTTON_HOVER = "KerbNoteLite/Textures/ButtonHover"; //24x80, x:dynamic, y:topBarY // Tlo przycisku hover
    public const string TEXTURE_BUTTON_CLICK = "KerbNoteLite/Textures/ButtonClick"; //24x80, x:dynamic, y:topBarY // Tlo przycisku kliknietego
    public const string TEXTURE_BUTTON_RED = "KerbNoteLite/Textures/red_Button"; //24x80, x:dynamic, y:topBarY // Tlo przycisku czerwonego
    public const string TEXTURE_AAA = "KerbNoteLite/Textures/AAA"; //24x24, x:noteX, y:areaRect.yMax+10 // Ikona zoom pod notatke
    public const string TEXTURE_NOTE_WINDOW = "KerbNoteLite/Textures/NoteWindow"; // dynamic, x:noteX, y:topMargin // Tlo obszaru notatki
    public const string TEXTURE_BACKGROUND_WINDOW = "KerbNoteLite/Textures/BackgroundWindow"; // dynamic, x:0, y:0 // Tlo calego okna moda
   // public const string TEXTURE_BACKGROUND_UNDO = "KerbNoteLite/Textures/BackgroundUndo"; // dynamic, x:0, y:0 // Tlo przycisku undo delete

    public const float WINDOWS_DEFAULT_X = 200f; // Pozycja X okna glównego
    public const float WINDOWS_DEFAULT_Y = 200f; // Pozycja Y okna glównym
    public const float WINDOWS_DEFAULT_WIDTH = 460f; // Szerokosc okna glównym
    public const float WINDOWS_DEFAULT_HEIGHT = 410f; // Wysokosc okna glównym

    public const float TOP_BAR_HEIGHT = 24f; // Wysokosc paska górnego
    public const float TOP_BAR_Y = 10f; // Pozycja Y paska górnego
    public const float TOP_BAR_BUTTON_PADDING = 24f; // Padding przycisków na pasku topbarem

    public const float TAB_BAR_Y = TOP_BAR_Y + TOP_BAR_HEIGHT + 3f; // Pozycja Y paska zakladek (3px pod topbarem)
    public const float TAB_BARHEIGHT = 20f; // Wysokosc paska zakladek (zmniejszona)
    public const float TAB_BAR_MARGIN = 12f; // Margines paska zakladek
    public const float TAB_MIN_WIDTH = 80f; // Minimalna szerokosc zakladki
    public const float TAB_MAX_WIDTH = 340f; // Maksymalna szerokosc zakladki
    public const float TAB_PADDING = 28f; // Padding tekstu zakladki

    public const float NOTE_BG_MARGIN = 10f; // Margines tla notatki
    public const float NOTE_TOP_MARGIN = TAB_BAR_Y + TAB_BARHEIGHT; // Pozycja Y notatki (bezposrednio pod tabbarem)
    public const float NOTE_BOTTOM_MARGIN = 100f; // Margines dolny notatki (zmniejszony)
    public const float NOTE_WIDTH = WINDOWS_DEFAULT_WIDTH - 2 * NOTE_BG_MARGIN - 2f; // Szerokosc notatki
    public const float NOTE_X = NOTE_BG_MARGIN + 1f; // Pozycja X notatki

    public const float AAA_BTN_WIDTH = TOP_BAR_HEIGHT + TOP_BAR_BUTTON_PADDING; // Szerokosc przycisku AAA
    public const float AAA_BTN_HEIGHT = TOP_BAR_HEIGHT; // Wysokosc przycisku AAA
    public const float AAA_BTN_X = NOTE_X; // Pozycja X przycisku AAA
    public const float AAA_BTN_Y_OFFSET = 10f; // Offset Y przycisku AAA wzgledem notatki

    public const float CALC_DISPLAY_X = 15f; // Pozycja X wyswietlacza kalkulatora
    public const float CALC_DISPLAY_Y = 50f; // Pozycja Y wyswietlacza kalkulatora
    public const float CALC_DISPLAY_WIDTH = WINDOWS_DEFAULT_WIDTH - 90f; // Szerokosc wyswietlacza kalkulatora (skrócone z prawej)
    public const float CALC_DISPLAY_HEIGHT = 40f; // Wysokosc wyswietlacza kalkulatora

    public const float CALC_HISTORY_TOP_MARGIN = 100f; // Margines górny historii kalkulatora
    public const float CALC_HISTORY_BOTTOM_MARGIN = 70f; // Margines dolny historii kalkulatora
    public const float CALC_HISTORY_SIDE_MARGIN = 12f; // Margines boczny historii kalkulatora
    public const float CALC_KEYPAD_X = WINDOWS_DEFAULT_WIDTH - 230f; // Pozycja X klawiatury kalkulatora
    public const float CALC_KEYPAD_Y = 100f; // Pozycja Y klawiatury kalkulatora
    public const float CALC_KEYPAD_WIDTH = 210f; // Szerokosc klawiatury kalkulatora
    public const float CALC_KEYPAD_HEIGHT = 300f; // Wysokosc klawiatury kalkulatora
    // --- KONIEC KONFIGURACJI ---

    private const string DEFAULT_SKIN_NAME = "Green";

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
    private Rect windowRect = new Rect(200, 200, WINDOWS_DEFAULT_WIDTH, WINDOWS_DEFAULT_HEIGHT); // Domyslny rozmiar

    // Publiczna wlasciwosc do dostepu do pozycji i rozmiaru okna
    public Rect WindowRect
    {
        get { return windowRect; }
    }

    // Publiczna wlasciwosc do sprawdzania czy okno jest widoczne
    public bool IsWindowVisible
    {
        get { return showWindow; }
    }

    // Publiczny dostep do biezacego aktywnego indeksu zakladki
    public int ActiveTabIndex { get { return activeTabIndex; } }

    // Get GUID of the active tab
    public string ActiveTabGuid
    {
        get
        {
            if (tabs == null || tabs.Count == 0) return null;
            int idx = Mathf.Clamp(activeTabIndex, 0, tabs.Count - 1);
            return tabs[idx].guid;
        }
    }

    // Get GUID for a specific tab index
    public string GetTabGuid(int tabIndex)
    {
        if (tabs == null || tabIndex < 0 || tabIndex >= tabs.Count) return null;
        return tabs[tabIndex].guid;
    }

    // Expose tab count for external helpers (e.g., SettingWindow)
    public int TabCount { get { return tabs != null ? tabs.Count : 0; } }

    // Expose application launcher button for SettingWindow right-click detection
    public static ApplicationLauncherButton AppButton { get; private set; }

    // Selected save override for cross-reading notes/alarms
    public static string ActiveSaveOverride { get; private set; }

    // Current skin name (from header) used for saving
    private string currentSkinName = DEFAULT_SKIN_NAME;

    // Manage multiple MiniNote instances per tab (by GUID)
    private readonly Dictionary<string, MiniNote> miniNotesByGuid = new Dictionary<string, MiniNote>(StringComparer.OrdinalIgnoreCase);

    // Reference to the alarm slider panel
    private SliderWindow sliderWindow;

    // --- expose active tab text for MiniNote ---
    public string ActiveTabText
    {
        get
        {
            if (tabs == null || tabs.Count == 0) return string.Empty;
            return tabs[Mathf.Clamp(activeTabIndex, 0, tabs.Count - 1)].text ?? string.Empty;
        }
    }

    // Provide text for an arbitrary tab index
    public string GetTabText(int tabIndex)
    {
        if (tabs == null || tabIndex < 0 || tabIndex >= tabs.Count) return string.Empty;
        return tabs[tabIndex].text ?? string.Empty;
    }

    // Provide name/title for an arbitrary tab index
    public string GetTabName(int tabIndex)
    {
        if (tabs == null || tabIndex < 0 || tabIndex >= tabs.Count) return string.Empty;
        return string.IsNullOrEmpty(tabs[tabIndex].name) ? ("Tab " + (tabIndex + 1)) : tabs[tabIndex].name;
    }

    // Get tab index from GUID
    public int GetTabIndexFromGuid(string guid)
    {
        if (tabs == null || string.IsNullOrEmpty(guid)) return -1;
        for (int i = 0; i < tabs.Count; i++)
        {
            if (string.Equals(tabs[i].guid, guid, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private bool showWindow = false;
    private string notesPath;
    private ApplicationLauncherButton btn;
    private Texture2D iconOn;
    private Texture2D iconOff;

    // Historia kalkulatora teraz uzywa statycznego store zamiast lokalnej listy
    private Vector2 historyScroll = Vector2.zero;
    private Texture2D resizeIcon;

    // ? Style GUI
    private GUIStyle buttonStyle, buttonStyleRed, noteStyle, textAreaStyle;
    private GUIStyle calcDisplayStyle;

    // ? Tekstury zakladek
    private Texture2D TabTexture;
    private Texture2D TabHoverTexture;
    private Texture2D TabClickTexture;

    // Tekstury
    private Texture2D ButtonTexture;
    private Texture2D ButtonHoverTexture;
    private Texture2D ButtonClickTexture;
    private Texture2D AAATexture; // Nowa tekstura dla przycisku zoom
    private Texture2D DropTexture;

    // ? Zakladki
    private List<NoteTab> tabs = new List<NoteTab>();
    private int activeTabIndex = 0;

    private Texture2D noteTex; // jesli chcesz uzywac osobno
    private Texture2D noteAreaTex; // skin-driven NoteWindow for note area

    // reference to MiniNote
    private MiniNote miniNote;

    // --- editor click lock state ---
    private const string EditorLockID = "KerbNote_EditorLock";
    private bool editorLockActive = false;
    private Rect lastMiniBtnRectScreen = new Rect();

    // ? Klasa zakladki

    public class NoteTab
    {
        public string guid; // unikalny identyfikator zakladki
        public string name;
        public string text;
        public string lastSaved;
        public Vector2 scroll;
        public Stack<string> undoStack = new Stack<string>(); // Historia zmian tekstu

        public NoteTab(string name)
        {
            this.guid = System.Guid.NewGuid().ToString(); // generuj unikalny GUID
            this.name = name;
            this.text = "";
            this.lastSaved = "";
            this.scroll = Vector2.zero;
            this.undoStack = new Stack<string>();
        }

        // Konstruktor do wczytywania z pliku z GUID
        public NoteTab(string guid, string name)
        {
            this.guid = !string.IsNullOrEmpty(guid) ? guid : System.Guid.NewGuid().ToString();
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
        // Init alarms storage
        AlarmManager.Init();
        // Init global alarms storage
        GlobalAlarmManager.Init();
        // Init audio
        SoundManager.Init();
        // Configure skin ASAP from current/active save header so icons/backgrounds use it
        try
        {
            notesPath = ComputeNotesPath();
            EnsureNotesDirectory();
            EnsureNotesHeaderExists(notesPath, DEFAULT_SKIN_NAME);
            string fromHeader = ReadSkinHeader(notesPath);
            if (!string.IsNullOrEmpty(fromHeader)) currentSkinName = fromHeader; else currentSkinName = DEFAULT_SKIN_NAME;
            ApplySkinInternal(currentSkinName); // configure resolver and refresh textures
            // Prewarm immediately so first OnGUI uses skin textures/styles
            PrewarmUI();
        }
        catch { }
    }

    void OnAppLauncherReady()
    {
        if (btn != null) return;

        // Use SkinAssets (file-based) first for icons
        iconOn = SkinAssets.Get("IconOn") ?? GameDatabase.Instance.GetTexture(TEXTURE_ICON_ON, false);
        iconOff = SkinAssets.Get("IconOff") ?? GameDatabase.Instance.GetTexture(TEXTURE_ICON_OFF, false);

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
        AppButton = btn; // expose for settings right-click detection
    }

    private void InitWindowRect()
    {
        // Center window on first launch or if invalid size
        if (windowRect.width <= 0f || windowRect.height <= 0f)
        {
            float w = 500f;
            float h = 400f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            windowRect = new Rect(x, y, w, h);
            Debug.Log($"[KerbNote] InitWindowRect centered at screen center: {windowRect}");
        }
    }
    void Start()
    {
        // Ensure main window has a sane centered rect (but do not show it yet)
        if (windowRect.width <= 0f || windowRect.height <= 0f || windowRect.width < 100f || windowRect.height < 100f)
            InitWindowRect();
        else
            windowRect = new Rect((Screen.width - windowRect.width) / 2f, (Screen.height - windowRect.height) / 2f, windowRect.width, windowRect.height);

        // Do NOT show window on startup; user opens via app icon
        showWindow = false;

        // Inicjalizacja SliderWindow
        GameObject sliderObj = new GameObject("SliderWindow");
        SliderWindow slider = sliderObj.AddComponent<SliderWindow>();
        slider.mainWindowRect = this.windowRect;
        sliderWindow = slider; // keep reference for MiniNote integration

        // Inicjalizacja GlobalAlarmPanel (lewy panel z alarmami)
        GameObject alarmPanelObj = new GameObject("GlobalAlarmPanel");
        GlobalAlarmPanel alarmPanel = alarmPanelObj.AddComponent<GlobalAlarmPanel>();
        globalAlarmPanel = alarmPanel;
        // Ensure it starts hidden (full reset sets slideT=0 and positions it off-screen)
        try
        {
            globalAlarmPanel.mainWindowRect = this.windowRect;
            globalAlarmPanel.ResetHiddenImmediate();
        }
        catch { }

        this.resizeIcon = SkinAssets.Get("resize") ?? GameDatabase.Instance.GetTexture("KerbNoteLite/Textures/resize", false);
        if (this.resizeIcon == null)
            Debug.LogWarning("[KerbNote] resize.png not found!");

        // Upewnij sie, ze sciezka notatek/szynka jest ustawiona i naglórek istnieje
        notesPath = ComputeNotesPath();
        EnsureNotesDirectory();
        EnsureNotesHeaderExists(notesPath, DEFAULT_SKIN_NAME);
        var headerSkin = ReadSkinHeader(notesPath);
        if (!string.IsNullOrEmpty(headerSkin)) currentSkinName = headerSkin;
        ApplySkinInternal(currentSkinName);

        // Force re-apply after textures are loaded to ensure SettingWindow/SliderWindow pick the same skin
        try { SkinReload.Reload(this); } catch { }

        // Inicjalizacja tekstur
        backgroundUndoTex = GameDatabase.Instance.GetTexture("KerbNoteLite/Textures/BackgroundUndo", false);
        TabTexture = SkinAssets.Get("Tab") ?? GameDatabase.Instance.GetTexture(TEXTURE_TAB, false);
        TabHoverTexture = SkinAssets.Get("TabHover") ?? GameDatabase.Instance.GetTexture(TEXTURE_TAB_HOVER, false);
        TabClickTexture = SkinAssets.Get("TabClick") ?? GameDatabase.Instance.GetTexture(TEXTURE_TAB_CLICK, false);
        ButtonTexture = SkinAssets.Get("Button") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON, false);
        ButtonHoverTexture = SkinAssets.Get("ButtonHover") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_HOVER, false);
        ButtonClickTexture = SkinAssets.Get("ButtonClick") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_CLICK, false);
        AAATexture = SkinAssets.Get("AAA") ?? GameDatabase.Instance.GetTexture(TEXTURE_AAA, false); // Ladowanie tekstury AAA
        noteTex = SkinAssets.Get("BackgroundWindow") ?? GameDatabase.Instance.GetTexture(TEXTURE_BACKGROUND_WINDOW, false); // Ladowanie tla okna
        noteAreaTex = SkinAssets.Get("NoteWindow") ?? GameDatabase.Instance.GetTexture(TEXTURE_NOTE_WINDOW, false); // Ladowanie tla obszaru notatki

        // Init overlay Mini button window id
        miniButtonWindowID = GetInstanceID() ^0x11EE77;

        // Logi bledów
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
        
        // Ensure window rect is valid and centered on first run
        if (windowRect.width < 100f || windowRect.height < 100f)
        {
            InitWindowRect();
            Debug.Log("[KerbNote] windowRect initialized via InitWindowRect() in Start()");
        }

        // Inicjalizacja okna
        // windowRect already centered at the beginning of Start()
        if (windowRect.width <= 0f || windowRect.height <= 0f)
            InitWindowRect();
        
        // Ladowanie tekstur tla UI
        try { KerbalUIBackground.LoadTexture(); }
        catch (Exception ex) { Debug.LogError("[KerbNote] LoadTexture error: " + ex.Message); }

        // --- Per-save notes path (stored in mod folder subdir) + migration from legacy/global per-save file ---
        notesPath = ComputeNotesPath();
        EnsureNotesDirectory();
        string legacyGlobalPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/KerbNoteLite/notes.txt");
        MigrateLegacyNotesIfNeeded(legacyGlobalPath);
        // Also touch-create empty file for new saves, so notes don't overlap between saves (after migration)
        try { if (!File.Exists(notesPath)) File.WriteAllText(notesPath, string.Empty); } catch {}

        // Try load from per-save file, else fallback to legacy, else create default
        if (!TryLoadNotesFrom(notesPath))
        {
            if (TryLoadNotesFrom(legacyGlobalPath))
            {
                // Persist immediately into per-save location after successful fallback
                SaveNotes();
            }
            else
            {
                tabs.Clear();
                tabs.Add(new NoteTab("Tab1"));
            }
        }

        // Remove legacy single MiniNote creation. We now manage instances per tab only via button and dictionary.
        // Rebind and de-duplicate persistent MiniNotes across scene changes (one per TabGuid).
        var minis = FindObjectsOfType<MiniNote>();
        var map = new Dictionary<string, MiniNote>(StringComparer.OrdinalIgnoreCase);
        foreach (var mn in minis)
        {
            if (mn == null) continue;
            // Bind to this KerbNote instance (do not force reposition here)
            mn.SetHost(this);
            string guid = mn.TabGuid;
            if (string.IsNullOrEmpty(guid)) continue; // skip instances without valid GUID
            
            if (map.TryGetValue(guid, out var existing) && existing != null && existing != mn)
            {
                // Prefer the visible instance; if both/none visible, prefer older (lower InstanceID)
                bool keepExisting;
                if (existing.IsVisible && !mn.IsVisible) keepExisting = true;
                else if (!existing.IsVisible && mn.IsVisible) keepExisting = false;
                else keepExisting = existing.GetInstanceID() < mn.GetInstanceID();

                var toDestroy = keepExisting ? mn : existing;
                var toKeep = keepExisting ? existing : mn;
                if (!ReferenceEquals(toDestroy, toKeep))
                {
                    Debug.Log("[KerbNote] Duplicate MiniNote for tab guid " + guid + ". Keeping id=" + toKeep.GetInstanceID() + ", destroying id=" + toDestroy.GetInstanceID());
                    Destroy(toDestroy.gameObject);
                    map[guid] = toKeep;
                }
            }
            else
            {
                map[guid] = mn;
            }
        }
        miniNotesByGuid.Clear();
        foreach (var kv in map)
            miniNotesByGuid[kv.Key] = kv.Value;
        
        // Cleanup orphaned alarms (alarms for tabs that no longer exist or have invalid GUID format)
        try
        {
            AlarmManager.CleanupOrphanedAlarms(this);
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote] Failed to cleanup orphaned alarms: " + ex.Message);
        }
    }

    // Compute safe note area height (avoid drawing over bottom logo area)
    private float GetSafeNoteAreaHeight(Rect windowRect, float topMargin, float bottomMargin)
    {
        const float backgroundTexHeight =384f;
        const float logoHeight =94f;
        float scaledLogoHeight = logoHeight / backgroundTexHeight * windowRect.height;
        float safeBottomY = windowRect.y + windowRect.height - scaledLogoHeight;

        float noteAreaY = windowRect.y + topMargin;
        float maxNoteAreaHeight = safeBottomY - noteAreaY;
        float requestedHeight = windowRect.height - topMargin - bottomMargin;
        return Mathf.Min(requestedHeight, maxNoteAreaHeight >0 ? maxNoteAreaHeight :0);
    }

    void OnToggleOn()
    {
        if (!uiReady)
        {
            // Queue opening until UI is ready to avoid any fallback draw
            if (openWhenReadyCoro == null)
                openWhenReadyCoro = StartCoroutine(OpenWhenReady());
            return;
        }
        showWindow = true;
        // ensure global alarm panel starts hidden when opening
        try { if (globalAlarmPanel != null) globalAlarmPanel.ResetHiddenImmediate(); } catch { }
        if (btn != null && iconOn != null)
            btn.SetTexture(iconOn);
    }

    void OnToggleOff()
    {
        showWindow = false;
        SaveNotes();
        if (editorLockActive) RemoveEditorLock();
        if (btn != null && iconOff != null)
            btn.SetTexture(iconOff);
        // Leave any MiniNotes as they are (persist across scenes)
    }

    // Open/show the main window and focus a specific tab
    public void OpenOnTab(int tabIndex)
    {
        if (tabs == null || tabs.Count ==0)
        {
            showWindow = true;
            if (btn != null) btn.SetTrue(true);
            return;
        }
        activeTabIndex = Mathf.Clamp(tabIndex, 0, tabs.Count - 1);
        showWindow = true;
        if (btn != null) btn.SetTrue(true);
        if (btn != null && iconOn != null) btn.SetTexture(iconOn);
    }

    // Public helper to instantly show the alarm panel (used by MiniNote "edit")
    public void ShowAlarmPanelImmediate()
    {
        try
        {
            if (sliderWindow == null)
            {
                sliderWindow = FindObjectOfType<SliderWindow>();
            }
            if (sliderWindow != null)
            {
                sliderWindow.ShowPanelImmediate();
            }
        }
        catch { }
    }

    // Public helper to hide the main window (used by MiniNote to toggle editing)
    public void HideWindow()
    {
        OnToggleOff();
        if (btn != null)
        {
            try { btn.SetFalse(true); } catch { }
        }
    }

    // Switch active save for notes and alarms (Settings)
    public void SwitchToSave(String saveName)
    {
        ActiveSaveOverride = saveName;
        // Notes path handling
        notesPath = ComputeNotesPath();
        EnsureNotesDirectory();
        // Attempt migration from old locations when switching saves
        String legacyGlobalPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/KerbNoteLite/notes.txt");
        MigrateLegacyNotesIfNeeded(legacyGlobalPath);
        try { if (!File.Exists(notesPath)) File.WriteAllText(notesPath, string.Empty); } catch { }

        // Ensure header exists and apply skin from that header BEFORE loading UI textures
        EnsureNotesHeaderExists(notesPath, DEFAULT_SKIN_NAME);
        var name = ReadSkinHeader(notesPath);
        if (!string.IsNullOrEmpty(name)) currentSkinName = name; else currentSkinName = DEFAULT_SKIN_NAME;
        ApplySkinInternal(currentSkinName);
        // ensure styles drop and rebuild after switching save/skin
        ForceRebuildStyles();

        if (!TryLoadNotesFrom(notesPath))
        {
            tabs.Clear();
            tabs.Add(new NoteTab("Tab1"));
        }
        // Re-init alarms for this save path
        AlarmManager.Init();
        
        // Cleanup orphaned alarms after loading new save
        try
        {
            AlarmManager.CleanupOrphanedAlarms(this);
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote] Failed to cleanup orphaned alarms on save switch: " + ex.Message);
        }
    }

    // --- Debug klikniec myszki ---
    private Vector2 lastClickPos = Vector2.zero;
    private Vector2 clickDistance = Vector2.zero;

    private bool isClickOnAAA = false;

    // Tracker to avoid spamming logs for outside button
    private bool miniOutsideRectLogged = false;

    private int miniButtonWindowID =0; // overlay window for Mini button (always on top)

    // --- pozostale metody bez zmian ---

    private bool stylesInitialized = false;


    private void CancelRenamePopup()
    {
        showRenamePopup = false;
        renameFocusRequested = false;
        tabRenameBuffer = tabs[activeTabIndex].name;
        isEditActive = false;
    }
    double EvaluateExpression(string expression)
    {
        // Nowa, bardziej niezawodna implementacja: shunting-yard + postfix
        // Obsluga: + - * /, nawiasy (), unary +/-, liczby z kropka lub przecinkiem
        if (string.IsNullOrWhiteSpace(expression)) return 0d;
        string expr = expression.Trim();

        // Zamien przecinki na kropki (ulatwia parsowanie InvariantCulture)
        expr = expr.Replace(',', '.');

        // Tokenizacja reczna (aby wylapac unary minus / plus)
        var tokens = new List<string>();
        int i = 0;
        while (i < expr.Length)
        {
            char c = expr[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (char.IsDigit(c) || c == '.')
            {
                int start = i; bool hasDot = (c == '.');
                i++;
                while (i < expr.Length)
                {
                    char d = expr[i];
                    if (char.IsDigit(d)) { i++; continue; }
                    if (d == '.' && !hasDot) { hasDot = true; i++; continue; }
                    break;
                }
                tokens.Add(expr.Substring(start, i - start));
                continue;
            }
            if (c == '+' || c == '-' )
            {
                // Unary jesli na poczatku albo po innym operatorze lub '('
                bool unary = (tokens.Count == 0) ||
                              (tokens.Count > 0 && IsOperator(tokens[tokens.Count - 1])) ||
                              (tokens.Count > 0 && tokens[tokens.Count - 1] == "(");
                if (unary)
                {
                    // Zlacz ze nastepna liczba jesli wystepuje
                    int signStart = i;
                    i++; // konsumuj znak
                    // Oczekujemy liczby po unary; jesli brak -> traktuj jako 0 +/- (np. - (3+2)) -> wstaw 0
                    // Pomin spacje
                    while (i < expr.Length && char.IsWhiteSpace(expr[i])) i++;
                    if (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.'))
                    {
                        int numStart = i; bool hasDot2 = (expr[i] == '.'); i++;
                        while (i < expr.Length)
                        {
                            char d = expr[i];
                            if (char.IsDigit(d)) { i++; continue; }
                            if (d == '.' && !hasDot2) { hasDot2 = true; i++; continue; }
                            break;
                        }
                        string num = expr.Substring(numStart, i - numStart);
                        tokens.Add((c == '-' ? "-" : "+") + num); // np. -3 -> pojedynczy token
                    }
                    else
                    {
                        // Wstaw 0 i operator jesli brak liczby (np. -(3+2))
                        tokens.Add("0");
                        tokens.Add(c.ToString());
                    }
                    continue;
                }
                else
                {
                    tokens.Add(c.ToString()); i++; continue;
                }
            }
            if (c == '*' || c == '/' || c == '(' || c == ')')
            {
                tokens.Add(c.ToString()); i++; continue;
            }
            // Nieznany znak – zglos blad
            throw new ArgumentException("Nieznany znak w wyrazeniu: '" + c + "'");
        }

        // Shunting yard: konwersja do postfix
        var output = new List<string>();
        var opStack = new Stack<string>();
        foreach (var t in tokens)
        {
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                output.Add(t);
            }
            else if (t == "(")
            {
                opStack.Push(t);
            }
            else if (t == ")")
            {
                while (opStack.Count > 0 && opStack.Peek() != "(")
                    output.Add(opStack.Pop());
                if (opStack.Count == 0 || opStack.Peek() != "(")
                    throw new ArgumentException("Niezgodne nawiasy");
                opStack.Pop(); // usun '('
            }
            else if (IsOperator(t))
            {
                while (opStack.Count > 0 && IsOperator(opStack.Peek()) &&
                       OperatorPrecedence(opStack.Peek()) >= OperatorPrecedence(t))
                {
                    output.Add(opStack.Pop());
                }
                opStack.Push(t);
            }
            else
            {
                throw new ArgumentException("Nieznany token: " + t);
            }
        }
        while (opStack.Count > 0)
        {
            var op = opStack.Pop();
            if (op == "(" || op == ")") throw new ArgumentException("Niezgodne nawiasy (pozostaly)");
            output.Add(op);
        }

        // Ewaluacja postfix
        var valStack = new Stack<double>();
        foreach (var t in output)
        {
            double num;
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out num))
            {
                valStack.Push(num);
                continue;
            }
            if (valStack.Count < 2) throw new ArgumentException("Blad skladni przy operatorze '" + t + "'");
            double b = valStack.Pop();
            double a = valStack.Pop();
            switch (t)
            {
                case "+": valStack.Push(a + b); break;
                case "-": valStack.Push(a - b); break;
                case "*": valStack.Push(a * b); break;
                case "/":
                    if (Mathf.Approximately((float)b, 0f)) throw new DivideByZeroException("Dzielenie przez zero");
                    valStack.Push(a / b); break;
                default: throw new ArgumentException("Nieznany operator w ewaluacji: " + t);
            }
        }
        if (valStack.Count != 1) throw new ArgumentException("Blad koncowy ewaluacji – stos ma " + valStack.Count + " elementów");
        return valStack.Pop();
    }

    private static bool IsOperator(string t)
    {
        return t == "+" || t == "-" || t == "*" || t == "/";
    }
    private static int OperatorPrecedence(string op)
    {
        return (op == "+" || op == "-") ? 1 : (op == "*" || op == "/") ? 2 : 0;
    }
    void SaveNotes()
    {
        try
        {
            EnsureNotesDirectory();

            StringBuilder sb = new StringBuilder();

            // Prepend or update header with skin name
            string header = !string.IsNullOrEmpty(currentSkinName) ? currentSkinName : (ReadSkinHeader(notesPath) ?? DEFAULT_SKIN_NAME);
            if (string.IsNullOrEmpty(header)) header = DEFAULT_SKIN_NAME;
            sb.AppendLine("Skin: " + header);
            sb.AppendLine();

            foreach (var tab in tabs)
            {
                // Format: === TabName|Guid ===
                sb.AppendLine($"=== {tab.name}|{tab.guid} ===");
                sb.AppendLine(tab.text ?? string.Empty);
                sb.AppendLine();
                tab.lastSaved = tab.text; // aktualizacja stanu
            }

            File.WriteAllText(notesPath, sb.ToString());
        }
        catch (IOException ex)
        {
            Debug.LogError("[KerbNote] notes write error: " + ex.Message);
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote] notes unexpected write error: " + ex.Message);
        }
    }

    void OnDestroy()
    {
        if (editorLockActive) RemoveEditorLock();
        if (btn != null)
            ApplicationLauncher.Instance.RemoveModApplication(btn);

        GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
        SaveNotes();
    }

    // --- Scroll tab bar ---
    private float tabBarScrollOffset = 0f;
    private const float tabBarScrollMin = 0f;
    private float tabBarScrollMax = 0f;
    private bool isScrollingTabs = false;
    private Vector2 scrollStartPos = Vector2.zero;
    private bool isDraggingTab = false; // na przyszlosc, scroll dziala tylko gdy dragging == false
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
    private int noteZoomLevel = 0; // 0-4, poziom powiekszenia notatki

    // --- Skalowanie okna ---
    private bool isResizingWindow = false;
    private Vector2 resizeStartMouse;
    private Rect resizeStartRect;
    private const float WINDOW_MIN_WIDTH = WINDOWS_DEFAULT_WIDTH;
    private const float WINDOW_MIN_HEIGHT = WINDOWS_DEFAULT_HEIGHT;
    private const float WINDOWS_MAX_WIDTH = 1000f;
    private const float WINDOWS_MAX_HEIGHT = 800f;
    private const float RESIZE_GRIP_SIZE = 18f;

    // --- AAA Button Y (dynamic position below note window) ---
    private float aaaBtnY = 0f;

    // --- Helpers for per-save notes ---
    private string ComputeNotesPath()
    {
        try
        {
            string folder = !string.IsNullOrEmpty(ActiveSaveOverride) ? ActiveSaveOverride : HighLogic.SaveFolder;
            string modDir = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbNoteLite", "AlarmsAndNotes");
            if (!string.IsNullOrEmpty(folder))
            {
                // Prefer upper-case naming, but accept lower-case if that file already exists
                string upper = Path.Combine(modDir, $"Notes_{folder}.txt");
                if (File.Exists(upper)) return upper;
                string lower = Path.Combine(modDir, $"notes_{folder}.txt");
                if (File.Exists(lower)) return lower;
                return upper; // default to upper when creating new
            }
            // Fallback when no save context (e.g., main menu)
            string fallbackUpper = Path.Combine(modDir, "notes.txt");
            if (File.Exists(fallbackUpper)) return fallbackUpper;
            return fallbackUpper;
        }
        catch { }
        // Fallback (defensive)
        return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbNoteLite", "AlarmsAndNotes", "notes.txt");
    }

    public static string ComputeNotesPathForSave(string saveName)
    {
        try
        {
            string modDir = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbNoteLite", "AlarmsAndNotes");
            if (string.IsNullOrEmpty(saveName)) return Path.Combine(modDir, "notes.txt");
            return Path.Combine(modDir, $"Notes_{saveName}.txt");
        }
        catch { return null; }
    }

    private void EnsureNotesDirectory()
    {
        try
        {
            var dir = Path.GetDirectoryName(notesPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote] EnsureNotesDirectory failed: " + ex.Message);
        }
    }

    private void MigrateLegacyNotesIfNeeded(string legacyPath)
    {
        try
        {
            // Allow migration when target is missing or an empty placeholder
            bool canMigrate = !File.Exists(notesPath) || new FileInfo(notesPath).Length == 0;
            if (!canMigrate) return;

            EnsureNotesDirectory();

            string folder = !string.IsNullOrEmpty(ActiveSaveOverride) ? ActiveSaveOverride : HighLogic.SaveFolder;
            // Candidates to migrate from (in order):
            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(folder))
            {
                // old per-save in mod root (both case variants)
                candidates.Add(Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbNoteLite", $"Notes_{folder}.txt"));
                candidates.Add(Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbNoteLite", $"notes_{folder}.txt"));
                // new folder but lower-case variant
                candidates.Add(Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbNoteLite", "AlarmsAndNotes", $"notes_{folder}.txt"));
            }
            if (!string.IsNullOrEmpty(legacyPath)) candidates.Add(legacyPath);

            foreach (var src in candidates)
            {
                try
                {
                    if (!string.IsNullOrEmpty(src) && File.Exists(src) && new FileInfo(src).Length > 0)
                    {
                        File.Copy(src, notesPath, true);
                        Debug.Log("[KerbNote] Migrated notes from " + src + " to " + notesPath);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[KerbNote] Migration copy failed from " + src + ": " + ex.Message);
                }
            }

            // If nothing to migrate, ensure the target exists
            if (!File.Exists(notesPath))
            {
                File.WriteAllText(notesPath, string.Empty);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote] Migration failed: " + ex.Message);
        }
    }

    private bool TryLoadNotesFrom(string path)
    {
        if (!File.Exists(path)) return false;

        try
        {
            string[] lines = File.ReadAllLines(path);
            tabs.Clear();
            NoteTab currentTab = null;
            StringBuilder sb = new StringBuilder();

            foreach (var raw in lines)
            {
                var line = NormalizeHeaderLine(raw);
                // Skip header lines like "Skin: XYZ"
                if (!string.IsNullOrEmpty(line) && line.StartsWith("Skin:", StringComparison.InvariantCultureIgnoreCase))
                {
                    var h = ExtractSkinName(line);
                    if (!string.IsNullOrEmpty(h)) currentSkinName = h;
                    continue;
                }

                // Match format: === TabName|Guid ===
                var match = Regex.Match(raw, @"^=== (.+)\|([a-f0-9\-]+) ===$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (currentTab != null)
                    {
                        currentTab.text = sb.ToString().TrimEnd();
                        currentTab.lastSaved = currentTab.text;
                        tabs.Add(currentTab);
                        sb.Length = 0;
                    }
                    string tabName = match.Groups[1].Value;
                    string tabGuid = match.Groups[2].Value;
                    currentTab = new NoteTab(tabGuid, tabName);
                }
                else
                {
                    // Fallback: old format without GUID: === TabName ===
                    var oldMatch = Regex.Match(raw, @"^=== (.+) ===$");
                    if (oldMatch.Success)
                    {
                        if (currentTab != null)
                        {
                            currentTab.text = sb.ToString().TrimEnd();
                            currentTab.lastSaved = currentTab.text;
                            tabs.Add(currentTab);
                            sb.Length = 0;
                        }
                        string tabName = oldMatch.Groups[1].Value;
                        currentTab = new NoteTab(tabName); // auto-generate new GUID
                    }
                    else if (currentTab != null)
                    {
                        sb.AppendLine(raw);
                    }
                }
            }
            if (currentTab != null)
            {
                currentTab.text = sb.ToString().TrimEnd();
                currentTab.lastSaved = currentTab.text;
                tabs.Add(currentTab);
            }
            if (tabs.Count == 0)
                tabs.Add(new NoteTab("Tab1"));
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote] notes read error from " + path + ": " + ex.Message);
            tabs.Clear();
            tabs.Add(new NoteTab("Tab1"));
            return false;
        }
    }

    // Allow external appending of imported tabs (merged, not replacing)
    public int AppendImportedTabs(List<NoteTab> imported, bool save = true)
    {
        if (imported == null || imported.Count == 0) return 0;
        int added = 0;
        foreach (var t in imported)
        {
            if (t == null) continue;
            // Create a fresh tab instance to avoid sharing GUI state like scroll/undo
            var nt = new NoteTab(string.IsNullOrEmpty(t.name) ? ("Tab " + (tabs.Count + 1)) : t.name);
            nt.text = t.text ?? string.Empty;
            nt.lastSaved = nt.text;
            nt.scroll = Vector2.zero;
            nt.undoStack = new Stack<string>();
            tabs.Add(nt);
            added++;
        }
        if (save) SaveNotes();
        return added;
    }

    // --- Skin helpers ---
    public void ApplySkinFromFolder(string skinName, string texturesFolder, bool persistHeader)
    {
        if (string.IsNullOrEmpty(skinName)) skinName = DEFAULT_SKIN_NAME;
        currentSkinName = skinName;
        try
        {
            ApplySkinInternalCore(skinName, texturesFolder);
            if (persistHeader)
            {
                try { UpdateSkinHeaderInNotes(notesPath, skinName); } catch { }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote] ApplySkinFromFolder failed: " + ex.Message);
        }
    }

    public void ApplyAndPersistSkin(string skinName)
    {
        if (string.IsNullOrEmpty(skinName)) skinName = DEFAULT_SKIN_NAME;
        currentSkinName = skinName;
        ApplySkinInternal(skinName);
        // Update header in notes file
        try { UpdateSkinHeaderInNotes(notesPath, skinName); } catch { }
    }

    private void ApplySkinInternal(string skinName)
    {
        try
        {
            string texturesFolder = GetTexturesFolderForSkin(skinName);
            ApplySkinInternalCore(skinName, texturesFolder);
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote] ApplySkinInternal failed: " + ex.Message);
        }
    }

    // Core that actually configures assets and reloads UI using an explicit folder (can be null -> fallback to Green/default)
    private void ApplySkinInternalCore(string skinName, string texturesFolder)
    {
        // Use Green as guaranteed fallback pack instead of Stock
        string greenFallback = GetTexturesFolderForSkin("Green");
        if (!Directory.Exists(greenFallback)) greenFallback = null;

        if (string.IsNullOrEmpty(texturesFolder) || !Directory.Exists(texturesFolder))
        {
            Debug.LogWarning("[KerbNote] Skin folder not found for '" + skinName + "': " + texturesFolder + ". Falling back to Green/default.");
        }
        // Configure resolver (current + Green fallback), DB-first mode
        SkinAssets.Configure(texturesFolder, greenFallback, fileOnly: false);

        // Trace which files are used
        try { Debug.Log("[KerbNote] Applying skin '" + skinName + "' from: " + SkinAssets.CurrentFolder + " (url=" + SkinAssets.CurrentUrl + "), fallback: " + SkinAssets.FallbackFolder + " (url=" + SkinAssets.FallbackUrl + ")"); } catch { }

        // Apply backgrounds via resolver
        var bgWin = SkinAssets.Get("BackgroundWindow");
        var noteWin = SkinAssets.Get("NoteWindow");
        KerbalUIBackground.OverrideWithTextures(bgWin, noteWin);

        // Reload all visuals and styles
        try { SkinReload.Reload(this); } catch { }

        // Refresh AppLauncher icon immediately
        try
        {
            iconOn = SkinAssets.Get("IconOn") ?? iconOn;
            iconOff = SkinAssets.Get("IconOff") ?? iconOff;
            if (AppButton != null)
            {
                AppButton.SetTexture(IsWindowVisible ? (iconOn ?? iconOff) : (iconOff ?? iconOn));
            }
        }
        catch { }

        // Reset cached styles so Calc/X/Mini rebuild with new textures immediately
        ForceRebuildStyles();

        // Notify listeners so other windows refresh (SliderWindow Alarm_bar, Settings styles)
        NotifySkinChanged(skinName);
    }

    private string GetTexturesFolderForSkin(string skinName)
    {
        try
        {
            string basePath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbNoteLite", "texture_pack");
            string direct = Path.Combine(basePath, skinName, "Textures");
            if (Directory.Exists(direct)) return direct;
            // Try case-insensitive search for matching pack name
            if (Directory.Exists(basePath))
            {
                var hit = Directory.GetDirectories(basePath)
                    .FirstOrDefault(d => string.Equals(Path.GetFileName(d), skinName, StringComparison.InvariantCultureIgnoreCase));
                if (!string.IsNullOrEmpty(hit))
                {
                    string alt = Path.Combine(hit, "Textures");
                    if (Directory.Exists(alt)) return alt;
                }
            }
            return direct;
        }
        catch { return null; }
    }

    private string ReadSkinHeader(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            using (var sr = new StreamReader(path))
            {
                for (int i = 0; i < 5; i++)
                {
                    var raw = sr.ReadLine();
                    if (raw == null) break;
                    var line = NormalizeHeaderLine(raw);
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith("Skin:", StringComparison.InvariantCultureIgnoreCase))
                        return ExtractSkinName(line);
                    // If the first non-empty line is not a skin header, stop scanning
                    if (!line.StartsWith("=== ")) break;
                }
            }
        }
        catch { }
        return null;
    }

    private static string NormalizeHeaderLine(string line)
    {
        if (line == null) return null;
        // Trim BOM and leading whitespace
        return line.TrimStart('\uFEFF', ' ', '\t');
    }

    private static string ExtractSkinName(string line)
    {
        try
        {
            if (line == null) return null;
            line = NormalizeHeaderLine(line);
            var idx = line.IndexOf(':');
            if (idx >= 0 && idx + 1 < line.Length)
            {
                var name = line.Substring(idx + 1).Trim();
                return string.IsNullOrEmpty(name) ? null : name;
            }
        }
        catch { }
        return null;
    }

    private void EnsureNotesHeaderExists(string path, string defaultSkin)
    {
        try
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "Skin: " + (string.IsNullOrEmpty(defaultSkin) ? DEFAULT_SKIN_NAME : defaultSkin) + Environment.NewLine + Environment.NewLine);
                return;
            }
            var first = ReadSkinHeader(path);
            if (!string.IsNullOrEmpty(first)) return; // already present
            // Insert header at top preserving content
            var existing = File.ReadAllText(path);
            var header = "Skin: " + (string.IsNullOrEmpty(defaultSkin) ? DEFAULT_SKIN_NAME : defaultSkin) + Environment.NewLine + Environment.NewLine;
            File.WriteAllText(path, header + existing);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KerbNote] EnsureNotesHeaderExists failed: " + ex.Message);
        }
    }

    public void UpdateNotesSkinHeader(string skinName)
    {
        currentSkinName = string.IsNullOrEmpty(skinName) ? DEFAULT_SKIN_NAME : skinName;
        try { UpdateSkinHeaderInNotes(notesPath, currentSkinName); } catch {}
    }

    private static void UpdateSkinHeaderInNotes(string path, string skinName)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "Skin: " + (string.IsNullOrEmpty(skinName) ? DEFAULT_SKIN_NAME : skinName) + Environment.NewLine + Environment.NewLine);
                return;
            }
            var lines = File.ReadAllLines(path).ToList();
            int headerIndex = -1;
            for (int i = 0; i < Math.Min(lines.Count, 5); i++)
            {
                var l = lines[i];
                if (string.IsNullOrEmpty(l)) continue;
                if (l.StartsWith("Skin:", StringComparison.InvariantCultureIgnoreCase)) { headerIndex = i; break; }
                if (l.StartsWith("=== ")) break; // first non-empty non-header means no header
            }
            if (headerIndex >= 0)
            {
                lines[headerIndex] = "Skin: " + (string.IsNullOrEmpty(skinName) ? DEFAULT_SKIN_NAME : skinName);
            }
            else
            {
                lines.Insert(0, "Skin: " + (string.IsNullOrEmpty(skinName) ? DEFAULT_SKIN_NAME : skinName));
                lines.Insert(1, string.Empty);
            }
            File.WriteAllLines(path, lines);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KerbNote] UpdateSkinHeaderInNotes failed: " + ex.Message);
        }
    }
}
