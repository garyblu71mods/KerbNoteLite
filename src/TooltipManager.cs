using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Terraria-style tooltip system: shows small info window after 2s hover.
/// </summary>
public static class TooltipManager
{
    private static string _currentTooltipKey;
    private static float _hoverStartTime;
    private static Rect _lastHoverRect;
    private static bool _isShowing;
    private static readonly Dictionary<string, string> _tooltips = new Dictionary<string, string>();
    
    private const float HOVER_DELAY = 2.0f;
    private const float TOOLTIP_WIDTH = 200f;
    private const float TOOLTIP_PADDING = 8f;
    private static readonly Color TOOLTIP_BG_COLOR = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    private static readonly Color TOOLTIP_BORDER_COLOR = new Color(0.6f, 0.5f, 0.3f, 1f);
    private static readonly Color TOOLTIP_TEXT_COLOR = new Color(0.95f, 0.9f, 0.75f, 1f);
    
    private static Texture2D _bgTexture;
    private static Texture2D _borderTexture;
    private static GUIStyle _tooltipStyle;
    
    // Window ID for tooltip (very high negative number to ensure it's on top)
    private const int TOOLTIP_WINDOW_ID = -999999;
    private static Rect _tooltipRect;
    private static string _tooltipText;
    
    static TooltipManager()
    {
        RegisterDefaultTooltips();
    }
    
    /// <summary>
    /// Register all default tooltips for Terrain Alarm controls
    /// </summary>
    private static void RegisterDefaultTooltips()
    {
        // Terrain base
        _tooltips["terrain_base"] = "Sound alarm when descending too fast at low altitude. Pull up! (Suppressed when gear is deployed)";
        _tooltips["terrain_agl"] = "Alert altitude above ground. Set higher values for more warning time.";
        _tooltips["terrain_vs"] = "Vertical speed threshold (negative = descending). More negative = faster descent required to trigger.";
        
        // Counting out
        _tooltips["counting_out"] = "Announce altitude callouts during descent (200m, 100m, 50m, etc).";
        
        // Landed callout
        _tooltips["landed_callout"] = "Announce 'Landed' when touchdown is detected. Disable if repetitive landings are annoying.";
        
        // Sink rate
        _tooltips["sink_rate"] = "Warns 'Sink Rate!' when descending too fast below 70m AGL with gear deployed.";
        
        // Gear alarm
        _tooltips["gear_alarm"] = "Reminds you to deploy landing gear when approaching ground at moderate speed.";
        _tooltips["gear_agl"] = "Altitude to trigger gear reminder. Set higher for faster approaches.";
        _tooltips["gear_maxspeed"] = "Maximum speed to trigger gear alarm. Won't nag during high-speed flybys.";
        
        // Terrain ahead
        _tooltips["terrain_ahead"] = "Predicts terrain collision ahead based on current trajectory. Very useful for low-altitude flight!";
        _tooltips["ahead_time"] = "How many seconds ahead to scan for terrain. Higher = earlier warning but more false positives.";
        _tooltips["ahead_margin"] = "Safety margin above terrain. Set higher for mountainous terrain or if warnings come too late.";
        _tooltips["ahead_minspeed"] = "Minimum speed to activate terrain ahead scanning. Prevents false alarms during slow flight/taxi.";
        
        // Stall warning
        _tooltips["stall_warning"] = "Warns when losing lift (angle between nose and flight direction too large). Helps prevent aerodynamic stalls.";
        _tooltips["stall_mode_auto"] = "Automatic: triggers when angle between nose direction and actual flight path exceeds threshold (loss of lift detection).";
        _tooltips["stall_mode_manual"] = "Manual: triggers when horizontal speed drops below threshold (simple speed check).";
        _tooltips["stall_angle_threshold"] = "Auto mode: Maximum angle between nose and flight direction before alarm (0°=perfect, 45°=moderate deviation, 90°=sideways/falling). Lower = more sensitive.";
        _tooltips["stall_min_speed"] = "Manual mode: Minimum safe horizontal speed. Alarm triggers when below this speed (in m/s).";
        _tooltips["stall_min_agl"] = "Don't trigger stall warning below this altitude (prevents false alarms during landing approach).";
        
        // Vessel filter
        _tooltips["vessel_filter_all"] = "Enable terrain alarms for all vessel types (rockets, planes, rovers).";
        _tooltips["vessel_filter_plane"] = "Enable terrain alarms only for aircraft (vessels with wings, control surfaces, jet engines).";
        
        // Volume
        _tooltips["volume"] = "Master volume for all terrain alarm sounds. Adjust to your preference.";
        
        // Resources alarms
        _tooltips["resource_alarm"] = "Enable alarm when this resource drops below the set threshold. Click 'Set' to change value.";
        _tooltips["resource_mute"] = "Mute sound for this resource alarm (still shows screen message). Useful for non-critical resources.";
        _tooltips["resource_threshold"] = "Percentage threshold for alarm. Alarm triggers when resource falls below this value.";
        _tooltips["eva_monoprop_alarm"] = "Alarm when EVA jetpack fuel (MonoPropellant) drops below threshold. Helps prevent stranding Kerbals in space!";
        _tooltips["eva_monoprop_threshold"] = "EVA jetpack fuel threshold. Alarm triggers when MonoPropellant falls below this during EVA.";
        _tooltips["comm_alarm"] = "Alarm when communication signal strength drops below threshold. Prevents loss of contact with KSC!";
        _tooltips["comm_threshold"] = "Communication signal strength threshold (0-75%). Alarm warns before losing connection.";
        _tooltips["kerbal_voice_alarm"] = "Use Kerbal voice sounds for resource alarms (default). Adds character to warnings with various Kerbal exclamations!";
        _tooltips["beep_alarm"] = "Use simple beep sound for resource alarms (fallback). Clean electronic tone without voice samples.";
        
        // Delta-V alarm
        _tooltips["deltav_alarm"] = "Alarm when vessel's remaining Delta-V drops below threshold. Helps prevent getting stranded without fuel for maneuvers!";
        _tooltips["deltav_threshold"] = "Enter Delta-V threshold in meters per second (50-10000 m/s). Alarm triggers when remaining ?V falls below this value (accounts for all stages).";
    }
    
