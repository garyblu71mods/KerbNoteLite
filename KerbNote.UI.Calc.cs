using System;
using System.Text.RegularExpressions;
using UnityEngine;

public partial class KerbNote
{
    private void EvaluateAndCommit()
    {
        try
        {
            double result = EvaluateExpression(calcInput);
            string formatted = result.ToString("G");
            CalcHistoryStore.Add($"{calcInput} = {formatted}");
            calcInput = formatted;
        }
        catch
        {
            CalcHistoryStore.Add($"{calcInput} = Error");
            calcInput = "Error";
        }
        GUI.FocusControl("CalcDisplay");
    }

    private static bool IsOpChar(char c) => c=='+'||c=='-'||c=='*'||c=='/';

    private string AppendSanitizedOp(string acc, char op)
    {
        if (string.IsNullOrEmpty(acc))
        {
            // Allow only leading '-'
            if (op == '-') return "-";
            return acc;
        }
        char last = acc[acc.Length - 1];
        if (IsOpChar(last))
        {
            // Special case: allow "*-" or "/-" for negative numbers
            if (op == '-' && (last == '*' || last == '/'))
            {
                return acc + '-';
            }
            // Replace all trailing operators with the new one
            int trim = acc.Length - 1;
            while (trim >= 0 && IsOpChar(acc[trim])) trim--;
            return acc.Substring(0, trim + 1) + op;
        }
        else
        {
            return acc + op;
        }
    }

    private string SanitizeCalcString(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        string acc = string.Empty;
        for (int i = 0; i < raw.Length; i++)
        {
            char ch = raw[i];
            if (char.IsDigit(ch)) { acc += ch; continue; }
            if (IsOpChar(ch)) { acc = AppendSanitizedOp(acc, ch); continue; }
            if (ch == '.')
            {
                int start = acc.Length - 1; while (start >= 0 && !IsOpChar(acc[start])) start--; string seg = acc.Substring(start + 1);
                if (seg.IndexOf('.') >= 0) continue; // skip duplicated dot
                if (seg.Length == 0) acc += "0";
                acc += '.';
                continue;
            }
            if (ch == '(' || ch == ')') { acc += ch; continue; }
            // ignore other characters (spaces, etc.)
        }
        return acc;
    }

    private void AppendCalcKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (key == "C") { calcInput = string.Empty; GUI.FocusControl("CalcDisplay"); return; }

        if (key.Length == 1 && IsOpChar(key[0]))
        {
            calcInput = AppendSanitizedOp(calcInput ?? string.Empty, key[0]);
            GUI.FocusControl("CalcDisplay");
            return;
        }

        if (key == ".")
        {
            int start = (calcInput?.Length ?? 0) - 1;
            string src = calcInput ?? string.Empty;
            while (start >= 0 && !IsOpChar(src[start])) start--;
            string segment = src.Substring(start + 1);
            if (segment.Contains(".")) { GUI.FocusControl("CalcDisplay"); return; }
            if (segment.Length == 0) calcInput += "0";
            calcInput += ".";
            GUI.FocusControl("CalcDisplay");
            return;
        }

