using System;
using System.Linq;
using UnityEngine;

public partial class KerbNote
{
    // --- AAA Button below note ---
    void DrawAAABtnBelowNote()
    {
        float btnWidth = AAA_BTN_WIDTH;
        float btnHeight = AAA_BTN_HEIGHT;
        float btnX = NOTE_X +20f;
        float btnY = aaaBtnY; // U?yj pozycji pod notewindow
        Rect btnRect = new Rect(btnX, btnY, btnWidth, btnHeight);

        // AAA button (no on-screen labels; logs only to console)
        GUIStyle aaaBtnStyle = new GUIStyle(buttonStyle);
        bool pressed = false;
        if (Event.current.type == EventType.MouseDown && btnRect.Contains(Event.current.mousePosition))
        {
            Debug.Log("[KerbNote] AAA area MouseDown at " + Event.current.mousePosition);
            pressed = true;
            isClickOnAAA = true;
            isDraggingWindow = false; // wymuszamy reset dragowania okna
            Event.current.Use();
        }
        if (pressed)
        {
            noteZoomLevel = (noteZoomLevel +1) %5;
        }
        if (AAATexture != null)
        {
            float iconMaxWidth = btnWidth -8f;
            float iconMaxHeight = btnHeight -8f;
            float iconAspect =128f /64f;
            float iconWidth = iconMaxWidth;
            float iconHeight = iconMaxWidth / iconAspect;
            if (iconHeight > iconMaxHeight)
            {
                iconHeight = iconMaxHeight;
                iconWidth = iconMaxHeight * iconAspect;
            }
            float iconX = btnX + (btnWidth - iconWidth) /2f;
            float iconY = btnY + (btnHeight - iconHeight) /2f;
            GUI.DrawTexture(new Rect(iconX, iconY, iconWidth, iconHeight), AAATexture);
        }
    }

    // --- Stan do poprawy logiki zaznaczania w notatniku ---
    private bool noteMouseDownInField = false;
    private bool noteMouseDragged = false;
    private Vector2 noteMouseDownPos = Vector2.zero;
    private bool noteJustFocusedByCode = false;
    
    // Cache for note area style to avoid recreation every frame
    private GUIStyle cachedNoteAreaStyle;
    private int cachedNoteAreaStyleFontSize = -1;