    /// <summary>
    /// Register a custom tooltip for a specific key
    /// </summary>
    public static void RegisterTooltip(string key, string text)
    {
        if (string.IsNullOrEmpty(key)) return;
        _tooltips[key] = text ?? string.Empty;
    }
    
    /// <summary>
    /// Check if a tooltip exists for the given key
    /// </summary>
    public static bool HasTooltip(string key)
    {
        return !string.IsNullOrEmpty(key) && _tooltips.ContainsKey(key);
    }
    
    /// <summary>
    /// Call this for each interactive UI element. Pass a unique key and the element's Rect.
    /// </summary>
    public static void CheckHover(Rect rect, string tooltipKey)
    {
        if (string.IsNullOrEmpty(tooltipKey) || !_tooltips.ContainsKey(tooltipKey))
            return;
            
        Event e = Event.current;
        if (e == null) return;
        
        bool isHovering = rect.Contains(e.mousePosition);
        
        if (isHovering)
        {
            // Same rect hovering
            if (_currentTooltipKey == tooltipKey)
            {
                float elapsed = Time.realtimeSinceStartup - _hoverStartTime;
                if (elapsed >= HOVER_DELAY)
                {
                    _isShowing = true;
                }
                _lastHoverRect = rect;
            }
            else
            {
                // New element hovered - reset timer
                _currentTooltipKey = tooltipKey;
                _hoverStartTime = Time.realtimeSinceStartup;
                _lastHoverRect = rect;
                _isShowing = false;
            }
        }
        else if (_currentTooltipKey == tooltipKey)
        {
            // Mouse left this element - clear if it was ours
            ResetTooltip();
        }
    }
    
