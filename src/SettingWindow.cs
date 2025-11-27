using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// Settings: wysuwany panel od dołu okna KerbNote, z wąskim, wycentrowanym paskiem toggle
[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
public class SettingWindow : MonoBehaviour
{
 private static SettingWindow _instance;

 // Expose About visibility for other UI (e.g., Alarm_bar) to avoid stealing focus
 public static bool IsAboutVisible => _instance != null && _instance.showAbout;

 private KerbNote host; // okno główne
 private Vector2 screenSize;

 private Rect panelRect = new Rect(0,0,320f,220f);

 private bool isVisible = false;
 private float slideT =0f;
 private float slideSpeed =12f;
 private const float SNAP_EPSILON =0.001f;

 private float hiddenPanelY;
 private float visiblePanelY;

 private int panelWindowID;
 private int buttonWindowID;

 private Texture2D toggleTexHorizontal;
 private Texture2D toggleTexVertical;
 private bool useRotatedToggle = false;

 private const string TEXTURE_TOGGLE_HORIZONTAL = "KerbCalcProject/Textures/Alarm_bar_horizontal";
 private const string TEXTURE_TOGGLE_VERTICAL = "KerbCalcProject/Textures/Alarm_bar";

 // Pasek toggle (poziomy): stała szerokość 350, stała wysokość z tekstury
 private float toggleHeight =22f; // trzymamy stałą wysokość (z tekstury)
 private float toggleWidth =350f; // stała szerokość 350
 private float toggleOffsetX =0f; // opcjonalny offset od środka

 private bool stylesReady;
 private Texture2D tabTex;
 private Texture2D tabHoverTex;
 private Texture2D tabClickTex;
 private GUIStyle titleStyle;
 private GUIStyle labelCenterStyle;
 private GUIStyle toggleLabelStyle;

 private bool showSavePicker = false;
 private Vector2 savesScroll = Vector2.zero;
 private readonly List<string> saveNames = new List<string>();

 private bool showSkinPicker = false;
 private Vector2 skinScroll = Vector2.zero;
 private readonly List<string> skinPackNames = new List<string>();
 private readonly Dictionary<string, string> skinTextureFolderByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
 private const string SKINS_BASE_PATH = @"C:\Program Files\Epic Games\KerbalSpaceProgram\English\GameData\KerbCalcProject\texture_pack";

 private bool isDraggingBar = false;
 private Vector2 dragStartMouse;
 private float dragStartProtrusion;
 private bool barClickCandidate = false;

 // Click-through prevention
 private const string INPUT_LOCK_ID = "KerbNote_ClickBlock";
 private bool inputLockSet = false;
 private Rect lastPanelWinRect;
 private Rect lastBarRect;

 // --- About/Help modal ---
 private bool showAbout = false;
 private Rect aboutRect = new Rect(200, 120, 520, 420);
 private Vector2 aboutScroll = Vector2.zero;
 private string aboutText = string.Empty;
 private int aboutWindowID = -4000; // stały ujemny ID aby być na wierzchu
 private Rect lastAboutRect;
 private Texture2D _darkTex;
 private Texture2D _aboutCloseBtnTex; // Button.png tło dla X
 private Texture2D _aboutCloseBtnHoverTex;
 private Texture2D _aboutCloseBtnActiveTex;

 void Awake()
 {
 if (_instance != null && _instance != this)
 {
 Destroy(this.gameObject);
 return;
 }
 _instance = this;
 DontDestroyOnLoad(this.gameObject);

 screenSize = new Vector2(Screen.width, Screen.height);

 panelWindowID = GetInstanceID() ^0x51AFCC21;
 buttonWindowID = GetInstanceID() ^0x31B1EE4F;
 aboutWindowID = -4000; // wymuszenie stałego ID
 }

 void Start()
 {
 TrySetupStyles();

 // Znajdź okno główne
 if (host == null)
 {
 try { host = FindObjectOfType<KerbNote>(); } catch { host = null; }
 }
 if (host == null)
 {
 Debug.LogWarning("[KerbNote][SettingWindow] KerbNote host not found. Disabling.");
 enabled = false;
 return;
 }

 // Wczytaj tekstury paska toggle z aktywnego skina
 ReloadToggleTextures();

 // Ustaw zakotwiczenie i pozycję początkową
 UpdatePanelSizeAndAnchors();
 UpdateAnchoredPosition();
 }

 private void ReloadToggleTextures()
 {
     try { KerbalUIBackground.LoadTexture(); } catch { }
     // Prefer aktywny skin via SkinAssets
     toggleTexHorizontal = SkinAssets.Get("Alarm_bar_horizontal") ?? GameDatabase.Instance.GetTexture(TEXTURE_TOGGLE_HORIZONTAL, false);
     toggleTexVertical = SkinAssets.Get("Alarm_bar") ?? GameDatabase.Instance.GetTexture(TEXTURE_TOGGLE_VERTICAL, false);
     // Jeśli brak poziomej – użyj pionowej obróconej
     useRotatedToggle = (toggleTexHorizontal == null && toggleTexVertical != null);

     // Wysokość paska poziomego z tekstury, szerokość stała 350
     if (toggleTexHorizontal != null)
     {
         toggleHeight = Mathf.Clamp(toggleTexHorizontal.height, 12f, 64f);
     }
     else if (toggleTexVertical != null && useRotatedToggle)
     {
         // Gdy używamy obróconej pionowej, wysokość pozioma = szerokość pionowej (bez dalszego skalowania przy zmianie okna)
         toggleHeight = Mathf.Clamp(toggleTexVertical.width, 12f, 64f);
     }
     toggleWidth = 350f;
 }

 void Update()
 {
 if (host == null)
 {
 try { host = FindObjectOfType<KerbNote>(); } catch { host = null; }
 if (host == null) return;
 }

 // Auto-hide gdy okno główne niewidocne
 if (!host.IsWindowVisible)
 {
 isVisible = false;
 slideT =0f;
 UpdatePanelSizeAndAnchors();
 UpdateAnchoredPosition();
 // Ensure locks cleared when hidden
 ClearInputLock();
 return;
 }

 // Reakcja na zmianę rozdzielczości
 if (screenSize.x != Screen.width || screenSize.y != Screen.height)
 {
 screenSize = new Vector2(Screen.width, Screen.height);
 // Re-center About window na zmianę rozdzielczości
 if (showAbout)
 {
     CenterAboutWindow();
 }
 }

 UpdatePanelSizeAndAnchors();

 // Jeżeli użytkownik przeciąga pasek – nie nadpisuj slideT interpolacją
 bool animate = !isDraggingBar;
 if (animate)
 {
 float targetT = isVisible ?1f :0f;
 float nextT = Mathf.Lerp(slideT, targetT, Time.deltaTime * slideSpeed);
 if (Mathf.Abs(nextT - targetT) <0.0005f) nextT = targetT;
 slideT = nextT;
 }

 UpdateAnchoredPosition();
 }

 void OnGUI()
 {
 if (host == null || !host.IsWindowVisible)
 {
     ClearInputLock();
     return;
 }

 if (!stylesReady) TrySetupStyles();

 // Zawsze aktualizuj rozmiary (np. zmiana okna), ale jeśli About jest otwarte – rysujemy tylko About
 UpdatePanelSizeAndAnchors();
 UpdateAnchoredPosition();

 if (showAbout)
 {
     // About/Help modal only – bez paska Settings i panelu
     aboutRect = ClampRectToScreen(aboutRect, 10f);
     lastAboutRect = aboutRect;
     GUI.Window(aboutWindowID, aboutRect, DrawAboutWindow, string.Empty, GUIStyle.none);
     GUI.BringWindowToFront(aboutWindowID);

     // Input lock tylko dla About
     UpdateInputLock();
     return; // nie rysuj paska ani panelu
 }

 // --- poniżej normalny rendering paska i panelu, gdy About nie jest otwarte ---

 // Szerokość paska stała 350 i wycentrowana względem okna głównego
 Rect hostRect = host.WindowRect;
 float protrusion = Mathf.Clamp(panelRect.y + panelRect.height - hostRect.yMax, 0f, panelRect.height);

 // Panel (przycięty do wystającej części)
 if (protrusion > SNAP_EPSILON)
 {
 Rect panelWinRect = new Rect(
 panelRect.x,
 hostRect.yMax,
 panelRect.width,
 protrusion
 );
 lastPanelWinRect = panelWinRect; // remember for input lock
 GUI.Window(panelWindowID, panelWinRect, DrawPanelWindow, string.Empty, GUIStyle.none);
 }
 else
 {
 lastPanelWinRect = Rect.zero;
 }

 // Pozycja paska – centrowanie poziome względem okna głównego
 float btnX = hostRect.center.x - (toggleWidth /2f) + toggleOffsetX;
 float btnY = hostRect.yMax + protrusion; // porusza się razem z wysuwanym panelem w dół
 Rect btnAbsRect = new Rect(btnX, btnY, toggleWidth, toggleHeight);
 lastBarRect = btnAbsRect; // remember for input lock
 DrawToggleBarOverlay(btnAbsRect);

 lastAboutRect = Rect.zero;

 // After drawing, manage KSP input locks for hover over our UI
 UpdateInputLock();
 }

 private void DrawPanelWindow(int id)
 {
 Rect hostRect = host.WindowRect;
 float protrusion = Mathf.Clamp(panelRect.y + panelRect.height - hostRect.yMax,0f, panelRect.height);

 // Przesunięcie lokalne, aby dolna część była widoczna
 float localY = -(panelRect.height - protrusion);

 DrawPanelContents(localY);
 }

 private void DrawToggleBarOverlay(Rect absRect)
 {
 // Rysuj Alarm_bar obrócony o90° (poziomy szeroki pasek) w przestrzeni ekranu – brak clipowania przez Window
 if (toggleTexVertical != null && useRotatedToggle)
 {
 Vector2 pivot = new Vector2(absRect.x + absRect.width /2f, absRect.y + absRect.height /2f);
 // Prostokąt pre-rotacji (zamiana wymiarów) wycentrowany względem prostokąta docelowego
 Rect preRotRect = new Rect(
 absRect.x + (absRect.width - absRect.height) /2f,
 absRect.y + (absRect.height - absRect.width) /2f,
 absRect.height,
 absRect.width
 );
 Matrix4x4 old = GUI.matrix;
 GUIUtility.RotateAroundPivot(90f, pivot);
 GUI.DrawTexture(preRotRect, toggleTexVertical, ScaleMode.StretchToFill);
 GUI.matrix = old;
 }
 else if (toggleTexHorizontal != null)
 {
 // Fallback: gotowa pozioma tekstura – wysokość stała (z tekstury), szerokość rozciąga się
 GUI.DrawTexture(absRect, toggleTexHorizontal, ScaleMode.StretchToFill);
 }
 else
 {
 // Awaryjnie – prosty box
 GUI.Box(absRect, string.Empty, GUI.skin.button);
 }

 // Napis na paskie – czytelny i wycentrowany, podniesiony o3 px
 if (toggleLabelStyle == null)
 {
 toggleLabelStyle = new GUIStyle(GUI.skin.label)
 {
 alignment = TextAnchor.MiddleCenter,
 fontStyle = FontStyle.Bold
 };
 toggleLabelStyle.normal.textColor = new Color(0.95f,0.92f,0.8f,0.98f);
 }
 var labelRect = new Rect(absRect.x, absRect.y -3f, absRect.width, absRect.height);
 GUI.Label(labelRect, "Settings", toggleLabelStyle);

 // Przycisk niewidzialny – pojedynczy toggle na MouseUp
 if (GUI.Button(absRect, GUIContent.none, GUIStyle.none))
 {
 isVisible = !isVisible;
 }

 // Consume mouse events over the bar to prevent click-through below
 var e = Event.current;
 if (e != null && absRect.Contains(e.mousePosition))
 {
 if (e.isMouse || e.type == EventType.ScrollWheel)
 {
 e.Use();
 }
 }
 }

 private void DrawPanelContents(float localY)
 {
 // Tło panelu (styl jak w innych oknach)
 KerbalUIBackground.DrawNoteWindow(new Rect(0, localY, panelRect.width, panelRect.height));

 float margin =10f;
 // Mniejszy górny padding na stronie głównej, aby zlikwidować zbędną przestrzeń
 float y = localY +4f;
 float fullW = panelRect.width - margin *2f;

 // Widok główny: tytuł/nagłówki
 if (!showSavePicker && !showSkinPicker)
 {
 if (titleStyle != null)
 {
 // Niższy tytuł, mniej miejsca na górze
 GUI.Label(new Rect(margin, y, fullW,18f), "Settings", titleStyle);
 }
 // Zmniejszony odstęp pod tytułem, aby przyciski zaczynały się wyżej
 y +=20f;
 }

 float btnH =26f;
 float spacing =8f;

 if (!showSavePicker && !showSkinPicker)
 {
 Rect notesRect = new Rect(margin, y, fullW, btnH); y += btnH + spacing;
 Rect skinRect = new Rect(margin, y, fullW, btnH); y += btnH + spacing;
 Rect aboutRectBtn = new Rect(margin, y, fullW, btnH); y += btnH + spacing;

 if (DrawTexButton(notesRect, "Notes", tabTex, tabHoverTex, tabClickTex))
 {
 showSavePicker = true;
 showSkinPicker = false;
 BuildGameSavesList();
 }
 if (DrawTexButton(skinRect, "Skin", tabTex, tabHoverTex, tabClickTex))
 {
 showSkinPicker = true;
 showSavePicker = false;
 BuildSkinPackList();
 }
 if (DrawTexButton(aboutRectBtn, "About/Help", tabTex, tabHoverTex, tabClickTex))
 {
 OpenAboutModal();
 }
 }
 else if (showSavePicker)
 {
 // Nagłówek „Select save”5 px od górnej krawędzi panelu
 GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
 {
 fontStyle = FontStyle.Bold
 };
 labelStyle.normal.textColor = new Color(0.95f,0.9f,0.75f,0.95f);

 y = localY +5f;
 GUI.Label(new Rect(margin, y, fullW,18f), "Select save:", labelStyle);
 y +=20f;

 // Policz liczbę wierszy i listHeight (max5 widocznych), reszta w scrollu
 float rowH =24f;
 float rowSpacing =4f;
 int rows = saveNames != null ? saveNames.Count :0;
 int rowsVisible = Mathf.Clamp(rows,0,5);
 float fiveOrSevenHeight = rowsVisible * rowH + Mathf.Max(0, rowsVisible -1) * rowSpacing;
 float backH =22f;
 float bottomGap =6f; // mały odstęp od dołu
 float smallGap =6f; // odstęp między listą a Back
 float backY = localY + panelRect.height - backH - bottomGap;
 float available = Mathf.Max(0f, backY - smallGap - y);
 float listHeight = Mathf.Clamp(fiveOrSevenHeight,40f, available);

 // Scroll: viewRect wyższy niż listHeight, jeżeli elementów > widoczych
 float contentHeight = rows * (rowH + rowSpacing);
 Rect viewRect = new Rect(0,0, fullW -16f, Math.Max(contentHeight, listHeight));
 savesScroll = GUI.BeginScrollView(new Rect(margin, y, fullW, listHeight), savesScroll, viewRect);
 float itemY =0f;
 for (int i =0; i < rows; i++)
 {
 string sname = saveNames[i];
 Rect r = new Rect(0f, itemY, viewRect.width, rowH);
 if (DrawTexButton(r, sname, tabTex, tabHoverTex, tabClickTex))
 {
 ApplySaveSelection(sname);
 isVisible = false; // zamknij po wyborze
 }
 itemY += (rowH + rowSpacing);
 }
 GUI.EndScrollView();

 // Back tuż nad dolną krawędzią, po małym odstępie
 Rect backRect = new Rect(margin, backY, fullW, backH);
 if (DrawTexButton(backRect, "Back", tabTex, tabHoverTex, tabClickTex))
 {
 showSavePicker = false;
 }
 }
 else if (showSkinPicker)
 {
 // Nagłówek „Select skin”5 px od górnej krawędzi panelu
 GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
 {
 fontStyle = FontStyle.Bold
 };
 labelStyle.normal.textColor = new Color(0.95f,0.9f,0.75f,0.95f);

 y = localY +5f;
 GUI.Label(new Rect(margin, y, fullW,18f), "Select skin:", labelStyle);
 y +=20f;

 float rowH =24f;
 float rowSpacing =4f;
 int rows = skinPackNames != null ? skinPackNames.Count :0;
 int rowsVisible = Mathf.Clamp(rows,0,5);
 float suggestedHeight = rowsVisible * rowH + Mathf.Max(rowsVisible -1,0) * rowSpacing;
 float backH =22f;
 float bottomGap =6f;
 float smallGap =6f;
 float backY = localY + panelRect.height - backH - bottomGap;
 float available = Mathf.Max(0f, backY - smallGap - y);
 float listHeight = Mathf.Clamp(suggestedHeight,40f, available);

 float contentHeight = rows * (rowH + rowSpacing);
 Rect viewRect = new Rect(0,0, fullW -16f, Math.Max(contentHeight, listHeight));
 skinScroll = GUI.BeginScrollView(new Rect(margin, y, fullW, listHeight), skinScroll, viewRect);
 float itemY =0f;
 for (int i =0; i < rows; i++)
 {
 string pack = skinPackNames[i];
 Rect r = new Rect(0f, itemY, viewRect.width, rowH);
 if (DrawTexButton(r, pack, tabTex, tabHoverTex, tabClickTex))
 {
 ApplySkinSelection(pack);
 // nie zamykamy panelu po wyborze skina – pozwól wybrać inny
 }
 itemY += (rowH + rowSpacing);
 }
 GUI.EndScrollView();

 Rect backRect = new Rect(margin, backY, fullW, backH);
 if (DrawTexButton(backRect, "Back", tabTex, tabHoverTex, tabClickTex))
 {
 showSkinPicker = false;
 }
 }
 }

 private bool DrawTexButton(Rect rect, string label, Texture2D normal, Texture2D hover, Texture2D active)
 {
 Event e = Event.current;
 bool isHover = rect.Contains(e.mousePosition);
 bool isActive = isHover && (e.type == EventType.MouseDown || (e.type == EventType.MouseDrag && GUIUtility.hotControl !=0));

 Texture2D bg = normal;
 if (isActive && active != null) bg = active; else if (isHover && hover != null) bg = hover;
 if (bg != null) GUI.DrawTexture(rect, bg, ScaleMode.StretchToFill);

 if (labelCenterStyle == null)
 {
 labelCenterStyle = new GUIStyle(GUI.skin.label)
 {
 alignment = TextAnchor.MiddleCenter,
 fontStyle = FontStyle.Bold
 };
 labelCenterStyle.normal.textColor = new Color(0.9f,0.85f,0.7f,0.95f);
 }
 GUI.Label(rect, label, labelCenterStyle);

 return GUI.Button(rect, GUIContent.none, GUIStyle.none);
 }

 private void ApplySaveSelection(string save)
 {
 try
 {
 if (host == null) host = FindObjectOfType<KerbNote>();
 if (host == null) return;

 // Ensure header exists for the picked save before switching
 try
 {
     var path = KerbNote.ComputeNotesPathForSave(save);
     if (!string.IsNullOrEmpty(path))
     {
         // Create file with default header if missing
         if (!File.Exists(path))
         {
             File.WriteAllText(path, "Skin: Green" + Environment.NewLine + Environment.NewLine);
         }
     }
 }
 catch {}

 // Wywołaj przełączenie na nowy sejw (to wczyta nowe notatki i skina)
 host.SwitchToSave(save);

 // Wymuś aktywację pierwszej zakładki i odświeżenie okna
 if (host.TabCount >0)
 {
 host.OpenOnTab(0); // To otworzy okno i ustawi pierwszą zakładkę jako aktywną
 }
 
 // Zainicjuj alarmy dla nowego sejwa
 AlarmManager.Init();
 AlarmRunner.ForceReevaluateNow();

 // Po wyborze sejwa – przy kolejnym otwarciu pokaż widok główny Settings
 showSavePicker = false;
 savesScroll = Vector2.zero;
 }
 catch (Exception ex)
 {
 Debug.LogError("[KerbNote][SettingWindow] ApplySaveSelection failed: " + ex.Message);
 }
 }

 private void ApplySkinSelection(string skinName)
 {
     try
     {
         string texturesFolder;
         if (!skinTextureFolderByName.TryGetValue(skinName, out texturesFolder))
         {
             Debug.LogError("[KerbNote][SettingWindow] Skin not found in map: " + skinName);
             return;
         }
 
         // Apply via host with explicit folder to avoid any case/path mismatch
         if (host != null)
         {
             host.ApplySkinFromFolder(skinName, texturesFolder, persistHeader: true);
         }
 
         // Odśwież własne tekstury i style, aby przyciski w Settings użyły nowego skina
         stylesReady = false;
         TrySetupStyles();
         ReloadToggleTextures();
 
         // Provide TabRed and refresh AlarmSelector
         foreach (var sel in GameObject.FindObjectsOfType<AlarmSelector>())
         {
             sel.RefreshTextures();
         }
 
         Debug.Log("[KerbNote][SettingWindow] Skin applied: " + texturesFolder);
     }
     catch (Exception ex)
     {
         Debug.LogError("[KerbNote][SettingWindow] ApplySkinSelection failed: " + ex.Message);
     }
 }

 private void TrySetupStyles()
 {
 if (stylesReady) return;
 try
 {
 tabTex = SkinAssets.Get("Tab") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB, false);
 tabHoverTex = SkinAssets.Get("TabHover") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB_HOVER, false);
 tabClickTex = SkinAssets.Get("TabClick") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB_CLICK, false);

 titleStyle = new GUIStyle(GUI.skin.label)
 {
 alignment = TextAnchor.MiddleLeft,
 fontStyle = FontStyle.Bold,
 fontSize =14
 };
 titleStyle.normal.textColor = new Color(0.9f,0.8f,0.6f,0.95f);
 stylesReady = true;
 }
 catch { stylesReady = true; }
 }

 private void UpdatePanelSizeAndAnchors()
 {
 if (host == null) return;
 Rect hostRect = host.WindowRect;
 panelRect.width = Mathf.Max(200f, hostRect.width -60f);
 panelRect.x = hostRect.x +30f;
 panelRect.height = ComputeDesiredPanelHeightForCurrentView();
 hiddenPanelY = hostRect.yMax - panelRect.height;
 visiblePanelY = hostRect.yMax;
 }

 private float ComputeDesiredPanelHeightForCurrentView()
 {
 const float topMargin =6f;
 const float headerTopPad =5f;
 const float headerBlock =20f;
 const float rowH =24f;
 const float rowSpacing =4f;
 const float smallGap =6f;
 const float backH =22f;
 const float bottomGap =6f;
 const float minPanel =120f;

 if (showSavePicker)
 {
 int total = saveNames != null ? saveNames.Count :0;
 int rowsVisible = Mathf.Clamp(total,0,5);
 float listHeight = rowsVisible >0 ? rowsVisible * rowH + Mathf.Max(rowsVisible -1,0) * rowSpacing :0f;
 float height = topMargin + headerTopPad + headerBlock + listHeight + smallGap + backH + bottomGap;
 return Mathf.Max(minPanel, height);
 }
 else if (showSkinPicker)
 {
 int total = skinPackNames != null ? skinPackNames.Count :0;
 int rowsVisible = Mathf.Clamp(total,0,5);
 float listHeight = rowsVisible >0 ? rowsVisible * rowH + Mathf.Max(rowsVisible -1,0) * rowSpacing :0f;
 float height = topMargin + headerTopPad + headerBlock + listHeight + smallGap + backH + bottomGap;
 return Mathf.Max(minPanel, height);
 }
 else
 {
 // Strona główna: mniejszy top i mniejszy blok tytułu, aby zredukować pustą przestrzeń u góry
 float topMarginMain = 4f;      // było 6f
 float titleBlock = 20f;        // 18f wysokości tytułu + 2f odstępu pod nim
 int totalButtons =3;
 float buttonsBlock = totalButtons * rowH + (totalButtons -1) * rowSpacing;
 float height = topMarginMain + titleBlock + buttonsBlock + bottomGap;
 return Mathf.Max(minPanel, height);
 }
 }

 private void UpdateAnchoredPosition()
 {
 if (host == null) return;
 panelRect.y = Mathf.Lerp(hiddenPanelY, visiblePanelY, slideT);
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
     // Refresh own styles and toggle textures when skin changes from any source (notes header or settings)
     stylesReady = false;
     TrySetupStyles();
     ReloadToggleTextures();
 }
 
 // Build list of saves (used by Notes button)
 private void BuildGameSavesList()
 {
     saveNames.Clear();
     try
     {
         string savesDir = Path.Combine(KSPUtil.ApplicationRootPath, "saves");
         if (!Directory.Exists(savesDir)) return;
         var dirs = Directory.GetDirectories(savesDir);
         foreach (var d in dirs)
         {
             var name = Path.GetFileName(d);
             if (string.IsNullOrEmpty(name)) continue;
             if (string.Equals(name, "Training", StringComparison.OrdinalIgnoreCase)) continue;
             if (string.Equals(name, "Scenarios", StringComparison.OrdinalIgnoreCase)) continue;
             saveNames.Add(name);
 
             // Ensure Notes_<save>.txt exists with header
             try
             {
                 string path = KerbNote.ComputeNotesPathForSave(name);
                 if (!string.IsNullOrEmpty(path))
                 {
                     string dir = Path.GetDirectoryName(path);
                     if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                     if (!File.Exists(path))
                     {
                         File.WriteAllText(path, "Skin: Green" + Environment.NewLine + Environment.NewLine);
                     }
                     else
                     {
                         var lines = File.ReadAllLines(path).ToList();
                         bool hasHeader = false;
                         for (int i = 0; i < Math.Min(lines.Count, 5); i++)
                         {
                             var l = lines[i];
                             if (string.IsNullOrEmpty(l)) continue;
                             if (l.StartsWith("Skin:", StringComparison.InvariantCultureIgnoreCase)) { hasHeader = true; break; }
                             if (l.StartsWith("=== ")) break;
                         }
                         if (!hasHeader)
                         {
                             lines.Insert(0, "Skin: Green");
                             lines.Insert(1, string.Empty);
                             File.WriteAllLines(path, lines);
                         }
                     }
                 }
             }
             catch (Exception ex)
             {
                 Debug.LogWarning("[KerbNote][SettingWindow] Ensure header for save '" + name + "' failed: " + ex.Message);
             }
         }
         saveNames.Sort(StringComparer.InvariantCultureIgnoreCase);
         try { MoveOrphanedAlarmsAndNotesToDelatedSaves(saveNames); }
         catch (Exception ex) { Debug.LogError("[KerbNote][SettingWindow] Cleanup orphaned files failed: " + ex.Message); }
     }
     catch (Exception ex)
     {
         Debug.LogError("[KerbNote][SettingWindow] BuildGameSavesList error: " + ex.Message);
     }
 }
 
 // Build list of skin packs (used by Skin button)
 private void BuildSkinPackList()
 {
     skinPackNames.Clear();
     skinTextureFolderByName.Clear();
     try
     {
         string basePath = Directory.Exists(SKINS_BASE_PATH)
             ? SKINS_BASE_PATH
             : Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbCalcProject", "texture_pack");
         if (!Directory.Exists(basePath))
         {
             Debug.LogWarning("[KerbNote][SettingWindow] Skins base path not found: " + basePath);
             return;
         }
         var dirs = Directory.GetDirectories(basePath);
         foreach (var d in dirs)
         {
             var packName = Path.GetFileName(d);
             if (string.IsNullOrEmpty(packName)) continue;
             string texturesDir = Path.Combine(d, "Textures");
             if (!Directory.Exists(texturesDir)) continue;
             skinPackNames.Add(packName);
             skinTextureFolderByName[packName] = texturesDir;
         }
         skinPackNames.Sort(StringComparer.InvariantCultureIgnoreCase);
     }
     catch (Exception ex)
     {
         Debug.LogError("[KerbNote][SettingWindow] BuildSkinPackList error: " + ex.Message);
     }
 }
 
 // Move orphaned alarms/notes files (no matching save folder) into DelatedSaves
 private void MoveOrphanedAlarmsAndNotesToDelatedSaves(List<string> validSaves)
 {
     if (validSaves == null) return;
     try
     {
         string modDir = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbCalcProject", "AlarmsAndNotes");
         if (!Directory.Exists(modDir)) return;
         string targetDir = Path.Combine(modDir, "DelatedSaves");
         if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
         var files = Directory.GetFiles(modDir, "*.txt");
         var validSet = new HashSet<string>(validSaves, StringComparer.InvariantCultureIgnoreCase);
         foreach (var file in files)
         {
             string fname = Path.GetFileName(file);
             if (string.IsNullOrEmpty(fname)) continue;
             string nameNoExt = Path.GetFileNameWithoutExtension(fname);
             int us = nameNoExt.IndexOf('_');
             if (us <= 0) continue;
             string prefix = nameNoExt.Substring(0, us);
             string candidate = nameNoExt.Substring(us + 1);
             if (string.IsNullOrEmpty(candidate)) continue;
             if (!prefix.Equals("Notes", StringComparison.OrdinalIgnoreCase) && !prefix.Equals("Alarms", StringComparison.OrdinalIgnoreCase)) continue;
             if (!validSet.Contains(candidate))
             {
                 string dest = Path.Combine(targetDir, fname);
                 if (File.Exists(dest))
                 {
                     string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                     dest = Path.Combine(targetDir, nameNoExt + "_" + stamp + Path.GetExtension(fname));
                 }
                 try
                 {
                     File.Move(file, dest);
                     Debug.Log("[KerbNote][SettingWindow] Moved orphan file '" + fname + "' to DelatedSaves (no save folder for '" + candidate + "').");
                 }
                 catch (Exception mvEx)
                 {
                     Debug.LogError("[KerbNote][SettingWindow] Move failed for '" + fname + "': " + mvEx.Message);
                 }
             }
         }
     }
     catch (Exception ex)
     {
         Debug.LogError("[KerbNote][SettingWindow] Orphan cleanup error: " + ex.Message);
     }
 }

 // --- About/Help helpers ---
 private void OpenAboutModal()
 {
     try
     {
         // ensure bg textures are prepared
         try { KerbalUIBackground.LoadTexture(); } catch { }
         string root = KSPUtil.ApplicationRootPath;
         // Bazowe katalogi do sprawdzenia
         var baseDirs = new List<string>
         {
             @"C:\\Program Files\\Epic Games\\KerbalSpaceProgram\\English\\GameData\\KerbCalcProject",
             Path.Combine(root, "English", "GameData", "KerbCalcProject"),
             Path.Combine(root, "GameData", "KerbCalcProject")
         };
         string fileNameBase = "About_Help"; // bez rozszerzenia
         string[] exts = { "", ".txt", ".md", ".markdown" };
         string text = null;
         foreach (var dir in baseDirs)
         {
             if (string.IsNullOrEmpty(dir)) continue;
             foreach (var ext in exts)
             {
                 string candidate = Path.Combine(dir, fileNameBase + ext);
                 try
                 {
                     if (File.Exists(candidate))
                     {
                         text = File.ReadAllText(candidate);
                         if (!string.IsNullOrEmpty(text)) break;
                     }
                 }
                 catch { }
             }
             if (!string.IsNullOrEmpty(text)) break;
         }
         if (string.IsNullOrEmpty(text))
         {
             text = "# KerbCalc / KerbNote\nPlik 'About_Help' nie znaleziony. Umieść go w: GameData/KerbCalcProject lub English/GameData/KerbCalcProject (warianty: About_Help, About_Help.txt, About_Help.md).";
         }
         aboutText = ConvertMarkdownToUnityRichText(text);
         CenterAboutWindow();
         showAbout = true;
     }
     catch (Exception ex)
     {
         aboutText = "**Error**: " + ex.Message;
         CenterAboutWindow();
         showAbout = true;
     }
 }

 private void CenterAboutWindow()
 {
     float w = Mathf.Clamp(Screen.width * 0.4f, 420f, Mathf.Max(420f, Screen.width - 40f));
     float h = Mathf.Clamp(Screen.height * 0.5f, 320f, Mathf.Max(320f, Screen.height - 60f));
     aboutRect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
 }

 private Rect ClampRectToScreen(Rect r, float pad)
 {
     if (r.width > Screen.width - pad*2) r.width = Screen.width - pad*2;
     if (r.height > Screen.height - pad*2) r.height = Screen.height - pad*2;
     if (r.x < pad) r.x = pad;
     if (r.y < pad) r.y = pad;
     if (r.xMax > Screen.width - pad) r.x = Screen.width - pad - r.width;
     if (r.yMax > Screen.height - pad) r.y = Screen.height - pad - r.height;
     return r;
 }

 private void DrawAboutWindow(int id)
 {
     // Tło teksturowane + ciemny welon
     try { KerbalUIBackground.LoadTexture(); } catch { }
     KerbalUIBackground.Draw(new Rect(0, 0, aboutRect.width, aboutRect.height));

     if (_darkTex == null)
     {
         _darkTex = new Texture2D(1,1);
         _darkTex.SetPixel(0,0, new Color(0f,0f,0f,0.5f)); // półprzezroczysty, by było widać fakturę
         _darkTex.Apply();
     }
     GUI.DrawTexture(new Rect(0,0, aboutRect.width, aboutRect.height), _darkTex);

     float pad = 12f;
     float titleH = 30f; // większy tytuł
     Rect titleRect = new Rect(pad, pad, aboutRect.width - pad*2 - 34f, titleH);
     GUIStyle title = new GUIStyle(GUI.skin.label)
     {
         alignment = TextAnchor.MiddleLeft,
         fontStyle = FontStyle.Bold,
         fontSize = 18
     };
     title.normal.textColor = new Color(0.95f, 0.9f, 0.78f, 0.98f);
     GUI.Label(titleRect, "About / Help", title);

     // Zamknij – tło Button.png (+ hover/active jeśli istnieją)
     if (_aboutCloseBtnTex == null)
     {
         _aboutCloseBtnTex = SkinAssets.Get("Button") ?? GameDatabase.Instance.GetTexture("KerbCalcProject/Textures/Button", false);
         _aboutCloseBtnHoverTex = SkinAssets.Get("ButtonHover") ?? GameDatabase.Instance.GetTexture("KerbCalcProject/Textures/ButtonHover", false);
         _aboutCloseBtnActiveTex = SkinAssets.Get("ButtonClick") ?? GameDatabase.Instance.GetTexture("KerbCalcProject/Textures/ButtonClick", false);
     }
     Rect closeRect = new Rect(aboutRect.width - pad - 28f, pad + 1f, 28f, 28f);
     GUIStyle closeStyle = new GUIStyle(GUI.skin.button)
     {
         alignment = TextAnchor.MiddleCenter,
         fontStyle = FontStyle.Bold,
         fontSize = 14
     };
     if (_aboutCloseBtnTex != null)
     {
         closeStyle.normal.background = _aboutCloseBtnTex;
         closeStyle.hover.background = _aboutCloseBtnHoverTex ?? _aboutCloseBtnTex;
         closeStyle.active.background = _aboutCloseBtnActiveTex ?? _aboutCloseBtnTex;
         closeStyle.border = new RectOffset(0,0,0,0);
         closeStyle.padding = new RectOffset(0,0,0,0);
         closeStyle.normal.textColor = Color.white;
     }
     if (GUI.Button(closeRect, "X", closeStyle))
     {
         showAbout = false;
         return;
     }

     // Treść
     Rect contentRect = new Rect(pad, pad + titleH + 6f, aboutRect.width - pad*2, aboutRect.height - (pad + titleH + 6f) - pad);
     var contentStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true };
     contentStyle.normal.textColor = new Color(0.88f, 0.96f, 0.88f, 0.98f);
     contentStyle.fontSize = 16; // większa czcionka

     float textHeight = contentStyle.CalcHeight(new GUIContent(aboutText), contentRect.width - 16f);
     Rect viewRect = new Rect(0,0, contentRect.width - 16f, Mathf.Max(textHeight + 4f, contentRect.height));
     aboutScroll = GUI.BeginScrollView(contentRect, aboutScroll, viewRect);
     GUI.Label(new Rect(0,0, viewRect.width, textHeight), aboutText, contentStyle);
     GUI.EndScrollView();

     // Brak dragowania – nie wywołujemy GUI.DragWindow
 }
 
 // --- Markdown helpers ---
 private static string ConvertMarkdownToUnityRichText(string md)
 {
     if (string.IsNullOrEmpty(md)) return string.Empty;
     md = md.Replace("\r\n", "\n").Replace("\r", "\n");
     var lines = md.Split(new[] { "\n" }, StringSplitOptions.None);
     var sb = new StringBuilder(md.Length + 128);
     bool inCode = false;
     for (int i = 0; i < lines.Length; i++)
     {
         string line = lines[i];
         string trim = line.TrimStart();
         if (trim.StartsWith("```") ) { inCode = !inCode; continue; }
         if (inCode)
         {
             sb.Append("<color=#C0FFC0>").Append(EscapeRichText(line)).Append("</color>\n");
             continue;
         }
         var m = Regex.Match(trim, "^(#{1,6})\\s*(.+)$");
         if (m.Success)
         {
             int level = m.Groups[1].Value.Length;
             string text = m.Groups[2].Value.Trim();
             int size = 18 - (level - 1) * 2;
             size = Mathf.Clamp(size,10,22);
             sb.Append("<size=").Append(size).Append("><b>").Append(EscapeRichText(text)).Append("</b></size>\n");
             continue;
         }
         if (trim.StartsWith("- ") || trim.StartsWith("* "))
         {
             int leading = line.Length - trim.Length;
             string rest = line.Substring(leading + 2);
             sb.Append(new string(' ', Mathf.Clamp(leading,0,12))).Append("• ").Append(rest).Append("\n");
             continue;
         }
         string t = line;
         t = Regex.Replace(t, @"\[(.+?)\]\((.+?)\)", m2 => m2.Groups[1].Value + " (" + m2.Groups[2].Value + ")");
         t = Regex.Replace(t, @"`([^`]+)`", m2 => "<color=#C0FFC0>" + EscapeRichText(m2.Groups[1].Value) + "</color>");
         t = Regex.Replace(t, @"\*\*(.+?)\*\*", "<b>$1</b>");
         t = Regex.Replace(t, @"_(.+?)_", "<i>$1</i>");
         t = Regex.Replace(t, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "<i>$1</i>");
         sb.Append(t).Append("\n");
     }
     return sb.ToString();
 }
 private static string EscapeRichText(string s) { return string.IsNullOrEmpty(s)?string.Empty: s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;"); }
 
 // --- Input lock helpers ---
 private void UpdateInputLock()
 {
     try
     {
         var e = Event.current;
         Vector2 mouse = (e != null) ? e.mousePosition : Vector2.zero;
         bool hover = false;
         if (lastBarRect.width > 0 && lastBarRect.height > 0 && lastBarRect.Contains(mouse)) hover = true;
         if (!hover && lastPanelWinRect.width > 0 && lastPanelWinRect.height > 0 && lastPanelWinRect.Contains(mouse)) hover = true;
         if (!hover && showAbout && lastAboutRect.width > 0 && lastAboutRect.height > 0 && lastAboutRect.Contains(mouse)) hover = true;
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
         try { InputLockManager.RemoveControlLock(INPUT_LOCK_ID); } catch { }
         inputLockSet = false;
     }
 }
}
