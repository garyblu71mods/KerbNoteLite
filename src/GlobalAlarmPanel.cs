using System.Linq;
using UnityEngine;

public class GlobalAlarmPanel : MonoBehaviour
{
    public Rect mainWindowRect = new Rect(100, 100, 500, 400);
    private Rect sliderRect = new Rect(0, 0, 300, 400);
    private bool isVisible = false;
    private float slideSpeed = 12f;

    public float minDistanceFromEdge = 20f;
    public bool allowDragging = false;
    public float buttonOffset = 0f;

    private float buttonWidth = 20f;
    private float buttonHeight = 300f;
    private Texture2D alarmBarTexture;
    private Texture2D alarmBarTextureFlipped;
    private float barFixedWidth = 18f;

    private float slideT = 0f;
    private const float SNAP_EPSILON = 0.001f;

    private float hiddenPanelX;
    private float visiblePanelX;

    private Vector2 screenSize;
    private KerbNote mainWindow;
    private bool isModActive = false;

    private int panelWindowID;
    private int buttonWindowID;

    private const string TEXTURE_ALARM_BAR = "KerbNoteLite/Textures/Alarm_bar";

    private const string INPUT_LOCK_ID = "KerbNote_GlobalAlarm_ClickBlock";
    private bool inputLockSet = false;
    private Rect lastPanelWinRect;
    private Rect lastButtonWinRect;

    private static bool anyMiniNoteVisible = false;
    public static void NotifyMiniNoteVisibilityChanged(bool visible)
    {
        anyMiniNoteVisible = visible;
    }

    private GUIStyle cachedAlarmButtonStyle;

    // Menu/page state
    private enum Page { Menu, Resources, Terrain, Reminder }
    private Page currentPage = Page.Menu;

    // Styles and textures for menu buttons (Tab style)
    private bool stylesInitialized;
    private GUIStyle titleStyle;
    private GUIStyle tabBtnStyle;
    private Texture2D tabTex, tabHoverTex, tabClickTex;

    // Runners references
    private ResourcesAlarmRunner resourcesRunner;
    private TerrainAlarmRunner terrainRunner;

    // Reminder input state (Kerbin calendar)
    private int remYear = 1, remDay = 1, remHour = 0, remMinute = 0;
    private string remNote = string.Empty;

    private bool _wasModActive;
    private bool _forceHiddenThisFrame;

    // Force panel to hidden state immediately (no animation)
    public void ResetHiddenImmediate()
    {
        isVisible = false;
        slideT = 0f;
        UpdateSizesAndAnchors();
        sliderRect.x = hiddenPanelX;
        ClearInputLock();
    }

    void Start()
    {
        if (mainWindow == null)
            mainWindow = FindObjectOfType<KerbNote>();

        if (mainWindow == null)
        {
            Debug.LogError("[GlobalAlarmPanel] Nie znaleziono glównego okna KerbNote!");
            enabled = false;
            return;
        }

        mainWindowRect = mainWindow.WindowRect;
        screenSize = new Vector2(Screen.width, Screen.height);

        KerbalUIBackground.LoadTexture();
        ReloadAlarmBarTexture();
        if (alarmBarTexture == null)
        {
            Debug.LogWarning("[GlobalAlarmPanel] Alarm_bar texture not found, using fallback sizes.");
        }

        if (alarmBarTexture != null && alarmBarTexture.width > 0)
        {
            barFixedWidth = Mathf.Clamp(alarmBarTexture.width, 8f, 64f);
        }
        else
        {
            barFixedWidth = 18f;
        }

        buttonHeight = 300f;
        isVisible = false;
        slideT = 0f;

        sliderRect.width = Mathf.Round(sliderRect.width * 0.75f);

        panelWindowID = GetInstanceID() ^ 0x5A5A5A5A;
        buttonWindowID = GetInstanceID() ^ 0x3C3C3C3C;

        UpdateSizesAndAnchors();
        // Start hidden (left, off-screen)
        sliderRect.x = hiddenPanelX;
        _wasModActive = false;
    }

