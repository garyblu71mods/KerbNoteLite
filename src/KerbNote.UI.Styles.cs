using UnityEngine;
using System.Collections.Generic;

public partial class KerbNote
{
    // Cache for solid color textures to avoid recreation
    private static readonly Dictionary<Color, Texture2D> solidTextureCache = new Dictionary<Color, Texture2D>();
    
    Texture2D MakeSolidTexture(int width, int height, Color color)
    {
        // Check cache first
        if (solidTextureCache.TryGetValue(color, out Texture2D cached))
        {
            return cached;
        }
        
        // Create new texture only if not cached
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        
        // Cache it for future use
        solidTextureCache[color] = tex;
        return tex;
    }

    private GUIStyle BuildButtonStyleFromTextures()
    {
        // Ensure textures exist
        if (ButtonTexture == null) ButtonTexture = SkinAssets.Get("Button") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON, false);
        if (ButtonHoverTexture == null) ButtonHoverTexture = SkinAssets.Get("ButtonHover") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_HOVER, false);
        if (ButtonClickTexture == null) ButtonClickTexture = SkinAssets.Get("ButtonClick") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_CLICK, false);
        if (ButtonTexture == null) return null;
        var s = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        s.normal.textColor = Color.white;
        s.normal.background = ButtonTexture;
        s.hover.background = ButtonHoverTexture ?? ButtonTexture;
        s.active.background = ButtonClickTexture ?? ButtonTexture;
        return s;
    }

    private GUIStyle BuildRedButtonStyleFromTextures()
    {
        var red = SkinAssets.Get("red_Button") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_RED, false);
        if (red == null) return null;
        var s = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        s.normal.textColor = new Color(1f,0.4f,0.4f);
        s.normal.background = red;
        s.hover.background = red;
        s.active.background = red;
        return s;
    }

    void InitStyles()
    {
        if (stylesInitialized) return;
        stylesInitialized = true;

        // Load textures only once during initialization
        if (noteTex == null)
        {
            noteTex = SkinAssets.Get("BackgroundWindow") ?? GameDatabase.Instance.GetTexture(TEXTURE_BACKGROUND_WINDOW, false);
            if (noteTex == null)
            {
                Debug.LogWarning("[KerbNote] BackgroundWindow.png not found – using fallback");
                noteTex = MakeSolidTexture(1,1, new Color(0.1f,0.1f,0.1f,0.0f));
            }
        }

        // Rename field style
        renameFieldStyle = new GUIStyle(GUI.skin.textField);
        renameFieldStyle.fontSize =14;
        renameFieldStyle.normal.textColor = Color.white;
        renameFieldStyle.normal.background = MakeSolidTexture(1,1, new Color(0f,0f,0f,0.0f));

        // Button styles - reuse already loaded textures from fields
        if (ButtonTexture == null) ButtonTexture = SkinAssets.Get("Button") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON, false);
        if (ButtonHoverTexture == null) ButtonHoverTexture = SkinAssets.Get("ButtonHover") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_HOVER, false);
        if (ButtonClickTexture == null) ButtonClickTexture = SkinAssets.Get("ButtonClick") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_CLICK, false);

        buttonStyle = new GUIStyle(GUI.skin.button);
        if (ButtonTexture != null)
        {
            buttonStyle.normal.background = ButtonTexture;
            buttonStyle.hover.background = ButtonHoverTexture ?? ButtonTexture;
            buttonStyle.active.background = ButtonClickTexture ?? ButtonTexture;
        }
        buttonStyle.fontSize =14;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.normal.textColor = Color.white;

        // Red button style - load only if needed
        buttonStyleRed = new GUIStyle(GUI.skin.button);
        Texture2D redButtonTex = SkinAssets.Get("red_Button") ?? GameDatabase.Instance.GetTexture(TEXTURE_BUTTON_RED, false);
        if (redButtonTex != null)
        {
            buttonStyleRed.normal.background = redButtonTex;
            buttonStyleRed.hover.background = redButtonTex;
            buttonStyleRed.active.background = redButtonTex;
        }
        buttonStyleRed.fontSize =16;
        buttonStyleRed.fontStyle = FontStyle.Bold;
        buttonStyleRed.alignment = TextAnchor.MiddleCenter;
        buttonStyleRed.normal.textColor = new Color(1f,0.4f,0.4f);

        // Note style
        noteStyle = new GUIStyle(GUI.skin.box);
        noteStyle.padding = new RectOffset(12, 12, 12, 12);
        noteStyle.border = new RectOffset(8, 8, 8, 8);
        noteStyle.normal.textColor = Color.white;
        noteStyle.fontSize = 15;
        noteStyle.alignment = TextAnchor.UpperLeft;
        noteStyle.normal.background = MakeSolidTexture(1, 1, new Color(0f, 0f, 0f, 0f));

        // Calculator display style
        calcDisplayStyle = new GUIStyle(GUI.skin.textField);
        calcDisplayStyle.fontSize = 20;
        calcDisplayStyle.fontStyle = FontStyle.Bold;
        calcDisplayStyle.alignment = TextAnchor.MiddleRight;
        calcDisplayStyle.padding = new RectOffset(8, 8, 6, 6);
        calcDisplayStyle.normal.textColor = new Color(1f, 0.95f, 0.3f);
        calcDisplayStyle.normal.background = MakeSolidTexture(1, 1, new Color(0.12f, 0.12f, 0.12f, 0.92f));
        calcDisplayStyle.hover.background = calcDisplayStyle.normal.background;
        calcDisplayStyle.focused.background = calcDisplayStyle.normal.background;
        calcDisplayStyle.active.background = calcDisplayStyle.normal.background;

        // Text area style
        textAreaStyle = new GUIStyle(GUI.skin.textArea);
        textAreaStyle.fontSize = 18;
        textAreaStyle.fontStyle = FontStyle.Bold;
        textAreaStyle.wordWrap = true;
        textAreaStyle.richText = true;
        textAreaStyle.normal.textColor = new Color(0.95f, 0.95f, 0.85f);
        textAreaStyle.normal.background = MakeSolidTexture(1, 1, new Color(0f, 0f, 0f, 0.0f));
        textAreaStyle.padding = new RectOffset(6, 6, 6, 6);
        textAreaStyle.border = new RectOffset(4, 4, 4, 4);
        textAreaStyle.alignment = TextAnchor.UpperLeft;
    }
}