    /// <summary>
    /// Draw the tooltip if it should be visible. Call once per OnGUI.
    /// </summary>
    public static void DrawTooltip()
    {
        if (!_isShowing || string.IsNullOrEmpty(_currentTooltipKey))
            return;
            
        if (!_tooltips.TryGetValue(_currentTooltipKey, out string text) || string.IsNullOrEmpty(text))
            return;
        
        EnsureTextures();
        EnsureStyle();
        
        // Calculate tooltip size based on text
        GUIContent content = new GUIContent(text);
        float textHeight = _tooltipStyle.CalcHeight(content, TOOLTIP_WIDTH - TOOLTIP_PADDING * 2);
        float tooltipHeight = textHeight + TOOLTIP_PADDING * 2;
        
        // Position: near mouse cursor with offset
        Event e = Event.current;
        Vector2 mousePos = e != null ? e.mousePosition : _lastHoverRect.center;
        
        // Position tooltip slightly below and to the right of cursor
        float tooltipX = mousePos.x + 15f;
        float tooltipY = mousePos.y + 20f;
        
        // Clamp to screen bounds
        tooltipX = Mathf.Clamp(tooltipX, 10f, Screen.width - TOOLTIP_WIDTH - 10f);
        tooltipY = Mathf.Clamp(tooltipY, 10f, Screen.height - tooltipHeight - 10f);
        
        _tooltipRect = new Rect(tooltipX, tooltipY, TOOLTIP_WIDTH, tooltipHeight);
        _tooltipText = text;
        
        // Draw as a window with very high priority (negative ID = always on top)
        GUI.Window(TOOLTIP_WINDOW_ID, _tooltipRect, DrawTooltipWindow, string.Empty, GUIStyle.none);
        GUI.BringWindowToFront(TOOLTIP_WINDOW_ID);
    }
    
    /// <summary>
    /// Internal window function for tooltip rendering
    /// </summary>
    private static void DrawTooltipWindow(int windowID)
    {
        // Draw border
        GUI.color = TOOLTIP_BORDER_COLOR;
        GUI.DrawTexture(new Rect(0, 0, _tooltipRect.width, _tooltipRect.height), _borderTexture);
        
        // Draw background (inset by 2px for border effect)
        Rect bgRect = new Rect(2, 2, _tooltipRect.width - 4, _tooltipRect.height - 4);
        GUI.color = TOOLTIP_BG_COLOR;
        GUI.DrawTexture(bgRect, _bgTexture);
        
        // Draw text
        GUI.color = Color.white;
        Rect textRect = new Rect(TOOLTIP_PADDING, TOOLTIP_PADDING, 
                                 _tooltipRect.width - TOOLTIP_PADDING * 2, 
                                 _tooltipRect.height - TOOLTIP_PADDING * 2);
        GUI.Label(textRect, _tooltipText, _tooltipStyle);
        
        GUI.color = Color.white; // Reset
    }
    
    /// <summary>
    /// Reset tooltip state (call when mouse leaves UI area or panel closes)
    /// </summary>
    public static void ResetTooltip()
    {
        _currentTooltipKey = null;
        _isShowing = false;
    }
    
    private static void EnsureTextures()
    {
        if (_bgTexture == null)
        {
            _bgTexture = new Texture2D(1, 1);
            _bgTexture.SetPixel(0, 0, Color.white);
            _bgTexture.Apply();
        }
        if (_borderTexture == null)
        {
            _borderTexture = new Texture2D(1, 1);
            _borderTexture.SetPixel(0, 0, Color.white);
            _borderTexture.Apply();
        }
    }
    
    private static void EnsureStyle()
    {
        if (_tooltipStyle == null)
        {
            _tooltipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(0, 0, 0, 0)
            };
            _tooltipStyle.normal.textColor = TOOLTIP_TEXT_COLOR;
        }
    }
}