    private void EnsureStyles()
    {
        if (stylesInitialized) return;
        stylesInitialized = true;
        GUI.skin = HighLogic.Skin;
        // Header style similar to AlarmSelector
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        titleStyle.normal.textColor = new Color(0.9f, 0.85f, 0.4f, 1f);

        // Load tab textures for buttons
        tabTex = SkinAssets.Get("Tab") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB, false);
        tabHoverTex = SkinAssets.Get("TabHover") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB_HOVER, false);
        tabClickTex = SkinAssets.Get("TabClick") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB_CLICK, false);

        tabBtnStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            wordWrap = false,
            fixedHeight = 24f,
            padding = new RectOffset(12, 12, 2, 2),
            margin = new RectOffset(6, 6, 4, 4)
        };
        tabBtnStyle.normal.background = tabTex;
        tabBtnStyle.hover.background = tabHoverTex ?? tabTex;
        tabBtnStyle.active.background = tabClickTex ?? tabTex;
        tabBtnStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f, 0.95f);
        tabBtnStyle.hover.textColor = Color.white;
        tabBtnStyle.active.textColor = Color.white;
    }

    public void ReloadAlarmBarTexture()
    {
        alarmBarTexture = SkinAssets.Get("Alarm_bar");
        if (alarmBarTexture == null)
        {
            alarmBarTexture = SkinAssets.Get("alarm_bar") ?? GameDatabase.Instance.GetTexture(TEXTURE_ALARM_BAR, false);
        }
        if (alarmBarTexture != null)
        {
            if (alarmBarTextureFlipped != null)
                Destroy(alarmBarTextureFlipped);
            alarmBarTextureFlipped = FlipTexture180(alarmBarTexture);
        }
    }

    private Texture2D FlipTexture180(Texture2D original)
    {
        if (original == null) return null;
        try
        {
            int width = original.width;
            int height = original.height;
            Color[] pixels = original.GetPixels();
            Color[] rotated = new Color[pixels.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int srcIdx = y * width + x;
                    int dstX = width - 1 - x;
                    int dstY = height - 1 - y;
                    int dstIdx = dstY * width + dstX;
                    rotated[dstIdx] = pixels[srcIdx];
                }
            }
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.SetPixels(rotated);
            result.Apply();
            result.filterMode = FilterMode.Bilinear;
            result.wrapMode = TextureWrapMode.Clamp;
            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[GlobalAlarmPanel] FlipTexture180 error: " + ex.Message);
            return original;
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

        mainWindowRect = mainWindow.WindowRect;
        isModActive = mainWindow.IsWindowVisible;

        _forceHiddenThisFrame = false;

        // If the mod window just became visible, force hidden immediately so we never show a "closing" animation.
        if (isModActive && !_wasModActive)
        {
            ResetHiddenImmediate();
            _forceHiddenThisFrame = true;
        }
        _wasModActive = isModActive;

        if (!isModActive)
        {
            ResetHiddenImmediate();
            return;
        }

        UpdateSizesAndAnchors();

        if (screenSize.x != Screen.width || screenSize.y != Screen.height)
        {
            screenSize = new Vector2(Screen.width, Screen.height);
        }

        // If the panel behaves inverted (shown when it should be hidden), flip the mapping.
        float targetT = isVisible ? 1f : 0f;
        float nextT = Mathf.Lerp(slideT, targetT, Time.deltaTime * slideSpeed);
        if (Mathf.Abs(nextT - targetT) < 0.0005f) nextT = targetT;
        slideT = nextT;

        UpdateAnchoredPosition();
    }

    private void UpdateSizesAndAnchors()
    {
        float reducedHeight = Mathf.Max(0f, mainWindowRect.height - 35f);
        sliderRect.height = reducedHeight;
        sliderRect.y = mainWindowRect.y + (mainWindowRect.height - reducedHeight) / 2f;
        buttonWidth = barFixedWidth;
        // Left side mirror of SliderWindow
        hiddenPanelX = mainWindowRect.x;                          // hidden: aligned with window edge (no protrusion)
        visiblePanelX = mainWindowRect.x - sliderRect.width;      // visible: shifted left so full panel protrudes
    }

    private void UpdateAnchoredPosition()
    {
        sliderRect.x = Mathf.Lerp(hiddenPanelX, visiblePanelX, slideT);
    }

    void OnGUI()
    {
        // Safety: OnGUI can run before Update on some frames.
        // Only force-hide on the visibility RISING EDGE, not every frame.
        bool modNowVisible = (mainWindow != null && mainWindow.IsWindowVisible);
        if (modNowVisible && !_wasModActive)
        {
            ResetHiddenImmediate();
            _forceHiddenThisFrame = true;
        }
        _wasModActive = modNowVisible;
        isModActive = modNowVisible;

        if (!isModActive) { ClearInputLock(); return; }
        if (SettingWindow.IsAboutVisible) { ClearInputLock(); return; }

        Rect currentMainRect = mainWindow.WindowRect;
        if (currentMainRect.x != mainWindowRect.x || currentMainRect.y != mainWindowRect.y ||
            currentMainRect.width != mainWindowRect.width || currentMainRect.height != mainWindowRect.height)
        {
            mainWindowRect = currentMainRect;
            UpdateSizesAndAnchors();
            UpdateAnchoredPosition();
        }

        // If we just force-hid due to mod opening, don't draw the slice in this same GUI pass.
        if (_forceHiddenThisFrame)
        {
            ClearInputLock();
            return;
        }

        // Mirror of SliderWindow: visible width of the part that sticks OUT to THE LEFT of the main window
        // Use geometry only; visibility is handled by slideT/isVisible driving sliderRect.x in Update.
        float protrusion = Mathf.Clamp(mainWindowRect.x - sliderRect.x, 0f, sliderRect.width);

        // Draw panel window as a clipped slice only in the protruding area (same idea as SliderWindow)
        if (protrusion > SNAP_EPSILON)
        {
            Rect panelWinRect = new Rect(sliderRect.x, sliderRect.y, protrusion, sliderRect.height);
            lastPanelWinRect = panelWinRect;
            GUI.Window(panelWindowID, panelWinRect, DrawPanelWindow, string.Empty, GUIStyle.none);
        }
        else
        {
            lastPanelWinRect = Rect.zero;
        }

        // Draw Alarm_bar button to the LEFT of the panel/window edge
        float btnX = (mainWindowRect.x - protrusion) - buttonWidth;
        float btnY = mainWindowRect.y + (mainWindowRect.height - buttonHeight) / 2f + buttonOffset;
        Rect btnWinRect = new Rect(btnX, btnY, buttonWidth, buttonHeight);
        lastButtonWinRect = btnWinRect;
        GUI.Window(buttonWindowID, btnWinRect, DrawAlarmBarWindow, string.Empty, GUIStyle.none);

        if (!anyMiniNoteVisible)
        {
            GUI.BringWindowToFront(panelWindowID);
            GUI.BringWindowToFront(buttonWindowID);
        }

        UpdateInputLock();
    }

    private void DrawPanelWindow(int id)
    {
        EnsureStyles();
        // Mirror of SliderWindow clipping: show the LEFT edge last (clip from left side)
        float protrusion = Mathf.Clamp(mainWindowRect.x - sliderRect.x, 0f, sliderRect.width);
        float localX = 0f;

        KerbalUIBackground.DrawNoteWindow(new Rect(localX, 0, sliderRect.width, sliderRect.height));

        GUI.Label(new Rect(localX + 8f, 6f, sliderRect.width - 16f, 20f), "Global Alarms", titleStyle);

        float y = 28f;
        Rect contentArea = new Rect(localX + 8f, y, sliderRect.width - 16f, sliderRect.height - y - 8f);
        GUILayout.BeginArea(contentArea);
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Width(contentArea.width), GUILayout.Height(contentArea.height));
        switch (currentPage)
        {
            case Page.Menu:
                DrawMainMenu();
                break;
            case Page.Resources:
                DrawResourcesPage();
                break;
            case Page.Terrain:
                DrawTerrainPage();
                break;
            case Page.Reminder:
                DrawReminderPage();
                break;
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        var e = Event.current;
        if (e != null)
        {
            if (e.isMouse || e.type == EventType.ScrollWheel)
            {
                e.Use();
            }
        }
    }

    private Vector2 _scroll = Vector2.zero;

    private void DrawMainMenu()
    {
        GUILayout.Space(4f);
        if (GUILayout.Button("Resources", tabBtnStyle))
        {
            currentPage = Page.Resources;
            if (resourcesRunner == null)
            {
                var go = new GameObject("ResourcesAlarmRunner");
                resourcesRunner = go.AddComponent<ResourcesAlarmRunner>();
                DontDestroyOnLoad(go);
            }
        }
        if (GUILayout.Button("Terrain alarm", tabBtnStyle))
        {
            currentPage = Page.Terrain;
            if (terrainRunner == null)
            {
                var go = new GameObject("TerrainAlarmRunner");
                terrainRunner = go.AddComponent<TerrainAlarmRunner>();
                DontDestroyOnLoad(go);
                TerrainAlarmConfig.LoadInto(terrainRunner);
            }

            // refresh input buffers from runner so fields show immediately
            _terrainAglStr = null;
            _terrainVsStr = null;
            _gearAglStr = null;
            _gearMaxSpdStr = null;
            _aheadMarginStr = null;
            _aheadMaxTimeStr = null;
            _aheadStepStr = null;
            _aheadMinSpeedStr = null;
        }
        if (GUILayout.Button("Time reminder", tabBtnStyle)) currentPage = Page.Reminder;
    }

    private void DrawResourcesPage()
    {
        GUILayout.Label("Resources alarms", HighLogic.Skin.label);
        GUILayout.Space(4f);
        bool enabled = (resourcesRunner != null && resourcesRunner.enabled);
        string label = enabled ? "Disable" : "Enable";
        if (GUILayout.Button(label, tabBtnStyle))
        {
            if (!enabled)
            {
                if (resourcesRunner == null)
                {
                    var go = new GameObject("ResourcesAlarmRunner");
                    resourcesRunner = go.AddComponent<ResourcesAlarmRunner>();
                    DontDestroyOnLoad(go);
                }
                resourcesRunner.Enable();
            }
            else
            {
                resourcesRunner.Disable();
            }
        }
        GUILayout.Space(8f);
        // Only show resources when in flight and vessel active
        if (!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel == null)
        {
            GUILayout.Label("Available in Flight when a vessel is active.", HighLogic.Skin.label);
        }
        else
        {
            string[] names = resourcesRunner != null ? resourcesRunner.GetCurrentVesselResourceNames() : new string[] { };
            float contentW = GUILayoutUtility.GetLastRect().width;
            foreach (var n in names.OrderBy(s => s))
            {
                bool isOn = resourcesRunner != null && resourcesRunner.EnabledResources.Contains(n);
                float cur = 0.15f;
                if (resourcesRunner != null)
                {
                    float v; if (!resourcesRunner.ThresholdByResource.TryGetValue(n, out v)) v = 0.15f; cur = v;
                }
                if (contentW >= 360f)
                {
                    // Single line layout
                    GUILayout.BeginHorizontal();
                    bool newOn = GUILayout.Toggle(isOn, "", GUILayout.Width(24));
                    if (resourcesRunner != null)
                    {
                        if (newOn) resourcesRunner.EnabledResources.Add(n); else resourcesRunner.EnabledResources.Remove(n);
                    }
                    GUILayout.Label(n, GUILayout.Width(150));
                    float newThr = GUILayout.HorizontalSlider(cur, 0.01f, 0.8f, GUILayout.Width(180));
                    GUILayout.Label(((int)(newThr*100)).ToString() + "%", GUILayout.Width(45));
                    if (resourcesRunner != null) resourcesRunner.ThresholdByResource[n] = newThr;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    // Two-line compact layout (fits narrow panels)
                    GUILayout.BeginHorizontal();
                    bool newOn = GUILayout.Toggle(isOn, "", GUILayout.Width(24));
                    if (resourcesRunner != null)
                    {
                        if (newOn) resourcesRunner.EnabledResources.Add(n); else resourcesRunner.EnabledResources.Remove(n);
                    }
                    GUILayout.Label(n);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    float newThr = GUILayout.HorizontalSlider(cur, 0.01f, 0.8f);
                    GUILayout.Label(((int)(newThr*100)).ToString() + "%", GUILayout.Width(45));
                    if (resourcesRunner != null) resourcesRunner.ThresholdByResource[n] = newThr;
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2f);
                }
            }
        }
        GUILayout.Space(8f);
        if (GUILayout.Button("Back", tabBtnStyle)) currentPage = Page.Menu;
    }

    // Terrain UI input buffers (prevent TextField from being overwritten every frame)
    private string _terrainAglStr;
    private string _terrainVsStr;
    private string _gearAglStr;
    private string _gearMaxSpdStr;
    private string _aheadMarginStr;
    private string _aheadMaxTimeStr;
    private string _aheadStepStr;
    private string _aheadMinSpeedStr;
    private bool _sinkRateToggle;
    private float _lastTerrainConfigSaveTs;

    private void DrawTerrainPage()
    {
        GUILayout.Label("Terrain alarm", HighLogic.Skin.label);
        GUILayout.Space(4f);
        bool enabled = (terrainRunner != null && terrainRunner.enabled);
        string label = enabled ? "Disable" : "Enable";
        if (GUILayout.Button(label, tabBtnStyle))
        {
            if (!enabled)
            {
                if (terrainRunner == null)
                {
                    var go = new GameObject("TerrainAlarmRunner");
                    terrainRunner = go.AddComponent<TerrainAlarmRunner>();
                    DontDestroyOnLoad(go);

                    TerrainAlarmConfig.LoadInto(terrainRunner);
                }

                terrainRunner.enabled = true;
                terrainRunner.Enable();

                // persist enabled state immediately so it survives restart
                TerrainAlarmConfig.SaveFrom(terrainRunner);

                _terrainAglStr = null;
                _terrainVsStr = null;
                _gearAglStr = null;
                _gearMaxSpdStr = null;
                _aheadMarginStr = null;
                _aheadMaxTimeStr = null;
                _aheadStepStr = null;
                _aheadMinSpeedStr = null;
                _sinkRateToggle = terrainRunner.EnableSinkRate;
            }
            else
            {
                terrainRunner.Disable();
                terrainRunner.enabled = false;
                TerrainAlarmConfig.SaveFrom(terrainRunner);
            }
        }
        GUILayout.Space(6f);

        if (terrainRunner == null)
        {
            if (GUILayout.Button("Back", tabBtnStyle)) currentPage = Page.Menu;
            return;
        }

        if (_terrainAglStr == null) _terrainAglStr = terrainRunner.AltitudeAGL.ToString("F0");
        if (_terrainVsStr == null) _terrainVsStr = terrainRunner.DescentSpeed.ToString("F0");

        if (_gearAglStr == null) _gearAglStr = terrainRunner.GearAlarmAGL.ToString("F0");
        if (_gearMaxSpdStr == null) _gearMaxSpdStr = terrainRunner.GearAlarmMaxSpeed.ToString("F0");

        if (_aheadMarginStr == null) _aheadMarginStr = terrainRunner.TerrainAheadMarginMeters.ToString("F0");
        if (_aheadMaxTimeStr == null) _aheadMaxTimeStr = terrainRunner.TerrainAheadMaxTime.ToString("F1");
        if (_aheadStepStr == null) _aheadStepStr = terrainRunner.TerrainAheadStep.ToString("F2");
        if (_aheadMinSpeedStr == null) _aheadMinSpeedStr = terrainRunner.TerrainAheadMinSpeed.ToString("F0");

        // Base terrain threshold (always relevant)
        terrainRunner.EnableTerrainBase = GUILayout.Toggle(terrainRunner.EnableTerrainBase, " Terrain warning (Pull Up)");

        GUILayout.BeginHorizontal();
        GUILayout.Label("AGL:", GUILayout.Width(50));
        _terrainAglStr = GUILayout.TextField(_terrainAglStr, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("VSp:", GUILayout.Width(50));
        _terrainVsStr = GUILayout.TextField(_terrainVsStr, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);

        // Counting out
        terrainRunner.EnableAltitudeCallouts = GUILayout.Toggle(terrainRunner.EnableAltitudeCallouts, " Counting out");

        GUILayout.Space(6f);

        // Sink rate
        terrainRunner.EnableSinkRate = GUILayout.Toggle(terrainRunner.EnableSinkRate, " Sink rate alarm");

        GUILayout.Space(6f);

        // Gear
        bool newGearOn = GUILayout.Toggle(terrainRunner.EnableGearAlarm, " Gear not deployed");
        if (newGearOn)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("GearAGL:", GUILayout.Width(70));
            _gearAglStr = GUILayout.TextField(_gearAglStr, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MaxSpd:", GUILayout.Width(70));
            _gearMaxSpdStr = GUILayout.TextField(_gearMaxSpdStr, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(6f);

        // Terrain ahead
        bool newAhead = GUILayout.Toggle(terrainRunner.EnableTerrainAhead, " Terrain ahead (TTI)");
        if (newAhead)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("MaxTime(s):", GUILayout.Width(90));
            _aheadMaxTimeStr = GUILayout.TextField(_aheadMaxTimeStr, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Step(s):", GUILayout.Width(90));
            _aheadStepStr = GUILayout.TextField(_aheadStepStr, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Margin(m):", GUILayout.Width(90));
            _aheadMarginStr = GUILayout.TextField(_aheadMarginStr, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MinSpd:", GUILayout.Width(90));
            _aheadMinSpeedStr = GUILayout.TextField(_aheadMinSpeedStr, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        // Apply parsed settings
        terrainRunner.EnableGearAlarm = newGearOn;
        terrainRunner.EnableTerrainAhead = newAhead;

        if (float.TryParse(_terrainAglStr, out var agl)) terrainRunner.AltitudeAGL = Mathf.Max(0f, agl);
        if (float.TryParse(_terrainVsStr, out var vs)) terrainRunner.DescentSpeed = vs;

        if (float.TryParse(_gearAglStr, out var gearAgl)) terrainRunner.GearAlarmAGL = Mathf.Clamp(gearAgl, 0f, 2000f);
        if (float.TryParse(_gearMaxSpdStr, out var gearMaxSpd)) terrainRunner.GearAlarmMaxSpeed = Mathf.Clamp(gearMaxSpd, 0f, 2000f);

        if (float.TryParse(_aheadMarginStr, out var margin)) terrainRunner.TerrainAheadMarginMeters = Mathf.Clamp(margin, 0f, 5000f);
        if (float.TryParse(_aheadMaxTimeStr, out var maxTime)) terrainRunner.TerrainAheadMaxTime = Mathf.Clamp(maxTime, 0.5f, 30f);
        if (float.TryParse(_aheadStepStr, out var step)) terrainRunner.TerrainAheadStep = Mathf.Clamp(step, 0.05f, 2f);
        if (float.TryParse(_aheadMinSpeedStr, out var minSpd)) terrainRunner.TerrainAheadMinSpeed = Mathf.Clamp(minSpd, 0f, 2000f);

        // Persist settings (debounced)
        if (Time.realtimeSinceStartup - _lastTerrainConfigSaveTs > 0.5f)
        {
            _lastTerrainConfigSaveTs = Time.realtimeSinceStartup;
            TerrainAlarmConfig.SaveFrom(terrainRunner);
        }

        // Bottom help (short)
        var wrap = new GUIStyle(HighLogic.Skin.label) { wordWrap = true };
        GUILayout.Space(6f);
        GUILayout.Label("[ ] Sink rate alarm: gear down + low AGL + high descent.", wrap);
        GUILayout.Label("[ ] Counting out: callouts at 50/40/30/20/10m.", wrap);
        GUILayout.Label("[ ] Gear not deployed: warns below GearAGL without gear.", wrap);
        GUILayout.Label("[ ] Terrain ahead (TTI): straight-line impact prediction.", wrap);

        GUILayout.Space(6f);
        if (GUILayout.Button("Back", tabBtnStyle)) currentPage = Page.Menu;
    }

    private void DrawReminderPage()
    {
        GUILayout.Label("Time reminder (Kerbin calendar)", HighLogic.Skin.label);
        GUILayout.Space(4f);
        // Use compact layout to avoid overflow; put inputs on separate lines
        GUILayout.BeginHorizontal();
        GUILayout.Label("Year:", GUILayout.Width(40));
        remYear = ClampParseInt(GUILayout.TextField(remYear.ToString(), GUILayout.Width(50)), 1, 9999, remYear);
        GUILayout.Label("Day:", GUILayout.Width(40));
        remDay = ClampParseInt(GUILayout.TextField(remDay.ToString(), GUILayout.Width(50)), 1, 426, remDay);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Hour:", GUILayout.Width(45));
        remHour = ClampParseInt(GUILayout.TextField(remHour.ToString(), GUILayout.Width(50)), 0, 5, remHour);
        GUILayout.Label("Min:", GUILayout.Width(35));
        remMinute = ClampParseInt(GUILayout.TextField(remMinute.ToString(), GUILayout.Width(50)), 0, 59, remMinute);
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
        GUILayout.Label("Note:", GUILayout.Width(45));
        remNote = GUILayout.TextArea(remNote ?? string.Empty, GUILayout.MinHeight(48));
        GUILayout.Space(6f);
        if (GUILayout.Button("Set reminder", tabBtnStyle))
        {
            var go = new GameObject("TimeReminderAlarm");
            var r = go.AddComponent<TimeReminderAlarm>();
            DontDestroyOnLoad(go);
            r.ScheduleKerbin(remYear, remDay, remHour, remMinute, remNote);
        }
        if (GUILayout.Button("Back", tabBtnStyle)) currentPage = Page.Menu;
    }

    private void DrawAlarmBarWindow(int id)
    {
        if (cachedAlarmButtonStyle == null || alarmBarTextureFlipped != cachedAlarmButtonStyle.normal.background)
        {
            cachedAlarmButtonStyle = new GUIStyle(GUI.skin.button);
            if (alarmBarTextureFlipped != null)
            {
                cachedAlarmButtonStyle.normal.background = alarmBarTextureFlipped;
                cachedAlarmButtonStyle.hover.background = alarmBarTextureFlipped;
                cachedAlarmButtonStyle.active.background = alarmBarTextureFlipped;
                cachedAlarmButtonStyle.border = new RectOffset(0, 0, 0, 0);
                cachedAlarmButtonStyle.padding = new RectOffset(0, 0, 0, 0);
            }
        }

        if (GUI.Button(new Rect(0, 0, buttonWidth, buttonHeight), GUIContent.none, cachedAlarmButtonStyle))
        {
            isVisible = !isVisible;
        }

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
        KerbalUIBackground.DrawNoteWindow(new Rect(localX, 0, sliderRect.width, sliderRect.height));
        // content drawn in DrawPanelWindow
    }

    private GUIStyle CreateHeaderStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(0.9f, 0.7f, 0.2f);
        return style;
    }

    private GUIStyle CreateContentStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.wordWrap = true;
        style.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        return style;
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
        ReloadAlarmBarTexture();
        stylesInitialized = false; // rebuild styles with new textures
        if (alarmBarTexture != null && alarmBarTexture.width > 0)
        {
            barFixedWidth = Mathf.Clamp(alarmBarTexture.width, 8f, 64f);
        }
        else
        {
            barFixedWidth = 18f;
        }
        cachedAlarmButtonStyle = null;
    }

    public void ShowPanelImmediate()
    {
        isVisible = true;
        slideT = 1f;
        UpdateSizesAndAnchors();
        UpdateAnchoredPosition();
    }

    public void Toggle()
    {
        isVisible = !isVisible;
    }

    public void Show()
    {
        isVisible = true;
    }

    public void Hide()
    {
        isVisible = false;
    }

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
            try { InputLockManager.RemoveControlLock(INPUT_LOCK_ID); }
            catch { }
            inputLockSet = false;
        }
    }

    void OnDestroy()
    {
        if (alarmBarTextureFlipped != null)
        {
            Destroy(alarmBarTextureFlipped);
        }
    }

    private int ClampParseInt(string s, int min, int max, int fallback)
    {
        int v; if (!int.TryParse(s, out v)) v = fallback; return Mathf.Clamp(v, min, max);
    }
}
