using KSP.UI.Screens;
using System;
using System.Linq;
using UnityEngine;

public partial class KerbNote
{
    private GlobalAlarmPanel globalAlarmPanel;

    private void EnsureCoreTexturesBound()
    {
        // Only check and bind textures if they're actually null - avoid redundant lookups
        if (ButtonTexture == null) ButtonTexture = SkinAssets.Get("Button") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON, false);
        if (ButtonHoverTexture == null) ButtonHoverTexture = SkinAssets.Get("ButtonHover") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_HOVER, false);
        if (ButtonClickTexture == null) ButtonClickTexture = SkinAssets.Get("ButtonClick") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_CLICK, false);
        if (TabTexture == null) TabTexture = SkinAssets.Get("Tab") ?? GameDatabase.Instance.GetTexture(TEXTURE_TAB, false);
        if (TabHoverTexture == null) TabHoverTexture = SkinAssets.Get("TabHover") ?? GameDatabase.Instance.GetTexture(TEXTURE_TAB_HOVER, false);
        if (TabClickTexture == null) TabClickTexture = SkinAssets.Get("TabClick") ?? GameDatabase.Instance.GetTexture(TEXTURE_TAB_CLICK, false);
        if (noteTex == null) noteTex = SkinAssets.Get("BackgroundWindow") ?? GameDatabase.Instance.GetTexture(TEXTURE_BACKGROUND_WINDOW, false);
    }

    // Stan podwójnego klikni?cia uchwytu zmiany rozmiaru
    private float lastResizeClickTime = 0f;
    private const float RESIZE_DOUBLE_CLICK_THRESHOLD = 0.35f;

    void OnGUI()
    {
        if (!showWindow) { if (editorLockActive) RemoveEditorLock(); return; }
        if (SettingWindow.IsAboutVisible)
        {
            if (editorLockActive) RemoveEditorLock();
            return;
        }
        GUI.skin = HighLogic.Skin;
        
        // Only check UI readiness if not already ready - avoid redundant calls
        if (!uiReady)
        {
            EnsureUiReady();
            if (!uiReady) return;
        }
        
        // Bind textures once at startup, not every frame
        EnsureCoreTexturesBound();
        
        // Only initialize styles once, not every frame
        if (!stylesInitialized)
        {
            try { InitStyles(); } catch { }
        }
        
        // Build button style once if missing
        if (buttonStyle == null)
        {
            buttonStyle = BuildButtonStyleFromTextures();
            if (buttonStyle == null) return;
        }

        var evt = Event.current;
        
        // OPTIMIZATION: Only process calculator keyboard events when calculator is visible
        if (showCalc && evt != null && evt.type == EventType.KeyDown)
        {
            bool handled = false;

            // 1) Przechwy? cyfry i operatory, aby zawsze stosowa? regu?y AppendCalcKey
            char ch = evt.character;
            string mapped = null;
            if (ch >= '0' && ch <= '9') mapped = ch.ToString();
            else if (ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '.') mapped = ch.ToString();
            else
            {
                switch (evt.keyCode)
                {
                    case KeyCode.Keypad0: mapped = "0"; break;
                    case KeyCode.Keypad1: mapped = "1"; break;
                    case KeyCode.Keypad2: mapped = "2"; break;
                    case KeyCode.Keypad3: mapped = "3"; break;
                    case KeyCode.Keypad4: mapped = "4"; break;
                    case KeyCode.Keypad5: mapped = "5"; break;
                    case KeyCode.Keypad6: mapped = "6"; break;
                    case KeyCode.Keypad7: mapped = "7"; break;
                    case KeyCode.Keypad8: mapped = "8"; break;
                    case KeyCode.Keypad9: mapped = "9"; break;
                    case KeyCode.KeypadPeriod: mapped = "."; break;
                    case KeyCode.KeypadPlus: mapped = "+"; break;
                    case KeyCode.KeypadMinus: mapped = "-"; break;
                    case KeyCode.KeypadMultiply: mapped = "*"; break;
                    case KeyCode.KeypadDivide: mapped = "/"; break;
                }
            }
            if (!string.IsNullOrEmpty(mapped)) { AppendCalcKey(mapped); handled = true; }

            // 2) Enter / Escape / Backspace
            bool isEnter = (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.character == '\n' || evt.character == '\r');
            if (isEnter) { EvaluateAndCommit(); handled = true; }
            else if (evt.keyCode == KeyCode.Escape || evt.character == (char)27) { calcInput = string.Empty; GUI.FocusControl("CalcDisplay"); handled = true; }
            else if (evt.keyCode == KeyCode.Backspace)
            {
                if (!string.IsNullOrEmpty(calcInput))
                {
                    calcInput = calcInput.Substring(0, calcInput.Length - 1);
                    GUI.FocusControl("CalcDisplay");
                }
                handled = true;
            }
            if (handled) evt.Use();
        }

        // Calculate resize handle position ONCE per frame
        Vector2 mouse = Event.current.mousePosition;
        Rect iconRect = new Rect(windowRect.x + windowRect.width +18f, windowRect.y + windowRect.height - RESIZE_GRIP_SIZE, RESIZE_GRIP_SIZE, RESIZE_GRIP_SIZE);
        Rect resizeHandle = iconRect;
        
        // Clamp window size
        float maxWidth = WINDOWS_MAX_WIDTH;
        float maxHeight = WINDOWS_MAX_HEIGHT;
        windowRect.width = Mathf.Min(windowRect.width, maxWidth);
        windowRect.height = Mathf.Min(windowRect.height, maxHeight);
        
        // Handle resize events
        if (Event.current.type == EventType.MouseDown && resizeHandle.Contains(mouse))
        {
            float now = Time.realtimeSinceStartup;
            if (now - lastResizeClickTime <= RESIZE_DOUBLE_CLICK_THRESHOLD)
            {
                HandleResizeDoubleClick();
                lastResizeClickTime = 0f;
                Event.current.Use();
            }
            else
            {
                lastResizeClickTime = now;
                isResizingWindow = true;
                resizeStartMouse = mouse;
                resizeStartRect = windowRect;
                Event.current.Use();
            }
        }
        if (isResizingWindow && Event.current.type == EventType.MouseDrag)
        {
            Vector2 delta = mouse - resizeStartMouse;
            windowRect.width = Mathf.Clamp(resizeStartRect.width + delta.x, WINDOW_MIN_WIDTH, WINDOWS_MAX_WIDTH);
            windowRect.height = Mathf.Clamp(resizeStartRect.height + delta.y, WINDOW_MIN_HEIGHT, WINDOWS_MAX_HEIGHT);
            Event.current.Use();
        }
        if (Event.current.type == EventType.MouseUp)
        {
            isResizingWindow = false;
            isClickOnAAA = false;
        }
        
        // ALWAYS draw resize icon when window is visible (not conditional on mouse events)
        if (this.resizeIcon != null && Event.current.type == EventType.Repaint)
        {
            GUI.DrawTexture(iconRect, this.resizeIcon);
        }

        // Draw main window WITHOUT depth manipulation - let it use default (0)
        windowRect = GUI.Window(windowID, windowRect, DrawWindowContents, "", GUIStyle.none);

        // Draw Mini button AFTER main window, so it appears on top
        if (!showCalc)
        {
            DrawMiniButtonOverlayInsideWindow();
        }
        else
        {
            lastMiniBtnRectScreen = new Rect();
        }

        ApplyGlobalClickLock(mouse);
    }

    // Prosta metoda DrawWindowContents
    void DrawWindowContents(int id)
    {
        try
        {
            // T?o na ca?e okno moda
            if (noteTex != null)
                GUI.DrawTexture(new Rect(0, 0, windowRect.width, windowRect.height), noteTex);
            
            DrawTopBar();
            if (!showCalc)
            {
                DrawTabBar();
                DrawNoteArea();
                DrawAAABtnBelowNote();
            }
            else
            {
                DrawDisplay();
                DrawCalcHistory();
                DrawKeypad();
            }
            
            if (!isClickOnAAA)
            {
                GUI.DragWindow();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote] DrawWindowContents exception: " + ex.Message);
        }
    }

    // Renders a Mini toggle button inside the main window area as its own GUI.Window so clicks are not intercepted
    private void DrawMiniButtonOverlayInsideWindow()
    {
        if (buttonStyle == null)
        {
            buttonStyle = BuildButtonStyleFromTextures();
            if (buttonStyle == null) return;
        }
        GUIStyle baseStyle = buttonStyle;

        // Size of the button
        float btnHeight = AAA_BTN_HEIGHT;
        float btnWidth = Mathf.Max(60f, baseStyle.CalcSize(new GUIContent("Mini")).x +18f);

        // ALWAYS calculate aaaBtnY based on current window layout, not relying on DrawNoteArea
        // This ensures the button stays visible even when switching tabs
        float topMargin = NOTE_TOP_MARGIN;
        float bottomMargin = NOTE_BOTTOM_MARGIN;
        float usableHeight = GetSafeNoteAreaHeight(windowRect, topMargin, bottomMargin);
        float calculatedAaaBtnY = topMargin + usableHeight + AAA_BTN_Y_OFFSET;
        
        // Use the calculated position (don't rely on aaaBtnY being set by DrawNoteArea)
        float miniBtnYLocal = calculatedAaaBtnY;

        // Position at the RIGHT side of the main window, aligned vertically with AAA button
        float rightMargin = NOTE_BG_MARGIN +12f; // small padding from right edge
        float miniBtnXLocal = windowRect.width - rightMargin - btnWidth;

        // Convert to screen space
        Rect miniBtnRectScreen = new Rect(windowRect.x + miniBtnXLocal, windowRect.y + miniBtnYLocal, btnWidth, btnHeight);
        lastMiniBtnRectScreen = miniBtnRectScreen;

        // Draw button WITHOUT depth manipulation - rely on draw order (drawn after main window = on top)
        GUI.Window(miniButtonWindowID, miniBtnRectScreen, id =>
        {
            GUIStyle miniStyle = buttonStyle;
            if (GUI.Button(new Rect(0f,0f, btnWidth, btnHeight), "Mini", miniStyle))
            {
                int tabIndex = activeTabIndex;
                string tabGuid = tabs[tabIndex].guid;
                MiniNote instance;
                if (!miniNotesByGuid.TryGetValue(tabGuid, out instance) || instance == null)
                {
                    var miniObj = new GameObject("MiniNote_Tab_" + tabGuid);
                    instance = miniObj.AddComponent<MiniNote>();
                    instance.InitWithGuid(this, tabGuid);
                    DontDestroyOnLoad(miniObj);
                    miniNotesByGuid[tabGuid] = instance;
                }
                else
                {
                    instance.SetHost(this);
                }
                instance.Toggle();
            }
        }, string.Empty, GUIStyle.none);

        // Force Mini button to stay on top by bringing it to front AFTER MiniNote's OnGUI has run
        // This ensures: Main Window (back) < Mini Button (middle) < MiniNote (front)
        GUI.BringWindowToFront(miniButtonWindowID);
    }

    private void ApplyGlobalClickLock(Vector2 mouse)
    {
        // Determine if mouse is over any KerbNote interactive surface
        bool mouseOver = showWindow && (windowRect.Contains(mouse) || (!showCalc && lastMiniBtnRectScreen.Contains(mouse)));

        if (!mouseOver)
        {
            if (editorLockActive) RemoveEditorLock();
            return;
        }

        // Pick a suitable ControlTypes mask for current scene
        ControlTypes mask;
        if (HighLogic.LoadedSceneIsEditor)
        {
            mask = ControlTypes.EDITOR_SOFT_LOCK;
        }
        else if (HighLogic.LoadedSceneIsFlight)
        {
            mask = ControlTypes.ALL_SHIP_CONTROLS; // blocks vessel/game clicks under our UI
        }
        else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
        {
            mask = ControlTypes.KSC_ALL; // blocks KSC facilities clicks (e.g., Mission Control/Contracts)
        }
        else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
        {
            mask = ControlTypes.KSC_ALL;
        }
        else
        {
            mask = ControlTypes.ALL_SHIP_CONTROLS;
        }

        try
        {
            InputLockManager.SetControlLock(mask, EditorLockID);
            editorLockActive = true;
        }
        catch { }
    }

    private void RemoveEditorLock()
    {
        try { InputLockManager.RemoveControlLock(EditorLockID); }
        catch { }
        editorLockActive = false;
    }

    private void HandleResizeDoubleClick()
    {
        // Map current size to 0..1 where 0 = min size, 1 = max size
        float minW = WINDOW_MIN_WIDTH;
        float minH = WINDOW_MIN_HEIGHT;
        float maxW = WINDOWS_MAX_WIDTH;
        float maxH = WINDOWS_MAX_HEIGHT;
        float normW = (maxW - minW) > 0 ? Mathf.Clamp01((windowRect.width - minW) / (maxW - minW)) : 0f;
        float normH = (maxH - minH) > 0 ? Mathf.Clamp01((windowRect.height - minH) / (maxH - minH)) : 0f;
        float sizeFactor = Mathf.Max(normW, normH);

        bool shrinkToMin = sizeFactor > 0.5f;
        if (shrinkToMin)
        {
            windowRect.width = minW;
            windowRect.height = minH;
        }
        else
        {
            windowRect.width = maxW;
            windowRect.height = maxH;
        }
    }
}
