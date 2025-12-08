using UnityEngine;
using System;
using System.IO;

public static class KerbalUIBackground
{
    private static Texture2D backgroundTexture;
    private static Texture2D noteWindowTexture;

    public static void LoadTexture()
    {
        // Prefer SkinAssets-resolved textures if available (file-based), else fallback to default DB textures
        var bgFromSkin = SkinAssets.Get("BackgroundWindow");
        var noteFromSkin = SkinAssets.Get("NoteWindow");
        if (bgFromSkin != null) backgroundTexture = bgFromSkin; else backgroundTexture = GameDatabase.Instance.GetTexture("KerbNoteLite/Textures/BackgroundWindow", false);
        if (noteFromSkin != null) noteWindowTexture = noteFromSkin; else noteWindowTexture = GameDatabase.Instance.GetTexture("KerbNoteLite/Textures/NoteWindow", false);

        if (backgroundTexture == null)
        {
            Debug.LogWarning("[KerbalUIBackground] Nie udalo sie zaladowac Textures/BackgroundWindow");
        }
        
        if (noteWindowTexture == null)
        {
            Debug.LogWarning("[KerbalUIBackground] Nie udalo sie zaladowac Textures/NoteWindow");
        }
    }

    // Inject pre-resolved textures
    public static void OverrideWithTextures(Texture2D background, Texture2D note)
    {
        try
        {
            if (note != null) noteWindowTexture = note;
            if (background != null) backgroundTexture = background;
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbalUIBackground] OverrideWithTextures error: " + ex.Message);
        }
    }

    public static Texture2D CurrentBackground => backgroundTexture;
    public static Texture2D CurrentNote => noteWindowTexture;

    // Prefer GameDatabase (supports .dds/.png), fallback to direct file loading from a given Textures folder (exact names only)
    public static void OverrideFromFolderOrUrl(string texturesFolder)
    {
        if (string.IsNullOrEmpty(texturesFolder)) return;
        string urlRoot = TryBuildUrlRootFromFolder(texturesFolder);
        bool fromDb = false;
        if (!string.IsNullOrEmpty(urlRoot))
        {
            fromDb = TryOverrideFromGameDb(urlRoot);
        }
        if (!fromDb)
        {
            OverrideFromFolderExact(texturesFolder);
        }
        Debug.Log("[KerbalUIBackground] Background set: bg=" + (backgroundTexture!=null) + ", note=" + (noteWindowTexture!=null));
    }

    // Folder-based override: only exact file names
    private static void OverrideFromFolderExact(string texturesFolder)
    {
        try
        {
            var notePath = Path.Combine(texturesFolder, "NoteWindow.png");
            if (File.Exists(notePath))
            {
                var note = TryLoadTextureFromFile(notePath);
                if (note != null) noteWindowTexture = note;
            }
            else
            {
                Debug.LogWarning("[KerbalUIBackground] Missing NoteWindow.png in: " + texturesFolder);
            }
            var bgPath = Path.Combine(texturesFolder, "BackgroundWindow.png");
            if (File.Exists(bgPath))
            {
                var bg = TryLoadTextureFromFile(bgPath);
                if (bg != null) backgroundTexture = bg;
            }
            else
            {
                Debug.LogWarning("[KerbalUIBackground] Missing BackgroundWindow.png in: " + texturesFolder);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbalUIBackground] OverrideFromFolderExact error: " + ex.Message);
        }
    }

    private static bool TryOverrideFromGameDb(string urlRoot)
    {
        bool ok = false;
        var noteUrl = urlRoot.TrimEnd('/') + "/NoteWindow";
        var note = GameDatabase.Instance.GetTexture(noteUrl, false);
        if (note != null) { noteWindowTexture = note; ok = true; }
        var bgUrl = urlRoot.TrimEnd('/') + "/BackgroundWindow";
        var bg = GameDatabase.Instance.GetTexture(bgUrl, false);
        if (bg != null) { backgroundTexture = bg; ok = true; }
        return ok;
    }

    private static string TryBuildUrlRootFromFolder(string texturesFolder)
    {
        try
        {
            var idx = texturesFolder.IndexOf("GameData", StringComparison.OrdinalIgnoreCase);
            if (idx <0) return null;
            var rel = texturesFolder.Substring(idx + "GameData".Length).TrimStart('\\','/');
            return rel.Replace('\\','/');
        }
        catch { return null; }
    }

    private static Texture2D TryLoadTextureFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return null;
            byte[] data = File.ReadAllBytes(filePath);
            var tex = new Texture2D(2,2, TextureFormat.RGBA32, false);
            var imgConvType = typeof(Texture2D).Assembly.GetType("UnityEngine.ImageConversion");
            bool loaded = false;
            if (imgConvType != null)
            {
                var mi = imgConvType.GetMethod("LoadImage", new Type[] { typeof(Texture2D), typeof(byte[]) });
                if (mi == null)
                {
                    // Alternate signature with markNonReadable
                    mi = imgConvType.GetMethod("LoadImage", new Type[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });
                    if (mi != null)
                    {
                        loaded = (bool)mi.Invoke(null, new object[] { tex, data, false });
                    }
                }
                else
                {
                    loaded = (bool)mi.Invoke(null, new object[] { tex, data });
                }
            }
            if (!loaded)
            {
                // Try instance method fallback if present
                var miInst = typeof(Texture2D).GetMethod("LoadImage", new Type[] { typeof(byte[]) });
                if (miInst != null)
                {
                    loaded = (bool)miInst.Invoke(tex, new object[] { data });
                }
            }
            if (loaded)
            {
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                return tex;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbalUIBackground] TryLoadTextureFromFile failed for '" + filePath + "': " + ex.Message);
        }
        return null;
    }

    public static void Draw(Rect rect)
    {
        if (backgroundTexture == null)
        {
            backgroundTexture = new Texture2D(1,1);
            backgroundTexture.SetPixel(0,0, new Color(0.1f,0.1f,0.1f,0.8f));
            backgroundTexture.Apply();
        }

        GUI.DrawTexture(rect, backgroundTexture);
    }

    public static void DrawNoteWindow(Rect rect)
    {
        if (noteWindowTexture == null)
        {
            noteWindowTexture = new Texture2D(1,1);
            noteWindowTexture.SetPixel(0,0, new Color(0.12f,0.12f,0.12f,0.85f));
            noteWindowTexture.Apply();
        }

        GUI.DrawTexture(rect, noteWindowTexture);
    }
}
