using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class SkinAssets
{
 private static string currentUrlRoot; // url root for GameDatabase lookup (e.g., KerbCalcProject/texture_pack/<Pack>/Textures)
 private static string currentFolder;   // absolute folder path
 private static string fallbackUrlRoot; // url root for fallback pack (e.g., Stock/Green)
 private static string fallbackFolder;  // absolute folder path for fallback
 private static bool useFileOnly = false; // when true, skip DB; default false -> prefer DB first

 public static bool DebugTrace = true;

 private const string DefaultUrlRoot = "KerbCalcProject/Textures";
 
 // Cache for loaded textures to avoid repeated file/DB lookups
 private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
 
 public static string CurrentFolder => currentFolder;
 public static string FallbackFolder => fallbackFolder;
 public static string CurrentUrl => currentUrlRoot;
 public static string FallbackUrl => fallbackUrlRoot;
 
 public static void Configure(string currentTexturesFolder, string fallbackTexturesFolder = null, bool fileOnly = false)
 {
  currentFolder = currentTexturesFolder;
  currentUrlRoot = BuildUrlRootFromFolder(currentTexturesFolder);

  fallbackFolder = fallbackTexturesFolder;
  fallbackUrlRoot = string.IsNullOrEmpty(fallbackTexturesFolder) ? null : BuildUrlRootFromFolder(fallbackTexturesFolder);

  useFileOnly = fileOnly;
  
  // Clear cache when configuration changes (skin change)
  textureCache.Clear();
  
  try { Debug.Log("[SkinAssets] Configure: currentFolder=" + currentFolder + ", currentUrl=" + currentUrlRoot + ", fallbackFolder=" + fallbackFolder + ", fallbackUrl=" + fallbackUrlRoot + ", fileOnly=" + useFileOnly); } catch {}
 }

 // Resolve by asset base name (e.g., "Tab", "Button", "IconOn")
 public static Texture2D Get(string assetBaseName)
 {
  if (string.IsNullOrEmpty(assetBaseName)) return null;
  
  // Check cache first - massive performance boost for repeated lookups
  string cacheKey = currentFolder + "|" + assetBaseName;
  if (textureCache.TryGetValue(cacheKey, out Texture2D cached))
  {
   return cached;
  }
  
  Texture2D tex = null;

  if (!useFileOnly)
  {
   foreach (var name in EnumerateNameVariants(assetBaseName))
   {
    tex = TryGetFromUrl(currentUrlRoot, name, "current");
    if (tex != null) break;
   }
  }
  
  if (tex == null)
  {
   tex = TryGetFromFileSmart(currentFolder, currentUrlRoot, assetBaseName, "current-file");
  }
  
  if (tex == null && !useFileOnly)
  {
   foreach (var name in EnumerateNameVariants(assetBaseName))
   {
    tex = TryGetFromUrl(fallbackUrlRoot, name, "fallback");
    if (tex != null) break;
   }
  }
  
  if (tex == null)
  {
   tex = TryGetFromFileSmart(fallbackFolder, fallbackUrlRoot, assetBaseName, "fallback-file");
  }

  if (tex == null)
  {
   try { Debug.LogWarning("[SkinAssets] Missing asset '" + assetBaseName + "' in current/fallback folders. Using default."); } catch {}
   tex = TryGetFromUrl(DefaultUrlRoot, assetBaseName, "default");
  }
  
  // Cache result (even if null to avoid repeated expensive lookups)
  textureCache[cacheKey] = tex;
  return tex;
 }

 private static IEnumerable<string> EnumerateNameVariants(string assetBaseName)
 {
  if (string.IsNullOrEmpty(assetBaseName)) yield break;
  var s = assetBaseName;
  var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
  Func<string, bool> add = n => { if (string.IsNullOrEmpty(n)) return false; if (seen.Contains(n)) return false; seen.Add(n); return true; };

  if (add(s)) yield return s;
  if (add(s.ToLowerInvariant())) yield return s.ToLowerInvariant();
  var r1 = s.Replace('_','-'); if (add(r1)) yield return r1; if (add(r1.ToLowerInvariant())) yield return r1.ToLowerInvariant();
  var r2 = s.Replace('-','_'); if (add(r2)) yield return r2; if (add(r2.ToLowerInvariant())) yield return r2.ToLowerInvariant();
  var r3 = RemoveSeparators(s); if (add(r3)) yield return r3; if (add(r3.ToLowerInvariant())) yield return r3.ToLowerInvariant();
 }

 private static string RemoveSeparators(string s)
 {
  var chars = s.Where(c => c != '_' && c != '-').ToArray();
  return new string(chars);
 }

 private static Texture2D TryGetFromUrl(string urlRoot, string assetBaseName, string sourceTag)
 {
  if (string.IsNullOrEmpty(urlRoot)) return null;
  try
  {
   var tex = GameDatabase.Instance.GetTexture(urlRoot.TrimEnd('/') + "/" + assetBaseName, false);
   if (tex != null && DebugTrace)
   {
    try { Debug.Log("[SkinAssets] Hit " + sourceTag + " url: " + urlRoot + "/" + assetBaseName + " => tex '" + tex.name + "' (" + tex.width + "x" + tex.height + ")"); } catch {}
   }
   return tex;
  }
  catch { return null; }
 }

 private static bool TryLoadImageBytes(byte[] data, out Texture2D tex)
 {
  tex = new Texture2D(2,2, TextureFormat.RGBA32, false);
  try
  {
   var imgConvType = typeof(Texture2D).Assembly.GetType("UnityEngine.ImageConversion");
   bool loaded = false;
   if (imgConvType != null)
   {
    var mi = imgConvType.GetMethod("LoadImage", new Type[] { typeof(Texture2D), typeof(byte[]) });
    if (mi == null)
    {
     mi = imgConvType.GetMethod("LoadImage", new Type[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });
     if (mi != null) loaded = (bool)mi.Invoke(null, new object[] { tex, data, false });
    }
    else
    {
      loaded = (bool)mi.Invoke(null, new object[] { tex, data });
    }
   }
   if (!loaded)
   {
    var miInst = typeof(Texture2D).GetMethod("LoadImage", new Type[] { typeof(byte[]) });
    if (miInst != null) loaded = (bool)miInst.Invoke(tex, new object[] { data });
   }
   if (!loaded) { tex = null; return false; }
   tex.filterMode = FilterMode.Bilinear;
   tex.wrapMode = TextureWrapMode.Clamp;
   return true;
  }
  catch { tex = null; return false; }
 }

 private static Texture2D TryGetFromFileSmart(string folder, string urlRoot, string assetBaseName, string sourceTag)
 {
  if (string.IsNullOrEmpty(folder)) return null;
  try
  {
   var path = Path.Combine(folder, assetBaseName + ".png");
   if (File.Exists(path))
   {
    var data = File.ReadAllBytes(path);
    if (TryLoadImageBytes(data, out var tex))
    {
     if (DebugTrace) try { Debug.Log("[SkinAssets] Hit " + sourceTag + " file: " + path + " => tex '" + tex.name + "' (" + tex.width + "x" + tex.height + ")"); } catch {}
     return tex;
    }
    return null;
   }

   string[] supported = new [] { ".png", ".jpg", ".jpeg" };
   string[] candidates = null;
   try { candidates = Directory.GetFiles(folder, assetBaseName + ".*", SearchOption.TopDirectoryOnly); } catch { candidates = null; }
   if (candidates != null && candidates.Length > 0)
   {
    var pick = candidates.FirstOrDefault(f => supported.Contains(Path.GetExtension(f), StringComparer.InvariantCultureIgnoreCase));
    if (!string.IsNullOrEmpty(pick) && File.Exists(pick))
    {
     var data = File.ReadAllBytes(pick);
     if (TryLoadImageBytes(data, out var tex2))
     {
      if (DebugTrace) try { Debug.Log("[SkinAssets] Hit " + sourceTag + " file: " + pick + " => tex '" + tex2.name + "' (" + tex2.width + "x" + tex2.height + ")"); } catch {}
      return tex2;
     }
    }

    var dds = candidates.FirstOrDefault(f => ".dds".Equals(Path.GetExtension(f), StringComparison.InvariantCultureIgnoreCase));
    if (!string.IsNullOrEmpty(dds) && !string.IsNullOrEmpty(urlRoot))
    {
     var texDds = TryGetFromUrl(urlRoot, assetBaseName, sourceTag + "-dds");
     if (texDds != null) return texDds;
    }
   }

   // Smart match ignoring separators/case
   try
   {
    var files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
    string desired = RemoveSeparators(assetBaseName).ToLowerInvariant();
    foreach (var f in files)
    {
     var ext = Path.GetExtension(f);
     if (string.IsNullOrEmpty(ext)) continue;
     if (ext.Equals(".png", StringComparison.InvariantCultureIgnoreCase) || ext.Equals(".jpg", StringComparison.InvariantCultureIgnoreCase) || ext.Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase))
     {
      var baseName = Path.GetFileNameWithoutExtension(f);
      if (string.IsNullOrEmpty(baseName)) continue;
      var norm = RemoveSeparators(baseName).ToLowerInvariant();
      if (norm == desired)
      {
       var data = File.ReadAllBytes(f);
       if (TryLoadImageBytes(data, out var tex3))
       {
        if (DebugTrace) try { Debug.Log("[SkinAssets] Hit " + sourceTag + " smart: " + f + " => tex '" + tex3.name + "' (" + tex3.width + "x" + tex3.height + ")"); } catch {}
        return tex3;
       }
      }
     }
     else if (ext.Equals(".dds", StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty(urlRoot))
     {
      var baseName = Path.GetFileNameWithoutExtension(f);
      foreach (var name in EnumerateNameVariants(baseName))
      {
       var texDds = TryGetFromUrl(urlRoot, name, sourceTag + "-dds-smart");
       if (texDds != null) return texDds;
      }
     }
    }
   }
   catch { }

   return null;
  }
  catch { return null; }
 }

 private static string BuildUrlRootFromFolder(string texturesFolder)
 {
  try
  {
   if (string.IsNullOrEmpty(texturesFolder)) return null;
   var norm = texturesFolder.Replace('\\','/');
   var marker = "/GameData/";
   int idx = norm.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
   if (idx < 0) return null;
   var rel = norm.Substring(idx + marker.Length);
   return rel.TrimStart('/');
  }
  catch { return null; }
 }
}
