using System;
using UnityEngine;

public partial class KerbNote
{
    // Cache for tab calculations to avoid redundant CalcSize calls
    private struct TabLayoutCache
    {
        public float width;
        public string displayName;
        public int lastUpdateFrame;
    }
    private TabLayoutCache[] tabLayoutCache = new TabLayoutCache[0];
    private GUIStyle cachedTabStyle;
    private int lastTabStyleUpdateFrame = -1;

    // Stan podwójnego klikni?cia na topbar
    private float lastTopBarClickTime = 0f;
    private const float TOPBAR_DOUBLE_CLICK_THRESHOLD = 0.35f;

    void DrawTabBar()
    {
        float tabBarHeight = TAB_BARHEIGHT;
        float tabBarY = TAB_BAR_Y;
        float tabBarMargin = TAB_BAR_MARGIN;
        float tabBarWidth = windowRect.width - 2 * tabBarMargin;
        Rect tabBarRect = new Rect(tabBarMargin, tabBarY, tabBarWidth, tabBarHeight);

        // Ensure cache array matches tab count
        if (tabLayoutCache.Length != tabs.Count)
        {
            tabLayoutCache = new TabLayoutCache[tabs.Count];
            for (int i = 0; i < tabLayoutCache.Length; i++)
            {
                tabLayoutCache[i].lastUpdateFrame = -1;
            }
        }

        // --- CACHED STYLE (reuse, don't create every frame) ---
        int currentFrame = Time.frameCount;
        if (cachedTabStyle == null || lastTabStyleUpdateFrame != currentFrame)
        {
            if (cachedTabStyle == null)
            {
                cachedTabStyle = new GUIStyle(GUI.skin.button);
                cachedTabStyle.fontStyle = FontStyle.Bold;
                cachedTabStyle.clipping = TextClipping.Clip;
                cachedTabStyle.padding = new RectOffset(13, 13, 2, 2);
            }
            lastTabStyleUpdateFrame = currentFrame;
        }

        // --- SCROLL WIDTH CALC WITH CACHE ---
        float totalTabsWidth = 0f;
        float tabMinWidth = 80f;
        float tabMaxWidth = 340f;
        float padding = 26f;
        
        for (int i = 0; i < tabs.Count; i++)
        {
            // Check if cache is valid for this frame and tab name hasn't changed
            bool cacheValid = tabLayoutCache[i].lastUpdateFrame == currentFrame - 1 && 
                             tabLayoutCache[i].displayName != null &&
                             (i != activeTabIndex || tabs[i].name == tabLayoutCache[i].displayName);
            
            if (!cacheValid)
            {
                string name = tabs[i].name;
                cachedTabStyle.alignment = (i == activeTabIndex) ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                float textWidth = cachedTabStyle.CalcSize(new GUIContent(name)).x;
                float tabWidth = i == activeTabIndex ? Mathf.Clamp(textWidth + padding, tabMinWidth, tabMaxWidth) : tabMinWidth;
                
                tabLayoutCache[i].width = tabWidth;
                tabLayoutCache[i].displayName = name;
                tabLayoutCache[i].lastUpdateFrame = currentFrame;
            }
            
            totalTabsWidth += tabLayoutCache[i].width + 3f;
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
                float tabWidth = tabLayoutCache[i].width;
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

        // --- T?o paska zak?adek ---
        GUIStyle tabBarStyle = new GUIStyle(GUI.skin.box);
        tabBarStyle.normal.background = MakeSolidTexture(1, 1, new Color(0f, 0f, 0f, 0.0f));
        tabBarStyle.border = new RectOffset(4, 4, 4, 4);
        tabBarStyle.padding = new RectOffset(6, 6, 6, 6);
        GUI.Box(tabBarRect, GUIContent.none, tabBarStyle);

        // --- RYSOWANIE ZAK?ADEK Z CLIPPINGIEM (using cached data) ---
        GUI.BeginGroup(tabBarRect);
        float tabXDraw = -tabBarScrollOffset;
        for (int i = 0; i < tabs.Count; i++)
        {
            string name = tabs[i].name;
            GUIStyle tabStyle = new GUIStyle(cachedTabStyle);
            tabStyle.alignment = (i == activeTabIndex) ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;

            bool isActiveTab = (i == activeTabIndex);
            Texture2D normalTex = isActiveTab && TabClickTexture != null ? TabClickTexture : TabTexture;
            Texture2D hoverTex = TabHoverTexture != null ? TabHoverTexture : normalTex;
            Texture2D clickTex = TabClickTexture != null ? TabClickTexture : hoverTex;
            tabStyle.normal.background = normalTex;
            tabStyle.hover.background = hoverTex;
            tabStyle.active.background = clickTex;

            tabStyle.normal.textColor = (i == activeTabIndex) ? new Color(0.8f, 0.6f, 0.2f) : new Color(0.6f, 0.4f, 0.2f);
            tabStyle.hover.textColor = (i == activeTabIndex) ? new Color(0.9f, 0.7f, 0.3f) : new Color(0.7f, 0.5f, 0.3f);
            tabStyle.active.textColor = (i == activeTabIndex) ? new Color(1f, 0.8f, 0.4f) : new Color(0.8f, 0.6f, 0.4f);

            float tabWidth = tabLayoutCache[i].width;
            string displayName = name;
            if (i != activeTabIndex && tabWidth == tabMinWidth)
            {
                int maxChars = Mathf.Max(1, Mathf.FloorToInt((tabMinWidth - 26f) / 8.5f));
                if (name.Length > maxChars)
                    displayName = name.Substring(0, maxChars) + "…";
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

        GUI.enabled = true;
    }

    void DrawTopBar()
    {
        try
        {
            if (!stylesInitialized)
            {
                try { InitStyles(); } catch { }
            }
            if (buttonStyle == null)
            {
                buttonStyle = BuildButtonStyleFromTextures();
            }
            if (buttonStyleRed == null)
            {
                buttonStyleRed = BuildRedButtonStyleFromTextures();
            }
            if (buttonStyle == null || buttonStyleRed == null)
            {
                Debug.LogWarning("[KerbNote][TopBar] Styles not ready -> skip frame");
                return;
            }

            float topBarY = 10f;
            float topBarHeight = 24f;
            float buttonPadding = 24f;
            Rect topBarRect = new Rect(10f, topBarY, windowRect.width - 20f, topBarHeight);

            // Handle double click on topbar for maximize/minimize
            Event evt = Event.current;
            if (evt != null && evt.type == EventType.MouseDown && evt.button == 0 && topBarRect.Contains(evt.mousePosition))
            {
                float now = Time.realtimeSinceStartup;
                if (now - lastTopBarClickTime <= TOPBAR_DOUBLE_CLICK_THRESHOLD)
                {
                    HandleTopBarDoubleClick();
                    lastTopBarClickTime = 0f;
                    evt.Use();
                }
                else
                {
                    lastTopBarClickTime = now;
                }
            }

            GUILayout.BeginArea(topBarRect);
            GUILayout.BeginHorizontal();

            GUIStyle topButtonStyle = buttonStyle;

            if (!showCalc)
            {
                // --- USUWANIE MINUS/PLUS W TRYBIE EDIT ---
                if (!showDeleteButton)
                {
                    if (!showRenamePopup)
                    {
                        // Rysuj - i + oraz edit tylko gdy popup nieaktywny
                        string minusLabel = "-";
                        float minusWidth = topButtonStyle.CalcSize(new GUIContent(minusLabel)).x + buttonPadding;
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

                        string editLabel = "edit";
                        float editWidth = topButtonStyle.CalcSize(new GUIContent(editLabel)).x + buttonPadding;
                        if (GUILayout.Button(editLabel, topButtonStyle, GUILayout.Width(editWidth), GUILayout.Height(topBarButtonHeight)))
                        {
                            showRenamePopup = true;
                            renameFocusRequested = true;
                            tabRenameBuffer = tabs[activeTabIndex].name;
                            isEditActive = true;
                        }
                    }
                }
                else
                {
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

                    string noLabel = "No";
                    float noWidth = topButtonStyle.CalcSize(new GUIContent(noLabel)).x + buttonPadding;
                    if (GUILayout.Button(noLabel, topButtonStyle, GUILayout.Width(noWidth), GUILayout.Height(topBarButtonHeight)))
                    {
                        showDeleteButton = false;
                    }

                    string yesLabel = "Yes";
                    float yesWidth = buttonStyleRed.CalcSize(new GUIContent(yesLabel)).x + buttonPadding;
                    if (GUILayout.Button(yesLabel, buttonStyleRed, GUILayout.Width(yesWidth), GUILayout.Height(topBarButtonHeight)))
                    {
                        recentlyDeletedTab = tabs[activeTabIndex];
                        recentlyDeletedTabIndex = activeTabIndex;
                        undoDeleteTime = Time.realtimeSinceStartup;
                        pendingPermanentDelete = true;
                        // Usu? alarmy dla tej zak?adki po GUID
                        try { AlarmManager.RemoveAllAlarmsForTab(tabs[activeTabIndex].guid); } catch (Exception ex) { Debug.LogError("[KerbNote] Failed to remove alarms: " + ex.Message); }
                        tabs.RemoveAt(activeTabIndex);
                        if (tabs.Count == 0) { tabs.Add(new NoteTab("Tab 1")); activeTabIndex = 0; } else { activeTabIndex = Mathf.Clamp(activeTabIndex, 0, tabs.Count - 1); }
                        tabRenameBuffer = tabs[activeTabIndex].name;
                        showDeleteButton = false;
                        isUndoHovered = false;
                        undoResumeTime = -1f;
                    }
                }

                if (recentlyDeletedTab != null && pendingPermanentDelete)
                {
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

                // --- Popup edycji (przesuwanie i rename) ---
                if (showRenamePopup)
                {
                    if (!editTabOriginalIndex.HasValue) editTabOriginalIndex = activeTabIndex;

                    float arrowBtnSize = topBarButtonHeight;
                    float textFieldWidth = 150f;
                    float okWidth = Mathf.Max(34f, topButtonStyle.CalcSize(new GUIContent("OK")).x + 10f);
                    float cancelWidth = Mathf.Max(34f, topButtonStyle.CalcSize(new GUIContent("X")).x + 10f);
                    float popupWidth = arrowBtnSize * 2 + textFieldWidth + okWidth + cancelWidth + 6f;
                    float popupHeight = topBarButtonHeight;
                    float popupX = 10f; // przesuni?cie w lewo o ~10px wzgl?dem lewej kraw?dzi topbara
                    float popupY = 0f;

                    GUI.BeginGroup(new Rect(popupX, popupY, popupWidth, popupHeight));

                    bool leftEdge = (activeTabIndex == 0);
                    GUIStyle arrowStyleL = new GUIStyle(topButtonStyle);
                    if (leftEdge && buttonStyleRed != null && buttonStyleRed.normal != null && buttonStyleRed.normal.background != null)
                    {
                        var red = buttonStyleRed.normal.background;
                        arrowStyleL.normal.background = red;
                        arrowStyleL.hover.background = red;
                        arrowStyleL.active.background = red;
                    }
                    if (GUI.Button(new Rect(0, 0, arrowBtnSize, arrowBtnSize), "<", arrowStyleL) && !leftEdge)
                    {
                        var tmp = tabs[activeTabIndex - 1];
                        tabs[activeTabIndex - 1] = tabs[activeTabIndex];
                        tabs[activeTabIndex] = tmp;
                        activeTabIndex--;
                    }

                    bool rightEdge = (activeTabIndex == tabs.Count - 1);
                    GUIStyle arrowStyleR = new GUIStyle(topButtonStyle);
                    if (rightEdge && buttonStyleRed != null && buttonStyleRed.normal != null && buttonStyleRed.normal.background != null)
                    {
                        var red = buttonStyleRed.normal.background;
                        arrowStyleR.normal.background = red;
                        arrowStyleR.hover.background = red;
                        arrowStyleR.active.background = red;
                    }
                    if (GUI.Button(new Rect(arrowBtnSize, 0, arrowBtnSize, arrowBtnSize), ">", arrowStyleR) && !rightEdge)
                    {
                        var tmp = tabs[activeTabIndex + 1];
                        tabs[activeTabIndex + 1] = tabs[activeTabIndex];
                        tabs[activeTabIndex] = tmp;
                        activeTabIndex++;
                    }

                    if (renameFieldStyle == null)
                    {
                        renameFieldStyle = new GUIStyle(GUI.skin.textField);
                        renameFieldStyle.fontStyle = FontStyle.Bold;
                        renameFieldStyle.alignment = TextAnchor.MiddleLeft;
                    }
                    GUI.SetNextControlName("RenameField");
                    tabRenameBuffer = GUI.TextField(new Rect(arrowBtnSize * 2, 0, textFieldWidth, arrowBtnSize), tabRenameBuffer, renameFieldStyle);
                    if (renameFocusRequested && Event.current.type == EventType.Repaint)
                    {
                        GUI.FocusControl("RenameField");
                        renameFocusRequested = false;
                    }
                    if (tabRenameBuffer.Length > 40) tabRenameBuffer = tabRenameBuffer.Substring(0, 40);

                    // ENTER i ESC
                    if (Event.current.type == EventType.KeyDown)
                    {
                        bool focusRename = GUI.GetNameOfFocusedControl() == "RenameField";
                        bool isEnter = (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter || Event.current.character == '\n' || Event.current.character == '\r');
                        bool isEsc = (Event.current.keyCode == KeyCode.Escape);
                        if (focusRename && isEnter)
                        {
                            tabs[activeTabIndex].name = tabRenameBuffer;
                            CancelRenamePopup();
                            isEditActive = false;
                            editTabOriginalIndex = null;
                            Event.current.Use();
                        }
                        else if (focusRename && isEsc)
                        {
                            // Anuluj jak przycisk X
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
                            Event.current.Use();
                        }
                    }

                    float xOk = arrowBtnSize * 2 + textFieldWidth;
                    if (GUI.Button(new Rect(xOk, 0, okWidth, arrowBtnSize), "OK", topButtonStyle))
                    {
                        tabs[activeTabIndex].name = tabRenameBuffer;
                        CancelRenamePopup();
                        isEditActive = false;
                        editTabOriginalIndex = null;
                    }

                    float xCancel = xOk + okWidth + 6f;
                    if (GUI.Button(new Rect(xCancel, 0, cancelWidth, arrowBtnSize), "X", topButtonStyle))
                    {
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

            string calcLabel = showCalc ? "Note" : "Calc";
            float calcWidth = buttonStyle.CalcSize(new GUIContent(calcLabel)).x + buttonPadding;
            if (GUILayout.Button(calcLabel, buttonStyle, GUILayout.Width(calcWidth), GUILayout.Height(topBarButtonHeight)))
                showCalc = !showCalc;

            string closeLabel = "X";
            float closeWidth = buttonStyleRed.CalcSize(new GUIContent(closeLabel)).x + buttonPadding;
            if (GUILayout.Button(closeLabel, buttonStyleRed, GUILayout.Width(closeWidth), GUILayout.Height(topBarButtonHeight)))
            {
                showWindow = false;
                if (btn != null) { try { btn.SetFalse(true); } catch { } }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.enabled = true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote] DrawTopBar exception: " + ex.Message);
        }
    }

    private void HandleTopBarDoubleClick()
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
