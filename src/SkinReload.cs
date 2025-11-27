using System;
using System.Reflection;
using KSP.UI.Screens;
using UnityEngine;

public static class SkinReload
{
    public static void Reload(KerbNote host)
    {
        if (host == null) return;
        try
        {
            // Resolve all textures via SkinAssets with final DB fallback for core mod defaults
            var resize = SkinAssets.Get("resize") ?? GameDatabase.Instance.GetTexture("KerbCalcProject/Textures/resize", false);
            var tab = SkinAssets.Get("Tab") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB, false);
            var tabHover = SkinAssets.Get("TabHover") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB_HOVER, false);
            var tabClick = SkinAssets.Get("TabClick") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB_CLICK, false);
            var btn = SkinAssets.Get("Button") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_BUTTON, false);
            var btnHover = SkinAssets.Get("ButtonHover") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_BUTTON_HOVER, false);
            var btnClick = SkinAssets.Get("ButtonClick") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_BUTTON_CLICK, false);
            var aaa = SkinAssets.Get("AAA") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_AAA, false);
            var bg = SkinAssets.Get("BackgroundWindow") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_BACKGROUND_WINDOW, false);
            var note = SkinAssets.Get("NoteWindow") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_NOTE_WINDOW, false);
            var iconOn = SkinAssets.Get("IconOn") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_ICON_ON, false);
            var iconOff = SkinAssets.Get("IconOff") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_ICON_OFF, false);

            // If any of the core button textures are null, hard-fallback to Green pack for all UI textures
            if (btn == null || btnHover == null || btnClick == null)
            {
                string greenUrl = "KerbCalcProject/texture_pack/Green/Textures";
                btn = GameDatabase.Instance.GetTexture(greenUrl + "/Button", false) ?? btn;
                btnHover = GameDatabase.Instance.GetTexture(greenUrl + "/ButtonHover", false) ?? btnHover;
                btnClick = GameDatabase.Instance.GetTexture(greenUrl + "/ButtonClick", false) ?? btnClick;
                tab = GameDatabase.Instance.GetTexture(greenUrl + "/Tab", false) ?? tab;
                tabHover = GameDatabase.Instance.GetTexture(greenUrl + "/TabHover", false) ?? tabHover;
                tabClick = GameDatabase.Instance.GetTexture(greenUrl + "/TabClick", false) ?? tabClick;
                bg = GameDatabase.Instance.GetTexture(greenUrl + "/BackgroundWindow", false) ?? bg;
                note = GameDatabase.Instance.GetTexture(greenUrl + "/NoteWindow", false) ?? note;
                aaa = GameDatabase.Instance.GetTexture(greenUrl + "/AAA", false) ?? aaa;
            }

            // Log what was found for debugging
            Debug.Log($"[SkinReload] Assets: Button={(btn!=null)}, Hover={(btnHover!=null)}, Click={(btnClick!=null)}, Tab={(tab!=null)}, BG={(bg!=null)}, Note={(note!=null)}");

            SetField(host, "resizeIcon", resize);
            SetField(host, "TabTexture", tab);
            SetField(host, "TabHoverTexture", tabHover);
            SetField(host, "TabClickTexture", tabClick);
            SetField(host, "ButtonTexture", btn);
            SetField(host, "ButtonHoverTexture", btnHover);
            SetField(host, "ButtonClickTexture", btnClick);
            SetField(host, "AAATexture", aaa);
            SetField(host, "noteTex", bg);
            SetField(host, "noteAreaTex", note);
            SetField(host, "iconOn", iconOn);
            SetField(host, "iconOff", iconOff);

            // Force style rebuild
            SetField(host, "stylesInitialized", false);
            InvokeMethod(host, "InitStyles");
        }
        catch (Exception ex)
        {
            Debug.LogError("[SkinReload] Reload failed: " + ex.Message);
        }
    }

    private static void SetField(object obj, string fieldName, object value)
    {
        if (obj == null) return;
        var t = obj.GetType();
        var f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f != null)
        {
            try { f.SetValue(obj, value); } catch { }
        }
    }

    private static void InvokeMethod(object obj, string name)
    {
        if (obj == null) return;
        var t = obj.GetType();
        var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (m != null)
        {
            try { m.Invoke(obj, null); } catch { }
        }
    }
}
