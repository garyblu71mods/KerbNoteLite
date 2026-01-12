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
        
        // Find existing alarm runners (created by AlarmSystemBootstrap) to avoid creating duplicates
        if (resourcesRunner == null)
        {
            resourcesRunner = FindObjectOfType<ResourcesAlarmRunner>();
        }
        if (terrainRunner == null)
        {
            terrainRunner = FindObjectOfType<TerrainAlarmRunner>();
        }
    }

    private void EnsureStyles()
    {
        if (stylesInitialized) return;
        stylesInitialized = true;
        GUI.skin = HighLogic.Skin;
        
        // Header style - use white/cream color like resource labels
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        titleStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f, 1f); // White/cream instead of yellow-green

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

        if (!isModActive) { ClearInputLock(); TooltipManager.ResetTooltip(); return; }
        if (SettingWindow.IsAboutVisible) { ClearInputLock(); TooltipManager.ResetTooltip(); return; }

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
            TooltipManager.ResetTooltip();
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
        
        // Draw tooltips last (on top of everything)
        TooltipManager.DrawTooltip();
    }

    private void DrawPanelWindow(int id)
    {
        EnsureStyles();
        // Mirror of SliderWindow clipping: show the LEFT edge last (clip from left side)
        float protrusion = Mathf.Clamp(mainWindowRect.x - sliderRect.x, 0f, sliderRect.width);
        float localX = 0f;

        KerbalUIBackground.DrawPanel(new Rect(localX, 0, sliderRect.width, sliderRect.height));

        float y = 8f; // Start content from top (no header needed)
        // Increase right margin to accommodate thin scrollbar (8px scrollbar + 8px padding = 16px total)
        Rect contentArea = new Rect(localX + 8f, y, sliderRect.width - 24f, sliderRect.height - y - 8f);
        
        // Custom scrollbar style - 2x narrower and aligned to right
        GUIStyle thinScrollbar = new GUIStyle(GUI.skin.verticalScrollbar);
        thinScrollbar.fixedWidth = 8f; // Half of default ~16px
        
        GUIStyle thinScrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb);
        thinScrollbarThumb.fixedWidth = 8f;
        
        GUILayout.BeginArea(contentArea);
        _scroll = GUILayout.BeginScrollView(_scroll, false, true, GUIStyle.none, thinScrollbar, GUI.skin.scrollView, 
                                           GUILayout.Width(contentArea.width), GUILayout.Height(contentArea.height));
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
                // First try to find existing runner (created by AlarmSystemBootstrap)
                resourcesRunner = FindObjectOfType<ResourcesAlarmRunner>();
                
                // If not found, create new one
                if (resourcesRunner == null)
                {
                    var go = new GameObject("ResourcesAlarmRunner");
                    resourcesRunner = go.AddComponent<ResourcesAlarmRunner>();
                    DontDestroyOnLoad(go);
                    ResourcesAlarmConfig.LoadInto(resourcesRunner);
                }
            }
        }
        if (GUILayout.Button("Terrain alarm", tabBtnStyle))
        {
            currentPage = Page.Terrain;
            if (terrainRunner == null)
            {
                // First try to find existing runner (created by AlarmSystemBootstrap)
                terrainRunner = FindObjectOfType<TerrainAlarmRunner>();
                
                // If not found, create new one
                if (terrainRunner == null)
                {
                    var go = new GameObject("TerrainAlarmRunner");
                    terrainRunner = go.AddComponent<TerrainAlarmRunner>();
                    DontDestroyOnLoad(go);
                    TerrainAlarmConfig.LoadInto(terrainRunner);
                }
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
        
        // TODO: Time reminder feature - hidden for now, will be developed in next version
        // if (GUILayout.Button("Time reminder", tabBtnStyle)) currentPage = Page.Reminder;
    }

    private void DrawResourcesPage()
    {
        // Back button at the top
        if (GUILayout.Button("Back", tabBtnStyle)) 
        {
            currentPage = Page.Menu;
            TooltipManager.ResetTooltip();
            return;
        }
        
        GUILayout.Space(4f);
        bool enabled = (resourcesRunner != null && resourcesRunner.enabled);
        string label = enabled ? "Disable" : "Enable";
        if (GUILayout.Button(label, tabBtnStyle))
        {
            if (!enabled)
            {
                if (resourcesRunner == null)
                {
                    // First try to find existing runner (created by AlarmSystemBootstrap)
                    resourcesRunner = FindObjectOfType<ResourcesAlarmRunner>();
                    
                    // If not found, create new one
                    if (resourcesRunner == null)
                    {
                        var go = new GameObject("ResourcesAlarmRunner");
                        resourcesRunner = go.AddComponent<ResourcesAlarmRunner>();
                        DontDestroyOnLoad(go);
                        ResourcesAlarmConfig.LoadInto(resourcesRunner);
                    }
                }
                resourcesRunner.Enable();
                ResourcesAlarmConfig.SaveFrom(resourcesRunner); // Save immediately
            }
            else
            {
                resourcesRunner.Disable();
                ResourcesAlarmConfig.SaveFrom(resourcesRunner); // Save immediately
            }
        }
        GUILayout.Space(8f);
        
        // White/cream text style for all labels
        GUIStyle textStyle = new GUIStyle(HighLogic.Skin.label);
        textStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        
        // Only show resources when in flight and vessel active
        if (!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel == null)
        {
            GUILayout.Label("Resource alarms available in Flight when a vessel is active.", textStyle);
        }
        else
        {
            // Style for resource labels: no wrapping, clip overflow, white/cream color
            GUIStyle resourceLabelStyle = new GUIStyle(GUI.skin.label);
            resourceLabelStyle.wordWrap = false;
            resourceLabelStyle.clipping = TextClipping.Clip;
            resourceLabelStyle.alignment = TextAnchor.MiddleLeft;
            resourceLabelStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            
            // Header style - smaller font for compact headers
            GUIStyle headerLabelStyle = new GUIStyle(resourceLabelStyle);
            headerLabelStyle.fontSize = 9; // Smaller font for headers
            
            // Header with column labels (Active first, then Mute)
            GUILayout.BeginHorizontal(GUILayout.Height(20));
            GUILayout.Label("Active", headerLabelStyle, GUILayout.Width(25)); // Active first
            GUILayout.Label("Mute", headerLabelStyle, GUILayout.Width(25));   // Mute second
            GUILayout.Label("Resource", resourceLabelStyle, GUILayout.Width(55));
            GUILayout.Label("Value", resourceLabelStyle, GUILayout.Width(35));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(2f);
            
            // Communication alarm as first resource-like entry
            bool commAlarm = resourcesRunner != null && resourcesRunner.EnableCommAlarm;
            float commThreshold = resourcesRunner != null ? resourcesRunner.CommSignalThreshold : 0.25f;
            bool isCommExpanded = (_expandedResourceSlider == "Communication");
            
            if (!isCommExpanded)
            {
                // Compact line: active (20) + mute (20) + label (55) + value (35) + Set (30)
                GUILayout.BeginHorizontal(GUILayout.Height(20));
                
                // Active checkbox first
                bool newCommAlarm = GUILayout.Toggle(commAlarm, "", GUILayout.Width(20));
                if (Event.current.type == EventType.Repaint)
                    TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "comm_alarm");
                
                if (resourcesRunner != null)
                {
                    resourcesRunner.EnableCommAlarm = newCommAlarm;
                    if (newCommAlarm != commAlarm)
                    {
                        ResourcesAlarmConfig.SaveFrom(resourcesRunner);
                    }
                }
                
                // Mute checkbox second
                bool commMuted = resourcesRunner != null && resourcesRunner.MuteCommAlarm;
                bool newCommMuted = GUILayout.Toggle(commMuted, "", GUILayout.Width(20));
                if (Event.current.type == EventType.Repaint)
                    TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "resource_mute");
                
                if (resourcesRunner != null && commMuted != newCommMuted)
                {
                    resourcesRunner.MuteCommAlarm = newCommMuted;
                    ResourcesAlarmConfig.SaveFrom(resourcesRunner);
                }
                
                GUILayout.Label("Communic..", resourceLabelStyle, GUILayout.Width(55));
                GUILayout.Label(((int)(commThreshold * 100)).ToString() + "%", resourceLabelStyle, GUILayout.Width(35));
                
                if (GUILayout.Button("Set", GUILayout.Width(30)))
                {
                    _expandedResourceSlider = "Communication";
                }
                
                GUILayout.EndHorizontal();
            }
            else
            {
                // Expanded slider view: full-width slider with OK button
                GUILayout.BeginVertical();
                GUILayout.Label($"Communication: {(int)(commThreshold * 100)}%", textStyle);
                GUILayout.BeginHorizontal();
                float newThreshold = GUILayout.HorizontalSlider(commThreshold, 0.05f, 0.75f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                    TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "comm_threshold");
                
                if (resourcesRunner != null && Mathf.Abs(newThreshold - commThreshold) > 0.01f)
                {
                    resourcesRunner.CommSignalThreshold = newThreshold;
                    ResourcesAlarmConfig.SaveFrom(resourcesRunner);
                }
                if (GUILayout.Button("OK", GUILayout.Width(40)))
                {
                    _expandedResourceSlider = null;
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(2f);
            }
            
            // Delta-V alarm
            bool deltaVAlarm = resourcesRunner != null && resourcesRunner.EnableDeltaVAlarm;
            float deltaVThreshold = resourcesRunner != null ? resourcesRunner.DeltaVThreshold : 100f;
            bool isDeltaVExpanded = (_expandedResourceSlider == "DeltaV");
            
            if (!isDeltaVExpanded)
            {
                // Compact line: active (20) + mute (20) + label (55) + value (35) + Set (30)
                GUILayout.BeginHorizontal(GUILayout.Height(20));
                
                // Active checkbox first
                bool newDeltaVAlarm = GUILayout.Toggle(deltaVAlarm, "", GUILayout.Width(20));
                if (Event.current.type == EventType.Repaint)
                    TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "deltav_alarm");
                
                if (resourcesRunner != null)
                {
                    resourcesRunner.EnableDeltaVAlarm = newDeltaVAlarm;
                    if (newDeltaVAlarm != deltaVAlarm)
                    {
                        ResourcesAlarmConfig.SaveFrom(resourcesRunner);
                    }
                }
                
                // Mute checkbox second
                bool deltaVMuted = resourcesRunner != null && resourcesRunner.MuteDeltaVAlarm;
                bool newDeltaVMuted = GUILayout.Toggle(deltaVMuted, "", GUILayout.Width(20));
                if (Event.current.type == EventType.Repaint)
                    TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "resource_mute");
                
                if (resourcesRunner != null && deltaVMuted != newDeltaVMuted)
                {
                    resourcesRunner.MuteDeltaVAlarm = newDeltaVMuted;
                    ResourcesAlarmConfig.SaveFrom(resourcesRunner);
                }
                
                GUILayout.Label("Delta-V", resourceLabelStyle, GUILayout.Width(55));
                if (Event.current.type == EventType.Repaint)
                    TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "deltav_alarm");
                
                GUILayout.Label(((int)deltaVThreshold).ToString() + "m/s", resourceLabelStyle, GUILayout.Width(35));
                
                if (GUILayout.Button("Set", GUILayout.Width(30)))
                {
                    _expandedResourceSlider = "DeltaV";
                }
                
                GUILayout.EndHorizontal();
            }
            else
            {
                // Expanded text input view: TextField with OK button
                GUILayout.BeginVertical();
                
                // Initialize buffer on first expand
                if (_deltaVThresholdStr == null)
                {
                    _deltaVThresholdStr = ((int)deltaVThreshold).ToString();
                }
                
                GUILayout.Label("Delta-V threshold (m/s):", textStyle);
                GUILayout.BeginHorizontal();
                
                // Text field for manual input
                _deltaVThresholdStr = GUILayout.TextField(_deltaVThresholdStr, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                    TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "deltav_threshold");
                
                // Parse and apply value
                if (resourcesRunner != null && float.TryParse(_deltaVThresholdStr, out float parsed))
                {
                    // Clamp to reasonable range (50-10000 m/s)
                    float clamped = Mathf.Clamp(parsed, 50f, 10000f);
                    if (Mathf.Abs(clamped - deltaVThreshold) > 1f)
                    {
                        resourcesRunner.DeltaVThreshold = clamped;
                        ResourcesAlarmConfig.SaveFrom(resourcesRunner);
                    }
                }
                
                if (GUILayout.Button("OK", GUILayout.Width(40)))
                {
                    _expandedResourceSlider = null;
                    _deltaVThresholdStr = null; // Reset buffer
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(2f);
            }
            
            // Now regular resources
            string[] names = resourcesRunner != null ? resourcesRunner.GetCurrentVesselResourceNames() : new string[] { };
            
            // Custom sort order: priority resources first, then alphabetical
            var priorityOrder = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "ElectricCharge", 1 },
                { "LiquidFuel", 2 },
                { "Oxidizer", 3 },
                { "MonoPropellant", 4 },
                { "XenonGas", 5 },
                { "Ore", 6 },
                { "Ablator", 7 },
                { "IntakeAir", 8 }
            };
            
            var sortedNames = names.OrderBy(n => 
            {
                int priority;
                if (priorityOrder.TryGetValue(n, out priority)) return priority;
                return 100; // other resources after priority ones
            }).ThenBy(n => n, System.StringComparer.OrdinalIgnoreCase);
            
            // Calculate available width: panel width minus margins and scrollbar
            float availableWidth = sliderRect.width - 16f - 16f; // 8f margin on each side, plus scrollbar reserve
            
            foreach (var n in sortedNames)
            {
                bool isOn = resourcesRunner != null && resourcesRunner.EnabledResources.Contains(n);
                float cur = 0.15f;
                if (resourcesRunner != null)
                {
                    float v; 
                    if (!resourcesRunner.ThresholdByResource.TryGetValue(n, out v)) v = 0.15f; 
                    cur = v;
                }
                
                // Check if this resource has expanded slider
                bool isExpanded = (_expandedResourceSlider == n);
                
                if (!isExpanded)
                {
                    // Compact line: active (20) + mute (20) + label (55) + value (35) + Set (30)
                    GUILayout.BeginHorizontal(GUILayout.Height(20));
                    
                    // Active checkbox first
                    bool newOn = GUILayout.Toggle(isOn, "", GUILayout.Width(20));
                    if (Event.current.type == EventType.Repaint)
                        TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "resource_alarm");
                    
                    if (resourcesRunner != null)
                    {
                        if (newOn) resourcesRunner.EnabledResources.Add(n); 
                        else resourcesRunner.EnabledResources.Remove(n);
                    }
                    
                    // Mute checkbox second
                    bool isMuted = resourcesRunner != null && resourcesRunner.MutedResources.Contains(n);
                    bool newMuted = GUILayout.Toggle(isMuted, "", GUILayout.Width(20));
                    if (Event.current.type == EventType.Repaint)
                        TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "resource_mute");
                    
                    if (resourcesRunner != null)
                    {
                        if (newMuted && !isMuted)
                        {
                            resourcesRunner.MutedResources.Add(n);
                            ResourcesAlarmConfig.SaveFrom(resourcesRunner);
                        }
                        else if (!newMuted && isMuted)
                        {
                            resourcesRunner.MutedResources.Remove(n);
                            ResourcesAlarmConfig.SaveFrom(resourcesRunner);
                        }
                    }
                    
                    // Truncate long resource names to fit
                    string displayName = n.Length > 11 ? n.Substring(0, 9) + ".." : n;
                    GUILayout.Label(displayName, resourceLabelStyle, GUILayout.Width(55));
                    
                    GUILayout.Label(((int)(cur*100)).ToString() + "%", resourceLabelStyle, GUILayout.Width(35));
                    
                    if (GUILayout.Button("Set", GUILayout.Width(30)))
                    {
                        _expandedResourceSlider = n;
                    }
                    
                    GUILayout.EndHorizontal();
                }
                else
                {
                    // Expanded slider view: full-width slider with OK button
                    GUILayout.BeginVertical();
                    
                    // Resource name header
                    GUILayout.Label($"{n}: {(int)(cur*100)}%", textStyle);
                    
                    GUILayout.BeginHorizontal();
                    
                    // Full-width slider
                    float newThr = GUILayout.HorizontalSlider(cur, 0.01f, 0.8f, GUILayout.ExpandWidth(true));
                    if (Event.current.type == EventType.Repaint)
                        TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "resource_threshold");
                    
                    if (resourcesRunner != null) resourcesRunner.ThresholdByResource[n] = newThr;
                    
                    // OK button to close
                    if (GUILayout.Button("OK", GUILayout.Width(40)))
                    {
                        _expandedResourceSlider = null;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.Space(2f);
                }
                
                // Show EVA Jetpack alarm right after MonoPropellant
                if (string.Equals(n, "MonoPropellant", System.StringComparison.OrdinalIgnoreCase))
                {
                    bool evaMonoPropAlarm = resourcesRunner != null && resourcesRunner.EnableEvaMonoPropAlarm;
                    float evaMonoPropThreshold = resourcesRunner != null ? resourcesRunner.EvaMonoPropThreshold : 0.15f;
                    bool isEvaMonoPropExpanded = (_expandedResourceSlider == "EVA_MonoPropellant");
                    
                    if (!isEvaMonoPropExpanded)
                    {
                        // Compact line for EVA Jetpack: active (20) + mute (20) + label (55) + value (35) + Set (30)
                        GUILayout.BeginHorizontal(GUILayout.Height(20));
                        
                        // Active checkbox first
                        bool newEvaMonoPropAlarm = GUILayout.Toggle(evaMonoPropAlarm, "", GUILayout.Width(20));
                        if (Event.current.type == EventType.Repaint)
                            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "eva_monoprop_alarm");
                        
                        if (resourcesRunner != null)
                        {
                            resourcesRunner.EnableEvaMonoPropAlarm = newEvaMonoPropAlarm;
                            if (newEvaMonoPropAlarm != evaMonoPropAlarm)
                            {
                                ResourcesAlarmConfig.SaveFrom(resourcesRunner);
                            }
                        }
                        
                        // Mute checkbox second
                        bool evaMuted = resourcesRunner != null && resourcesRunner.MuteEvaMonoPropAlarm;
                        bool newEvaMuted = GUILayout.Toggle(evaMuted, "", GUILayout.Width(20));
                        if (Event.current.type == EventType.Repaint)
                            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "resource_mute");
                        
                        if (resourcesRunner != null && evaMuted != newEvaMuted)
                        {
                            resourcesRunner.MuteEvaMonoPropAlarm = newEvaMuted;
                            ResourcesAlarmConfig.SaveFrom(resourcesRunner);
                        }
                        
                        GUILayout.Label("EVA Jetpack", resourceLabelStyle, GUILayout.Width(55));
                        GUILayout.Label(((int)(evaMonoPropThreshold * 100)).ToString() + "%", resourceLabelStyle, GUILayout.Width(35));
                        
                        if (GUILayout.Button("Set", GUILayout.Width(30)))
                        {
                            _expandedResourceSlider = "EVA_MonoPropellant";
                        }
                        
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        // Expanded slider view for EVA Jetpack
                        GUILayout.BeginVertical();
                        GUILayout.Label($"EVA Jetpack Fuel: {(int)(evaMonoPropThreshold * 100)}%", textStyle);
                        GUILayout.BeginHorizontal();
                        float newThreshold = GUILayout.HorizontalSlider(evaMonoPropThreshold, 0.05f, 0.8f, GUILayout.ExpandWidth(true));
                        if (Event.current.type == EventType.Repaint)
                            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "eva_monoprop_threshold");
                        
                        if (resourcesRunner != null && Mathf.Abs(newThreshold - evaMonoPropThreshold) > 0.01f)
                        {
                            resourcesRunner.EvaMonoPropThreshold = newThreshold;
                            ResourcesAlarmConfig.SaveFrom(resourcesRunner);
                        }
                        if (GUILayout.Button("OK", GUILayout.Width(40)))
                        {
                            _expandedResourceSlider = null;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        GUILayout.Space(2f);
                    }
                }
            }
            
            // Silence all alarms checkbox at the bottom
            GUILayout.Space(8f);
            bool silence = resourcesRunner != null && resourcesRunner.SilenceAlarms;
            bool newSilence = GUILayout.Toggle(silence, " Silence all resource alarms");
            if (Event.current.type == EventType.Repaint)
                TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "silence_all");
            
            if (resourcesRunner != null)
            {
                resourcesRunner.SilenceAlarms = newSilence;
            }
        }
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
    
    // Stall Warning UI input buffers
    private string _stallMinSpeedStr;
    private string _stallDescentRatioStr;
    private string _stallMinAglStr;
    
    // Resources page: expanded slider state
    private string _expandedResourceSlider; // null = none expanded, or resource name
    
    // Delta-V input buffer
    private string _deltaVThresholdStr;

    private void DrawTerrainPage()
    {
        // Back button at the top
        if (GUILayout.Button("Back", tabBtnStyle)) 
        {
            currentPage = Page.Menu;
            TooltipManager.ResetTooltip();
            return;
        }
        
        GUILayout.Space(4f);
        bool enabled = (terrainRunner != null && terrainRunner.enabled);
        string label = enabled ? "Disable" : "Enable";
        if (GUILayout.Button(label, tabBtnStyle))
        {
            if (!enabled)
            {
                if (terrainRunner == null)
                {
                    // First try to find existing runner (created by AlarmSystemBootstrap)
                    terrainRunner = FindObjectOfType<TerrainAlarmRunner>();
                    
                    // If not found, create new one
                    if (terrainRunner == null)
                    {
                        var go = new GameObject("TerrainAlarmRunner");
                        terrainRunner = go.AddComponent<TerrainAlarmRunner>();
                        DontDestroyOnLoad(go);

                        TerrainAlarmConfig.LoadInto(terrainRunner);
                    }
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
                // _aheadStepStr remains hidden - no UI control
                _aheadMinSpeedStr = null;
                _stallMinSpeedStr = null;
                _stallDescentRatioStr = null;
                _stallMinAglStr = null;
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
            return;
        }

        // Vessel type filter: All vessels vs Plane only (radio buttons)
        GUILayout.BeginHorizontal();
        bool allVessels = !terrainRunner.AircraftOnly;
        bool aircraftOnly = terrainRunner.AircraftOnly;
        
        bool newAllVessels = GUILayout.Toggle(allVessels, " All vessels", GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "vessel_filter_all");
        
        GUILayout.Space(4f);
        bool newAircraftOnly = GUILayout.Toggle(aircraftOnly, " Plane only", GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "vessel_filter_plane");
        
        // Apply changes immediately with proper radio button behavior
        if (newAllVessels && !allVessels)
        {
            terrainRunner.AircraftOnly = false;
            TerrainAlarmConfig.SaveFrom(terrainRunner);
        }
        else if (newAircraftOnly && !aircraftOnly)
        {
            terrainRunner.AircraftOnly = true;
            TerrainAlarmConfig.SaveFrom(terrainRunner);
        }
        
        GUILayout.EndHorizontal();
        GUILayout.Space(6f);

        if (_terrainAglStr == null) _terrainAglStr = terrainRunner.AltitudeAGL.ToString("F0");
        if (_terrainVsStr == null) _terrainVsStr = terrainRunner.DescentSpeed.ToString("F0");

        if (_gearAglStr == null) _gearAglStr = terrainRunner.GearAlarmAGL.ToString("F0");
        if (_gearMaxSpdStr == null) _gearMaxSpdStr = terrainRunner.GearAlarmMaxSpeed.ToString("F0");

        if (_aheadMarginStr == null) _aheadMarginStr = terrainRunner.TerrainAheadMarginMeters.ToString("F0");
        if (_aheadMaxTimeStr == null) _aheadMaxTimeStr = terrainRunner.TerrainAheadMaxTime.ToString("F1");
        // Step field remains in config but hidden from UI
        if (_aheadStepStr == null) _aheadStepStr = terrainRunner.TerrainAheadStep.ToString("F2");
        if (_aheadMinSpeedStr == null) _aheadMinSpeedStr = terrainRunner.TerrainAheadMinSpeed.ToString("F0");

        // Create text style for labels
        GUIStyle labelStyle = new GUIStyle(HighLogic.Skin.label);
        labelStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        
        // Create style for units (slightly dimmer)
        GUIStyle unitStyle = new GUIStyle(HighLogic.Skin.label);
        unitStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        unitStyle.fontSize = 11;

        // Base terrain threshold (always relevant)
        terrainRunner.EnableTerrainBase = GUILayout.Toggle(terrainRunner.EnableTerrainBase, " Terrain (Pull Up)");
        if (Event.current.type == EventType.Repaint)
            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "terrain_base");

        if (terrainRunner.EnableTerrainBase)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Altitude AGL:", labelStyle, GUILayout.Width(90));
            _terrainAglStr = GUILayout.TextField(_terrainAglStr, GUILayout.Width(50));
            if (Event.current.type == EventType.Repaint)
                TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "terrain_agl");
            GUILayout.Label("m", unitStyle, GUILayout.Width(20));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vertical Speed:", labelStyle, GUILayout.Width(90));
            _terrainVsStr = GUILayout.TextField(_terrainVsStr, GUILayout.Width(50));
            if (Event.current.type == EventType.Repaint)
                TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "terrain_vs");
            GUILayout.Label("m/s", unitStyle, GUILayout.Width(30));
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(6f);

        // Counting out
        terrainRunner.EnableAltitudeCallouts = GUILayout.Toggle(terrainRunner.EnableAltitudeCallouts, " Counting out");
        if (Event.current.type == EventType.Repaint)
            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "counting_out");

        GUILayout.Space(6f);
        
        // Landed callout (separate toggle under Counting out)
        terrainRunner.EnableLandedCallout = GUILayout.Toggle(terrainRunner.EnableLandedCallout, " Landed callout");
        if (Event.current.type == EventType.Repaint)
            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "landed_callout");

        GUILayout.Space(6f);

        // Sink rate
        terrainRunner.EnableSinkRate = GUILayout.Toggle(terrainRunner.EnableSinkRate, " Sink rate");
        if (Event.current.type == EventType.Repaint)
            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "sink_rate");

        GUILayout.Space(6f);

        // Gear
        bool newGearOn = GUILayout.Toggle(terrainRunner.EnableGearAlarm, " Gear alarm");
        if (Event.current.type == EventType.Repaint)
            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "gear_alarm");
        
        if (newGearOn)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Altitude AGL:", labelStyle, GUILayout.Width(90));
            _gearAglStr = GUILayout.TextField(_gearAglStr, GUILayout.Width(50));
            if (Event.current.type == EventType.Repaint)
                TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "gear_agl");
            GUILayout.Label("m", unitStyle, GUILayout.Width(20));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Max speed:", labelStyle, GUILayout.Width(90));
            _gearMaxSpdStr = GUILayout.TextField(_gearMaxSpdStr, GUILayout.Width(50));
            if (Event.current.type == EventType.Repaint)
                TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "gear_maxspeed");
            GUILayout.Label("m/s", unitStyle, GUILayout.Width(30));
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(6f);

        // Terrain ahead
        bool newAhead = GUILayout.Toggle(terrainRunner.EnableTerrainAhead, " Terrain ahead");
        if (Event.current.type == EventType.Repaint)
            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "terrain_ahead");
        
        if (newAhead)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Time to crash:", labelStyle, GUILayout.Width(90));
            _aheadMaxTimeStr = GUILayout.TextField(_aheadMaxTimeStr, GUILayout.Width(50));
            if (Event.current.type == EventType.Repaint)
                TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "ahead_time");
            GUILayout.Label("sec", unitStyle, GUILayout.Width(30));
            GUILayout.EndHorizontal();

            // Step field HIDDEN from UI - value preserved in config
            // GUILayout.BeginHorizontal();
            // GUILayout.Label("Scan step:", labelStyle, GUILayout.Width(90));
            // _aheadStepStr = GUILayout.TextField(_aheadStepStr, GUILayout.Width(50));
            // GUILayout.Label("sec", unitStyle, GUILayout.Width(30));
            // GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Safety margin:", labelStyle, GUILayout.Width(90));
            _aheadMarginStr = GUILayout.TextField(_aheadMarginStr, GUILayout.Width(50));
            if (Event.current.type == EventType.Repaint)
                TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "ahead_margin");
            GUILayout.Label("m", unitStyle, GUILayout.Width(20));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Min speed:", labelStyle, GUILayout.Width(90));
            _aheadMinSpeedStr = GUILayout.TextField(_aheadMinSpeedStr, GUILayout.Width(50));
            if (Event.current.type == EventType.Repaint)
                TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "ahead_minspeed");
            GUILayout.Label("m/s", unitStyle, GUILayout.Width(30));
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
        // Step parsing still present but hidden from UI
        if (float.TryParse(_aheadStepStr, out var step)) terrainRunner.TerrainAheadStep = Mathf.Clamp(step, 0.05f, 2f);
        if (float.TryParse(_aheadMinSpeedStr, out var minSpd)) terrainRunner.TerrainAheadMinSpeed = Mathf.Clamp(minSpd, 0f, 2000f);

        GUILayout.Space(6f);

        // Stall Warning
        bool newStallOn = GUILayout.Toggle(terrainRunner.EnableStallWarning, " Stall warning");
        if (Event.current.type == EventType.Repaint)
            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "stall_warning");
        
        if (newStallOn)
        {
            // Initialize text fields on first display
            if (_stallMinSpeedStr == null) _stallMinSpeedStr = terrainRunner.StallMinHorizontalSpeed.ToString("F0");
            if (_stallDescentRatioStr == null) _stallDescentRatioStr = (terrainRunner.StallDescentRatio * 100f).ToString("F0");
            if (_stallMinAglStr == null) _stallMinAglStr = terrainRunner.StallMinAGL.ToString("F0");

            // Mode selection: Auto vs Manual (radio buttons)
            GUILayout.BeginHorizontal();
            bool isAuto = (terrainRunner.StallWarningMode == TerrainAlarmRunner.StallMode.Auto);
            bool isManual = (terrainRunner.StallWarningMode == TerrainAlarmRunner.StallMode.Manual);
            
            bool newAuto = GUILayout.Toggle(isAuto, " Auto", GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "stall_mode_auto");
            
            GUILayout.Space(4f);
            bool newManual = GUILayout.Toggle(isManual, " Manual", GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "stall_mode_manual");
            
            // Apply radio button behavior
            if (newAuto && !isAuto)
            {
                terrainRunner.StallWarningMode = TerrainAlarmRunner.StallMode.Auto;
            }
            else if (newManual && !isManual)
            {
                terrainRunner.StallWarningMode = TerrainAlarmRunner.StallMode.Manual;
            }
            GUILayout.EndHorizontal();

            // Mode-specific settings
            if (terrainRunner.StallWarningMode == TerrainAlarmRunner.StallMode.Manual)
            {
                // Manual mode: minimum horizontal speed threshold
                GUILayout.BeginHorizontal();
                GUILayout.Label("Min speed:", labelStyle, GUILayout.Width(90));
                _stallMinSpeedStr = GUILayout.TextField(_stallMinSpeedStr, GUILayout.Width(50));
                if (Event.current.type == EventType.Repaint)
                    TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "stall_min_speed");
                GUILayout.Label("m/s", unitStyle, GUILayout.Width(30));
                GUILayout.EndHorizontal();
            }
            else
            {
                // Auto mode: descent ratio threshold (as percentage)
                GUILayout.BeginHorizontal();
                GUILayout.Label("Descent ratio:", labelStyle, GUILayout.Width(90));
                _stallDescentRatioStr = GUILayout.TextField(_stallDescentRatioStr, GUILayout.Width(50));
                if (Event.current.type == EventType.Repaint)
                    TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "stall_descent_ratio");
                GUILayout.Label("%", unitStyle, GUILayout.Width(30));
                GUILayout.EndHorizontal();
            }

            // Common setting: minimum AGL
            GUILayout.BeginHorizontal();
            GUILayout.Label("Min altitude:", labelStyle, GUILayout.Width(90));
            _stallMinAglStr = GUILayout.TextField(_stallMinAglStr, GUILayout.Width(50));
            if (Event.current.type == EventType.Repaint)
                TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "stall_min_agl");
            GUILayout.Label("m", unitStyle, GUILayout.Width(20));
            GUILayout.EndHorizontal();
        }

        // Apply parsed Stall Warning settings
        terrainRunner.EnableStallWarning = newStallOn;
        if (float.TryParse(_stallMinSpeedStr, out var stallMinSpeed)) 
            terrainRunner.StallMinHorizontalSpeed = Mathf.Clamp(stallMinSpeed, 0f, 500f);
        if (float.TryParse(_stallDescentRatioStr, out var stallRatioPercent)) 
            terrainRunner.StallDescentRatio = Mathf.Clamp(stallRatioPercent / 100f, 0.1f, 1.0f);
        if (float.TryParse(_stallMinAglStr, out var stallMinAgl)) 
            terrainRunner.StallMinAGL = Mathf.Clamp(stallMinAgl, 0f, 1000f);

        // Persist settings (debounced)
        if (Time.realtimeSinceStartup - _lastTerrainConfigSaveTs > 0.5f)
        {
            _lastTerrainConfigSaveTs = Time.realtimeSinceStartup;
            TerrainAlarmConfig.SaveFrom(terrainRunner);
        }

        GUILayout.Space(10f);
        
        // Volume slider at bottom
        GUIStyle volumeStyle = new GUIStyle(HighLogic.Skin.label);
        volumeStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        
        GUILayout.Label($"Volume: {(int)(terrainRunner.Volume * 100)}%", volumeStyle);
        GUILayout.BeginHorizontal();
        float newVolume = GUILayout.HorizontalSlider(terrainRunner.Volume, 0f, 1f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            TooltipManager.CheckHover(GUILayoutUtility.GetLastRect(), "volume");
        
        if (Mathf.Abs(newVolume - terrainRunner.Volume) > 0.01f)
        {
            terrainRunner.Volume = newVolume;
            if (Time.realtimeSinceStartup - _lastTerrainConfigSaveTs > 0.5f)
            {
                _lastTerrainConfigSaveTs = Time.realtimeSinceStartup;
                TerrainAlarmConfig.SaveFrom(terrainRunner);
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DrawReminderPage()
    {
        // Back button at the top
        if (GUILayout.Button("Back", tabBtnStyle)) 
        {
            currentPage = Page.Menu;
            TooltipManager.ResetTooltip();
            return;
        }
        
        GUILayout.Space(4f);
        
        // White/cream text style for all labels
        GUIStyle textStyle = new GUIStyle(HighLogic.Skin.label);
        textStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        
        // Use compact layout to avoid overflow; put inputs on separate lines
        GUILayout.BeginHorizontal();
        GUILayout.Label("Year:", textStyle, GUILayout.Width(40));
        remYear = ClampParseInt(GUILayout.TextField(remYear.ToString(), GUILayout.Width(50)), 1, 9999, remYear);
        GUILayout.Label("Day:", textStyle, GUILayout.Width(40));
        remDay = ClampParseInt(GUILayout.TextField(remDay.ToString(), GUILayout.Width(50)), 1, 426, remDay);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Hour:", textStyle, GUILayout.Width(45));
        remHour = ClampParseInt(GUILayout.TextField(remHour.ToString(), GUILayout.Width(50)), 0, 5, remHour);
        GUILayout.Label("Min:", textStyle, GUILayout.Width(35));
        remMinute = ClampParseInt(GUILayout.TextField(remMinute.ToString(), GUILayout.Width(50)), 0, 59, remMinute);
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
        GUILayout.Label("Note:", textStyle, GUILayout.Width(45));
        remNote = GUILayout.TextArea(remNote ?? string.Empty, GUILayout.MinHeight(48));
        GUILayout.Space(6f);
        if (GUILayout.Button("Set reminder", tabBtnStyle))
        {
            var go = new GameObject("TimeReminderAlarm");
            var r = go.AddComponent<TimeReminderAlarm>();
            DontDestroyOnLoad(go);
            r.ScheduleKerbin(remYear, remDay, remHour, remMinute, remNote);
        }
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