    // Poprawiona metoda DrawNoteArea (tylko jedna w klasie)
    void DrawNoteArea()
    {
        float leftMargin = NOTE_BG_MARGIN +10f;
        float rightMargin = NOTE_BG_MARGIN +10f;
        float topMargin = NOTE_TOP_MARGIN;
        float bottomMargin = NOTE_BOTTOM_MARGIN;
        float noteWidth = windowRect.width - leftMargin - rightMargin -2f;
        float noteX = leftMargin +1f;
        float usableHeight = GetSafeNoteAreaHeight(windowRect, topMargin, bottomMargin);
        Rect areaRect = new Rect(noteX, topMargin, noteWidth, usableHeight);

        // Zapami?taj pozycj? Y dolnej kraw?dzi notewindow do AAA
        aaaBtnY = areaRect.yMax + AAA_BTN_Y_OFFSET;

        NoteTab tab = tabs[activeTabIndex];

        int[] zoomFontSizes = {14,16,18,20,14 };
        int fontSize = zoomFontSizes[Mathf.Clamp(noteZoomLevel,0,4)];

        // CACHED STYLE - only recreate if font size changed
        if (cachedNoteAreaStyle == null || cachedNoteAreaStyleFontSize != fontSize)
        {
            cachedNoteAreaStyle = new GUIStyle(GUI.skin.textArea);
            cachedNoteAreaStyle.fontSize = fontSize;
            cachedNoteAreaStyle.fontStyle = FontStyle.Bold;
            cachedNoteAreaStyle.wordWrap = true;
            cachedNoteAreaStyle.richText = true;
            cachedNoteAreaStyle.normal.textColor = new Color(0.95f,0.95f,0.85f);
            cachedNoteAreaStyle.focused.textColor = new Color(1f,1f,1f);
            cachedNoteAreaStyle.normal.background = null;
            cachedNoteAreaStyle.focused.background = null;
            cachedNoteAreaStyle.active.background = null;
            cachedNoteAreaStyle.hover.background = null;
            cachedNoteAreaStyle.padding = new RectOffset(16,16,16,16);
            cachedNoteAreaStyle.border = new RectOffset(4,4,4,4);
            cachedNoteAreaStyle.alignment = TextAnchor.UpperLeft;
            cachedNoteAreaStyleFontSize = fontSize;
        }

        // Tekstura t?a pod notatk? (from skin/router)
        if (noteAreaTex == null)
        {
            noteAreaTex = SkinAssets.Get("NoteWindow") ?? GameDatabase.Instance.GetTexture(TEXTURE_NOTE_WINDOW, false);
        }
        if (noteAreaTex != null)
            GUI.DrawTexture(areaRect, noteAreaTex);

        // Oblicz wysoko?? potrzebn? dla tekstu + dodaj padding na ko?cu
        float scrollViewWidth = noteWidth -20f;
        float contentHeight = cachedNoteAreaStyle.CalcHeight(new GUIContent(tab.text), scrollViewWidth);
        // Dodaj padding równy wysoko?ci widoku, ?eby mo?na by?o scrollowa? ni?ej ni? tekst
        float paddingHeight = usableHeight * 0.8f;
        float totalContentHeight = contentHeight + paddingHeight;

        // --- Obs?uga myszy dla poprawnego focusu i zaznaczania (bez po?erania kliknie?) ---
        Event ePre = Event.current;
        if (!showRenamePopup)
        {
            if (ePre.type == EventType.MouseDown && areaRect.Contains(ePre.mousePosition))
            {
                noteMouseDownInField = true;
                noteMouseDragged = false;
                noteMouseDownPos = ePre.mousePosition;
                GUI.FocusControl("NoteField");
            }
            else if (ePre.type == EventType.MouseDrag && noteMouseDownInField)
            {
                if ((ePre.mousePosition - noteMouseDownPos).sqrMagnitude >2f *2f)
                    noteMouseDragged = true;
            }
            else if (ePre.type == EventType.MouseUp && noteMouseDownInField)
            {
                if (!noteMouseDragged && ePre.clickCount ==1)
                {
                    TextEditor te0 = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    if (te0 != null && GUI.GetNameOfFocusedControl() == "NoteField")
                    {
                        te0.selectIndex = te0.cursorIndex;
                    }
                }
                noteMouseDownInField = false;
                noteMouseDragged = false;
            }
        }

        // Rozpocznij obszar scrollowania z paddingiem
        tab.scroll = GUI.BeginScrollView(areaRect, tab.scroll, new Rect(0, 0, scrollViewWidth, totalContentHeight));

        // Blokuj edycj? gdy popup rename
        bool wasEnabled = GUI.enabled;
        if (showRenamePopup)
            GUI.enabled = false;

        GUI.SetNextControlName("NoteField");
        string prevText = tab.text;

        // Pole tekstowe notatki ze scrollowaniem (using cached style)
        tab.text = GUI.TextArea(
            new Rect(0, 0, scrollViewWidth, contentHeight),
            tab.text,
            cachedNoteAreaStyle
        );

        // --- Push to undo stack on change ---
        if (tab.text != prevText)
        {
            if (tab.undoStack.Count == 0 || tab.undoStack.Peek() != prevText)
                tab.undoStack.Push(prevText);
        }

        // --- Ctrl+Z undo (robust) ---
        Event e = Event.current;
        bool ctrlHeld = e.control || (e.modifiers & EventModifiers.Control) != 0 || e.command;
        bool noteFocused = GUI.GetNameOfFocusedControl() == "NoteField";
        bool mouseOverNote = areaRect.Contains(e.mousePosition);
        if (!showRenamePopup && (noteFocused || mouseOverNote) &&
            (e.type == EventType.KeyDown || e.type == EventType.KeyUp) && ctrlHeld && e.keyCode == KeyCode.Z)
        {
            if (tab.undoStack.Count > 0)
            {
                tab.text = tab.undoStack.Pop();
                GUI.FocusControl("NoteField");
                TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                if (te != null)
                {
                    te.cursorIndex = tab.text.Length;
                    te.selectIndex = te.cursorIndex;
                }
                GUI.changed = true;
            }
            e.Use();
        }

        // --- Tab inserts10 spaces in note field ---
        if (!showRenamePopup && noteFocused && e.type == EventType.KeyDown && e.keyCode == KeyCode.Tab)
        {
            const int TAB_SPACES = 10;
            string TAB_INSERT = new string(' ', TAB_SPACES);
            TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (te != null)
            {
                int start = Mathf.Min(te.cursorIndex, te.selectIndex);
                int end = Mathf.Max(te.cursorIndex, te.selectIndex);
                start = Mathf.Clamp(start, 0, tab.text != null ? tab.text.Length : 0);
                end = Mathf.Clamp(end, 0, tab.text != null ? tab.text.Length : 0);

                if (tab.undoStack.Count == 0 || tab.undoStack.Peek() != prevText)
                    tab.undoStack.Push(prevText);

                string before = (tab.text ?? string.Empty).Substring(0, start);
                string after = (tab.text ?? string.Empty).Substring(end);
                tab.text = before + TAB_INSERT + after;

                int newCaret = start + TAB_INSERT.Length;
                te.cursorIndex = newCaret;
                te.selectIndex = newCaret;
                GUI.changed = true;
            }
            e.Use();
        }

        // Poprawione auto-scroll do kursora podczas pisania
        if ((Event.current.type == EventType.KeyDown || Event.current.type == EventType.KeyUp ||
            (Event.current.type == EventType.Repaint && prevText != tab.text)) && noteFocused)
        {
            TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (editor != null)
            {
                float cursorY = editor.graphicalCursorPos.y;
                float viewHeight = usableHeight;
                
                // Gdy kursor jest poni?ej po?owy okna, scrolluj tak ?eby by? w górnej po?owie
                if (cursorY > tab.scroll.y + viewHeight * 0.5f)
                {
                    // Scrolluj tak, ?eby kursor by? w górnej cz??ci (oko?o 20% od góry)
                    float targetY = cursorY - viewHeight * 0.2f;
                    targetY = Mathf.Max(0, targetY);
                    targetY = Mathf.Min(targetY, totalContentHeight - viewHeight);
                    tab.scroll.y = targetY;
                }
                // Gdy kursor jest powy?ej widocznego obszaru
                else if (cursorY < tab.scroll.y)
                {
                    float targetY = cursorY - viewHeight * 0.2f;
                    targetY = Mathf.Max(0, targetY);
                    tab.scroll.y = targetY;
                }
            }
        }

        // Utrzymuj fokus na polu tekstowym gdy potrzeba
        if (!showRenamePopup)
        {
            if (Event.current.type == EventType.MouseDown && areaRect.Contains(Event.current.mousePosition))
            {
                noteJustFocusedByCode = true;
            }
            else if (Event.current.type == EventType.KeyDown && !isEditActive && 
                     Event.current.keyCode != KeyCode.Escape)
            {
                GUI.FocusControl("NoteField");
                noteJustFocusedByCode = true;
            }
        }

        if (noteJustFocusedByCode && Event.current.type == EventType.Repaint)
        {
            TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (te != null && GUI.GetNameOfFocusedControl() == "NoteField")
            {
                te.selectIndex = te.cursorIndex;
            }
            noteJustFocusedByCode = false;
        }

        GUI.enabled = wasEnabled;
        GUI.EndScrollView();

        // --- Subtelna informacja o alarmie w prawym górnym rogu notatki ---
        try
        {
            var alarm = AlarmManager.GetAlarmsForTab(tabs[activeTabIndex].guid).FirstOrDefault(a => a.Enabled);
            if (alarm != null && !string.IsNullOrEmpty(alarm.BodyName))
            {
                string FriendlySituation(Vessel.Situations s)
                {
                    switch (s)
                    {
                        case Vessel.Situations.PRELAUNCH: return "Prelaunch";
                        case Vessel.Situations.FLYING: return "Flying";
                        case Vessel.Situations.ORBITING: return "Orbiting";
                        case Vessel.Situations.SUB_ORBITAL: return "Sub-orbital";
                        case Vessel.Situations.ESCAPING: return "Exiting SOI";
                        case Vessel.Situations.LANDED: return "Landed";
                        case Vessel.Situations.SPLASHED: return "Splashed";
                        case Vessel.Situations.DOCKED: return "Docked";
                        default: return s.ToString();
                    }
                }
                string info = "alarm for " + FriendlySituation(alarm.Situation) + "-" + alarm.BodyName;
                GUIStyle smallInfo = new GUIStyle(GUI.skin.label);
                smallInfo.fontSize =11;
                smallInfo.normal.textColor = new Color(1f,1f,1f,0.6f);
                smallInfo.alignment = TextAnchor.UpperRight;
                Rect infoRect = new Rect(areaRect.x +6f, areaRect.y +4f, areaRect.width -12f,16f);
                GUI.Label(infoRect, info, smallInfo);
            }
        }
        catch { }
    }
}