        // digits and others
        calcInput += key;
        GUI.FocusControl("CalcDisplay");
    }

    void DrawDisplay()
    {
        if (calcDisplayStyle == null || buttonStyle == null)
        {
            try { InitStyles(); } catch (Exception ex) { Debug.LogWarning("[KerbNote][Calc] InitStyles (Display) failed: " + ex.Message); }
        }
        float displayX = CALC_DISPLAY_X;
        float displayY = CALC_DISPLAY_Y;
        float keypadX = windowRect.width - 230f;
        float backspaceOffset = 163f;
        float backspaceWidth = 46f;
        float backspaceX = keypadX + backspaceOffset;
        float displayWidth = backspaceX - displayX;
        float displayHeight = CALC_DISPLAY_HEIGHT;

        try
        {
            GUILayout.BeginArea(new Rect(displayX, displayY, displayWidth, displayHeight));
            var style = calcDisplayStyle;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.textField)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    padding = new RectOffset(8,8,6,6)
                };
                style.normal.textColor = new Color(1f, 0.95f, 0.3f);
                var bg = MakeSolidTexture(1, 1, new Color(0.12f, 0.12f, 0.12f, 0.92f));
                style.normal.background = bg;
                style.focused.background = bg;
                style.active.background = bg;
                style.hover.background = bg;
                style.onNormal.background = bg;
                style.onFocused.background = bg;
                style.onActive.background = bg;
                style.onHover.background = bg;
            }
            GUI.SetNextControlName("CalcDisplay");
            string before = calcInput;
            calcInput = GUILayout.TextField(calcInput, style, GUILayout.Height(30), GUILayout.ExpandWidth(true));
            // Sanity cleanup for keyboard typing (collapse multi-operators, keep *- and /-)
            string sanitized = SanitizeCalcString(calcInput);
            if (sanitized != calcInput) calcInput = sanitized;

            // Local key handling while TextField has focus
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.character == '\n' || e.character == '\r')
                {
                    EvaluateAndCommit();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape || e.character == (char)27)
                {
                    calcInput = string.Empty;
                    GUI.FocusControl("CalcDisplay");
                    e.Use();
                }
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(backspaceX, displayY, backspaceWidth, displayHeight));
            if (GUILayout.Button("<-", buttonStyle ?? GUI.skin.button, GUILayout.Width(backspaceWidth), GUILayout.Height(30)))
            {
                if (!string.IsNullOrEmpty(calcInput))
                    calcInput = calcInput.Substring(0, calcInput.Length - 1);
                GUI.FocusControl("CalcDisplay");
            }
            GUILayout.EndArea();
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote][Calc] DrawDisplay exception: " + ex.Message);
        }
    }

    void DrawCalcHistory()
    {
        if (buttonStyle == null)
        {
            try { InitStyles(); } catch (Exception ex) { Debug.LogWarning("[KerbNote][Calc] InitStyles (History) failed: " + ex.Message); }
        }
        float topMargin = 100f;
        float bottomMargin = 70f;
        float sideMargin = 12f;
        float historyHeight = windowRect.height - topMargin - bottomMargin;
        float minWidthMM = 30f;
        float dpi = Screen.dpi > 0 ? Screen.dpi : 96f;
        float minWidthPx = minWidthMM / 25.4f * dpi;
        float dynamicWidth = Mathf.Max(windowRect.width * 0.25f, minWidthPx);
        Rect historyAreaRect = new Rect(sideMargin, topMargin, dynamicWidth, historyHeight);

        try
        {
            GUIStyle historyStyle = new GUIStyle(GUI.skin.box);
            var bg = MakeSolidTexture(1, 1, new Color(0f, 0f, 0f, 0.3f));
            historyStyle.normal.background = bg;
            historyStyle.hover.background = bg;
            historyStyle.active.background = bg;
            historyStyle.focused.background = bg;
            historyStyle.normal.textColor = new Color(0.9f, 1f, 0.9f);
            historyStyle.fontSize = 13;
            historyStyle.padding = new RectOffset(6, 6, 6, 6);
            historyStyle.wordWrap = true;

            GUILayout.BeginArea(historyAreaRect);
            historyScroll = GUILayout.BeginScrollView(historyScroll);
            foreach (string entry in CalcHistoryStore.History)
                GUILayout.Label(entry, historyStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote][Calc] DrawCalcHistory exception: " + ex.Message);
        }
    }

    void DrawKeypad()
    {
        if (buttonStyle == null || buttonStyleRed == null)
        {
            try { InitStyles(); } catch (Exception ex) { Debug.LogWarning("[KerbNote][Calc] InitStyles (Keypad) failed: " + ex.Message); }
        }
        try
        {
            GUILayout.BeginArea(new Rect(windowRect.width - 230, 100, 210, 300));
            GUILayout.BeginVertical();
            string[] keys = { "7", "8", "9", "/", "4", "5", "6", "*", "1", "2", "3", "-", "C", "0", ".", "+" };
            float keyWidth = 46f;
            float keyHeight = 38f;
            float keyPadSpacing = 3f;
            for (int row = 0; row < 4; row++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(keyPadSpacing);
                for (int col = 0; col < 4; col++)
                {
                    string key = keys[row * 4 + col];
                    if (GUILayout.Button(key, buttonStyle ?? GUI.skin.button, GUILayout.Width(keyWidth), GUILayout.Height(keyHeight)))
                    {
                        AppendCalcKey(key);
                    }
                    GUILayout.Space(keyPadSpacing);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(keyPadSpacing);
            }
            GUILayout.Space(3);
            if (GUILayout.Button("Boom", buttonStyleRed ?? (buttonStyle ?? GUI.skin.button), GUILayout.Width(202), GUILayout.Height(44)))
            {
                EvaluateAndCommit();
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote][Calc] DrawKeypad exception: " + ex.Message);
        }
    }
}
