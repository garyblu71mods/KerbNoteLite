using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Celestial body selector panel: header with tab name, scrollable, clickable entries styled as tabs
public class AlarmSelector : MonoBehaviour
{
	// Window/panel
	private Rect windowRect = new Rect(420,280,300,300);
	private int windowID;
	private bool showWindow;

	// Host and tab context
	private KerbNote host;
	public string TabGuid { get; private set; } // zamiast TabIndex uzywamy GUID

	// Allow embedding container to close the panel
	public Action OnRequestClosePanel { get; set; }

	// Pages
	private enum Page { Bodies, Situations, Actions, Summary }
	private Page page = Page.Bodies;
	private CelestialBody selectedBody;
	private Vessel.Situations selectedSituation;
	private bool selectedEditorMode; // special selection for VAB/SPH
	private bool selectedSpaceCenterMode; // special selection for SpaceCenter
	private const string EDITOR_SPECIAL = "VAB/SPH";
	private const string SPACE_CENTER_SPECIAL = "SpaceCenter";

	// Celestial bodies
	private List<CelestialBody> bodies;
	private Vector2 scroll;

	// Tab textures for list items
	private Texture2D tabTex;
	private Texture2D tabHoverTex;
	private Texture2D tabClickTex;
	private Texture2D tabRedTex; // for Delete alarm

	// Styles
	private bool stylesInitialized;
	private GUIStyle titleStyle;
	private GUIStyle itemStyle;
	private GUIStyle smallBtnStyle;
	private GUIStyle redBtnStyle;
	private GUIStyle captionStyle;

	// Optional selection callbacks
	public Action<CelestialBody> OnSelected; // body picked (page1)
	public Action<CelestialBody, Vessel.Situations> OnSituationSelected; // body + situation (page2)

	public bool IsVisible => showWindow;

	// Input lock for standalone window to prevent click-through
	private const string INPUT_LOCK_ID = "KerbNote_AlarmSelector_ClickBlock";
	private bool inputLockSet = false;

	public void Init(KerbNote hostWindow, int tabIndex, Action<CelestialBody> onSelected = null)
	{
		host = hostWindow;
		TabGuid = host?.GetTabGuid(tabIndex); // pobierz GUID zamiast indeksu
		OnSelected = onSelected;
		EnsureTextures();
		EnsureBodies();
		LoadExistingAlarmIfAny();
	}

	private void Awake()
	{
		windowID = GetInstanceID();
	}

	private void OnEnable()
	{
		// Subskrybuj zmiany skinu, aby odswiezyc tekstury/styl panelu alarmów po przelaczeniu notesu/sejwa
		KerbNote.SkinChanged += OnSkinChanged;
	}

	private void OnDisable()
	{
		KerbNote.SkinChanged -= OnSkinChanged;
		ClearInputLock();
	}

	private void OnSkinChanged(string skin)
	{
		// Wymus ponowne pobranie tekstur oraz przebudowe styli
		RefreshTextures();
	}

	public void Show()
	{
		if (host != null)
		{
			TabGuid = host.ActiveTabGuid; // follow active tab GUID
			float w = windowRect.width;
			float h = windowRect.height;
			float x = host.WindowRect.xMax +16f;
			float y = host.WindowRect.y +40f;
			x = Mathf.Min(x, Screen.width - w -8f);
			y = Mathf.Clamp(y,8f, Screen.height - h -8f);
			windowRect.position = new Vector2(x, y);
		}
		LoadExistingAlarmIfAny();
		showWindow = true;
	}

	public void Hide() { showWindow = false; ClearInputLock(); }
	public void Toggle() { if (!showWindow) Show(); else Hide(); }

	private void EnsureTextures()
	{
		// pull from router first to honor selected skin
		if (tabTex == null) tabTex = SkinAssets.Get("Tab") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB, false);
		if (tabHoverTex == null) tabHoverTex = SkinAssets.Get("TabHover") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB_HOVER, false);
		if (tabClickTex == null) tabClickTex = SkinAssets.Get("TabClick") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB_CLICK, false);
		if (tabRedTex == null)
		{
			tabRedTex = SkinAssets.Get("TabRed");
			if (tabRedTex == null) tabRedTex = GameDatabase.Instance.GetTexture("KerbNoteLite/Textures/TabRed", false);
		}
	}

	public void RefreshTextures()
	{
		// force reload from SkinAssets and rebuild styles
		tabTex = SkinAssets.Get("Tab") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB, false);
		tabHoverTex = SkinAssets.Get("TabHover") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB_HOVER, false);
		tabClickTex = SkinAssets.Get("TabClick") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_TAB_CLICK, false);
		tabRedTex = SkinAssets.Get("TabRed") ?? GameDatabase.Instance.GetTexture("KerbNoteLite/Textures/TabRed", false);
		stylesInitialized = false;
	}

	private void EnsureBodies()
	{
		if (bodies == null)
		{
			bodies = FlightGlobals.Bodies != null ? new List<CelestialBody>(FlightGlobals.Bodies) : new List<CelestialBody>();
		}
	}

	private void InitStyles()
	{
		if (stylesInitialized) return;
		stylesInitialized = true;

		titleStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize =13,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleLeft,
			wordWrap = true
		};
		titleStyle.normal.textColor = new Color(0.9f,0.85f,0.4f,1f);

		captionStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize =12,
			fontStyle = FontStyle.Normal,
			alignment = TextAnchor.MiddleLeft,
			wordWrap = true
		};
		captionStyle.normal.textColor = new Color(0.85f,0.85f,0.85f,1f);

		itemStyle = new GUIStyle(GUI.skin.button)
		{
			alignment = TextAnchor.MiddleCenter,
			fontSize =12,
			wordWrap = false,
			fixedHeight =24f,
			padding = new RectOffset(12,12,2,2),
			margin = new RectOffset(6,6,4,4)
		};
		itemStyle.normal.background = tabTex;
		itemStyle.hover.background = tabHoverTex ?? tabTex;
		itemStyle.active.background = tabClickTex ?? tabTex;
		itemStyle.normal.textColor = new Color(0.95f,0.95f,0.95f,0.95f);
		itemStyle.hover.textColor = Color.white;
		itemStyle.active.textColor = Color.white;

		redBtnStyle = new GUIStyle(GUI.skin.button)
		{
			fontSize =12,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleCenter,
			padding = new RectOffset(8,8,2,2)
		};
		// Uzyj TabRed jako tla przycisku Delete alarm, jezeli dostepne
		if (tabRedTex != null)
		{
			redBtnStyle.normal.background = tabRedTex;
			redBtnStyle.hover.background = tabRedTex;
			redBtnStyle.active.background = tabRedTex;
			redBtnStyle.normal.textColor = Color.white;
			redBtnStyle.hover.textColor = Color.white;
			redBtnStyle.active.textColor = Color.white;
		}
		else
		{
			// fallback: czerwony tekst bez tla
			redBtnStyle.normal.textColor = new Color(1f,0.4f,0.4f,1f);
			redBtnStyle.hover.textColor = new Color(1f,0.6f,0.6f,1f);
		}
	}

	private const float TOP_BAR_HEIGHT =20f;
	private const float PADDING =8f;

	private void OnGUI()
	{
		if (!showWindow) { ClearInputLock(); return; }
		// Hide selector while About/Help modal visible
		if (SettingWindow.IsAboutVisible) { ClearInputLock(); return; }
		if (host == null)
		{
			host = FindObjectOfType<KerbNote>();
			if (host == null) { Hide(); return; }
		}

		// Follow host active tab if selector is visible
		if (TabGuid != host.ActiveTabGuid)
		{
			TabGuid = host.ActiveTabGuid;
			LoadExistingAlarmIfAny();
		}

		EnsureTextures();
		EnsureBodies();
		InitStyles();

		GUI.skin = HighLogic.Skin;
		windowRect = GUI.Window(windowID, windowRect, DrawWindow, string.Empty, GUIStyle.none);

		// Manage input lock for hover over standalone selector window
		UpdateInputLock();
	}

	private void LoadExistingAlarmIfAny()
	{
		selectedBody = null;
		selectedSituation = Vessel.Situations.PRELAUNCH;
		selectedEditorMode = false;
		selectedSpaceCenterMode = false;
		// Reset actions to defaults (all enabled by default)
		actionMiniNote = true; actionPlaySound = true; actionStopWarp = true; actionHideOnExit = false;
		page = Page.Bodies;

		var existing = AlarmManager.GetAlarmsForTab(TabGuid).FirstOrDefault(a => a.Enabled);
		if (existing != null)
		{
			// Special editor alarm?
			if (string.Equals(existing.BodyName, EDITOR_SPECIAL, StringComparison.OrdinalIgnoreCase))
			{
				selectedEditorMode = true;
				selectedSpaceCenterMode = false;
				selectedBody = null;
				selectedSituation = Vessel.Situations.PRELAUNCH;
				actionMiniNote = existing.MiniNote;
				actionPlaySound = existing.PlaySound;
				actionStopWarp = existing.StopWarp;
				actionHideOnExit = existing.HideOnExit;
				page = Page.Summary;
				return;
			}

			// Special SpaceCenter alarm?
			if (string.Equals(existing.BodyName, SPACE_CENTER_SPECIAL, StringComparison.OrdinalIgnoreCase))
			{
				selectedSpaceCenterMode = true;
				selectedEditorMode = false;
				selectedBody = null;
				selectedSituation = Vessel.Situations.PRELAUNCH;
				actionMiniNote = existing.MiniNote;
				actionPlaySound = existing.PlaySound;
				actionStopWarp = existing.StopWarp;
				actionHideOnExit = existing.HideOnExit;
				page = Page.Summary;
				return;
			}

			// Try resolve body by name
			var body = FlightGlobals.Bodies != null ? FlightGlobals.Bodies.FirstOrDefault(b => string.Equals(b.bodyName, existing.BodyName, StringComparison.OrdinalIgnoreCase)) : null;
			selectedBody = body;
			selectedSituation = existing.Situation;
			actionMiniNote = existing.MiniNote;
			actionPlaySound = existing.PlaySound;
			actionStopWarp = existing.StopWarp;
			actionHideOnExit = existing.HideOnExit;
			page = Page.Summary; // jump straight to edit existing alarm
		}
	}

	private void DrawWindow(int id)
	{
		GUI.Box(new Rect(0,0, windowRect.width, windowRect.height), GUIContent.none);
		DrawContent(new Rect(0,0, windowRect.width, windowRect.height));
		GUI.DragWindow(new Rect(0,0, windowRect.width, TOP_BAR_HEIGHT));

		// Consume events to block click-through under standalone window
		var e = Event.current;
		if (e != null)
		{
			if (e.isMouse || e.type == EventType.ScrollWheel)
			{
				e.Use();
			}
		}
	}

	// Embedded rendering: draw selector inside given area without its own Window
	public void DrawEmbedded(Rect area, KerbNote hostWindow, int tabIndex)
	{
		host = hostWindow ?? host;
		string currentGuid = host?.GetTabGuid(tabIndex);
		if (currentGuid != TabGuid) { TabGuid = currentGuid; LoadExistingAlarmIfAny(); }
		EnsureTextures();
		EnsureBodies();
		InitStyles();
		DrawContent(area);
	}

	private void DrawContent(Rect area)
	{
		// Top caption section: shows tab name and current body/situation
		int tabIdx = host != null ? host.GetTabIndexFromGuid(TabGuid) : -1;
		string tabTitle = (host != null && tabIdx >= 0) ? host.GetTabName(tabIdx) : string.Empty;
		string topLine = "Alarm set for: '" + (tabTitle ?? string.Empty) + "'";

		float titleWidth = area.width -2 * PADDING;
		// allow wrapping and measure dynamic number of lines
		titleStyle.wordWrap = true;
		float measured = titleStyle.CalcHeight(new GUIContent(topLine), titleWidth);
		int lines = Mathf.Max(1, Mathf.CeilToInt(measured / TOP_BAR_HEIGHT));
		float topTitleHeight = lines * TOP_BAR_HEIGHT;

		GUI.Label(new Rect(area.x + PADDING, area.y, titleWidth, topTitleHeight), topLine, titleStyle);

		// Body/Situation caption with dynamic wrapped height
		string bodyName;
		if (selectedEditorMode) bodyName = EDITOR_SPECIAL;
		else if (selectedSpaceCenterMode) bodyName = SPACE_CENTER_SPECIAL;
		else bodyName = (selectedBody != null ? selectedBody.bodyName : "(not set)");

		string sitLabel;
		if (selectedEditorMode)
			sitLabel = EDITOR_SPECIAL;
		else if (selectedSpaceCenterMode)
			sitLabel = SPACE_CENTER_SPECIAL;
		else
			sitLabel = (page == Page.Situations) ? "(not set)" : (selectedBody != null ? selectedSituation.ToString() : "(not set)");
		
		string captionText = "Body: " + bodyName + " • Situation: " + sitLabel;
		float captionWidth = area.width -2 * PADDING;
		captionStyle.wordWrap = true;
		float captionHeight = captionStyle.CalcHeight(new GUIContent(captionText), captionWidth);
		GUI.Label(new Rect(area.x + PADDING, area.y + topTitleHeight, captionWidth, captionHeight), captionText, captionStyle);

		float headerBottom = area.y + topTitleHeight + captionHeight +4f;

		// Delete alarm button if an enabled alarm exists for this tab
		var existing = AlarmManager.GetAlarmsForTab(TabGuid).FirstOrDefault(a => a.Enabled);
		if (existing != null)
		{
			if (GUI.Button(new Rect(area.x + PADDING, headerBottom,120f,22f), "Delete alarm", redBtnStyle))
			{
				// Delete current selected or existing
				string delBody;
				if (selectedEditorMode) delBody = EDITOR_SPECIAL;
				else if (selectedSpaceCenterMode) delBody = SPACE_CENTER_SPECIAL;
				else delBody = (selectedBody != null ? selectedBody.bodyName : existing.BodyName);
				
				var delSit = (selectedEditorMode || selectedSpaceCenterMode) ? Vessel.Situations.PRELAUNCH : (selectedBody != null ? selectedSituation : existing.Situation);
				AlarmManager.RemoveAlarm(TabGuid, delBody, delSit);
				// Reset to creation flow
				LoadExistingAlarmIfAny(); // will fall back to Bodies if no alarm
			}
			headerBottom +=22f +6f; // gap after delete button
		}

		// Now page contents area
		float listY = headerBottom;
		float listH = Mathf.Max(0f, area.height - (listY - area.y) - PADDING);
		Rect viewRect = new Rect(area.x + PADDING, listY, area.width -2 * PADDING, listH);

		switch (page)
		{
			case Page.Bodies: DrawBodiesList(viewRect); break;
			case Page.Situations: DrawSituationsList(viewRect); break;
			case Page.Actions: DrawActionsList(viewRect); break;
			case Page.Summary: DrawSummary(viewRect); break;
		}
	}

	private void DrawBodiesList(Rect viewRect)
	{
		float rowH = Mathf.Max(24f, itemStyle.fixedHeight);
		// +2 for special VAB/SPH and SpaceCenter rows
		int rows = (bodies != null ? bodies.Count :0) +2;
		float contentH = rows * (rowH + itemStyle.margin.vertical);
		scroll = GUI.BeginScrollView(viewRect, scroll, new Rect(0,0, viewRect.width -16f, contentH));
		float y =0f;

		// Special row: VAB/SPH
		Rect editorBtnRect = new Rect(0, y, viewRect.width -16f, rowH);
		if (GUI.Button(editorBtnRect, EDITOR_SPECIAL, itemStyle))
		{
			selectedEditorMode = true;
			selectedSpaceCenterMode = false;
			selectedBody = null;
			selectedSituation = Vessel.Situations.PRELAUNCH;
			page = Page.Actions; // skip situations
			scroll = Vector2.zero;
		}
		y += rowH + itemStyle.margin.vertical;

		// Special row: SpaceCenter
		Rect spaceCenterBtnRect = new Rect(0, y, viewRect.width -16f, rowH);
		if (GUI.Button(spaceCenterBtnRect, SPACE_CENTER_SPECIAL, itemStyle))
		{
			selectedSpaceCenterMode = true;
			selectedEditorMode = false;
			selectedBody = null;
			selectedSituation = Vessel.Situations.PRELAUNCH;
			page = Page.Actions; // skip situations
			scroll = Vector2.zero;
		}
		y += rowH + itemStyle.margin.vertical;

		if (bodies != null)
		{
			for (int i =0; i < bodies.Count; i++)
			{
				CelestialBody body = bodies[i];
				string label = body != null ? (string.IsNullOrEmpty(body.bodyName) ? "(unnamed)" : body.bodyName) : "(null)";
				Rect btnRect = new Rect(0, y, viewRect.width -16f, rowH);
				if (GUI.Button(btnRect, label, itemStyle))
				{
					selectedEditorMode = false;
					selectedSpaceCenterMode = false;
					selectedBody = body;
					page = Page.Situations;
					scroll = Vector2.zero;
					OnSelected?.Invoke(body);
				}
				y += rowH + itemStyle.margin.vertical;
			}
		}
		else
		{
			GUI.Label(new Rect(0,0, viewRect.width -16f, rowH), "No celestial body data", GUI.skin.label);
		}
		GUI.EndScrollView();
	}

	private struct SituationItem
	{
		public string Label;
		public Vessel.Situations Situation;
	}

	private void DrawSituationsList(Rect viewRect)
	{
		List<SituationItem> items = GetAvailableSituations(selectedBody);
		float rowH = Mathf.Max(24f, itemStyle.fixedHeight);
		float spacer =6f;
		float contentH = spacer + (1 + items.Count) * (rowH + itemStyle.margin.vertical);
		scroll = GUI.BeginScrollView(viewRect, scroll, new Rect(0,0, viewRect.width -16f, contentH));
		float y =0f;
		y += spacer;

		Rect backRect = new Rect(0, y, viewRect.width -16f, rowH);
		if (GUI.Button(backRect, " <--back", itemStyle))
		{
			page = Page.Bodies;
			scroll = Vector2.zero;
		}
		y += rowH + itemStyle.margin.vertical;

		for (int i =0; i < items.Count; i++)
		{
			var it = items[i];
			Rect btnRect = new Rect(0, y, viewRect.width -16f, rowH);
			if (GUI.Button(btnRect, it.Label, itemStyle))
			{
				selectedSituation = it.Situation;
				OnSituationSelected?.Invoke(selectedBody, it.Situation);
				page = Page.Actions;
				scroll = Vector2.zero;
			}
			y += rowH + itemStyle.margin.vertical;
		}
		GUI.EndScrollView();
	}

	// Actions selection state
	private bool actionMiniNote = true;
	private bool actionPlaySound = true;
	private bool actionStopWarp = true;
	private bool actionHideOnExit = false; // new toggle

	private void DrawActionsList(Rect viewRect)
	{
		float rowH = Mathf.Max(24f, itemStyle.fixedHeight);
		float spacer =6f;
		const float gapBeforeConfirm =8f; // extra gap between action buttons and Confirm
		int rows =1 +4 +2 +1; // back +4 toggles + confirm & close + extra gap
		float contentH = spacer + rows * (rowH + itemStyle.margin.vertical) + gapBeforeConfirm;
		scroll = GUI.BeginScrollView(viewRect, scroll, new Rect(0,0, viewRect.width -16f, contentH));
		float y =0f;
		y += spacer;

		if (GUI.Button(new Rect(0, y, viewRect.width -16f, rowH), " <--back", itemStyle))
		{
			// if editor mode or space center mode, back goes to Bodies; otherwise to Situations
			page = (selectedEditorMode || selectedSpaceCenterMode) ? Page.Bodies : Page.Situations;
			scroll = Vector2.zero;
		}
		y += rowH + itemStyle.margin.vertical;

		// Checkbox toggles instead of buttons
		GUILayout.BeginArea(new Rect(0, y, viewRect.width -16f, 4 * (rowH + itemStyle.margin.vertical)));
		actionMiniNote = GUILayout.Toggle(actionMiniNote, " Mini Note");
		actionPlaySound = GUILayout.Toggle(actionPlaySound, " Kerbal Voice");
		actionStopWarp = GUILayout.Toggle(actionStopWarp, " Kill Warp");
		actionHideOnExit = GUILayout.Toggle(actionHideOnExit, " Hide on Exit");
		GUILayout.EndArea();
		y += 4 * (rowH + itemStyle.margin.vertical);

		bool anySelected = actionMiniNote || actionPlaySound || actionStopWarp;
		if (!anySelected)
		{
			GUI.Label(new Rect(0, y, viewRect.width -16f, rowH), "Select at least one action", GUI.skin.label);
			y += rowH + itemStyle.margin.vertical;
		}

		// extra gap before Confirm
		y += gapBeforeConfirm;

		if (GUI.Button(new Rect(0, y, viewRect.width -16f, rowH), anySelected ? "Confirm" : "Confirm (select action)", itemStyle))
		{
			if (anySelected)
			{
				string bodyName;
				if (selectedEditorMode) bodyName = EDITOR_SPECIAL;
				else if (selectedSpaceCenterMode) bodyName = SPACE_CENTER_SPECIAL;
				else bodyName = (selectedBody != null ? selectedBody.bodyName : string.Empty);

				var def = new AlarmDefinition
				{
					TabGuid = this.TabGuid,
					BodyName = bodyName,
					Situation = (selectedEditorMode || selectedSpaceCenterMode) ? Vessel.Situations.PRELAUNCH : selectedSituation,
					MiniNote = actionMiniNote,
					PlaySound = actionPlaySound,
					StopWarp = actionStopWarp,
					HideOnExit = actionHideOnExit,
					Enabled = true
				};
				AlarmManager.SaveOrUpdateAlarm(def);
				page = Page.Summary;
				scroll = Vector2.zero;
			}
		}
		y += rowH + itemStyle.margin.vertical;

		// Extra spacing to push Close panel lower
		y += rowH;

		// Close panel button (temporary / utility)
		if (GUI.Button(new Rect(0, y, viewRect.width -16f, rowH), "Close panel", itemStyle))
		{
			var cb = OnRequestClosePanel; if (cb != null) cb();
		}
		y += rowH + itemStyle.margin.vertical;

		GUI.EndScrollView();
	}

	private void DrawSummary(Rect viewRect)
	{
		float rowH = Mathf.Max(24f, itemStyle.fixedHeight);
		float spacer =6f;
		int rows =4 +2 +1; //4 toggles + Close + extra gap
		float contentH = spacer + rows * (rowH + itemStyle.margin.vertical);
		scroll = GUI.BeginScrollView(viewRect, scroll, new Rect(0,0, viewRect.width -16f, contentH));
		float y =0f;
		y += spacer;

		// helper: immediate save of current action settings
		Action saveCurrent = () =>
		{
			string bodyName;
			if (selectedEditorMode) bodyName = EDITOR_SPECIAL;
			else if (selectedSpaceCenterMode) bodyName = SPACE_CENTER_SPECIAL;
			else bodyName = selectedBody != null ? selectedBody.bodyName : (AlarmManager.GetAlarmsForTab(TabGuid).FirstOrDefault(a => a.Enabled)?.BodyName ?? string.Empty);
			
			var def = new AlarmDefinition
			{
				TabGuid = this.TabGuid,
				BodyName = bodyName,
				Situation = (selectedEditorMode || selectedSpaceCenterMode) ? Vessel.Situations.PRELAUNCH : selectedSituation,
				MiniNote = actionMiniNote,
				PlaySound = actionPlaySound,
				StopWarp = actionStopWarp,
				HideOnExit = actionHideOnExit,
				Enabled = true
			};
			AlarmManager.SaveOrUpdateAlarm(def);
		};

		// Checkbox toggles with immediate save
		GUILayout.BeginArea(new Rect(0, y, viewRect.width -16f, 4 * (rowH + itemStyle.margin.vertical)));
		
		bool newMiniNote = GUILayout.Toggle(actionMiniNote, " Mini Note");
		if (newMiniNote != actionMiniNote) { actionMiniNote = newMiniNote; saveCurrent(); }
		
		bool newPlaySound = GUILayout.Toggle(actionPlaySound, " Kerbal Voice");
		if (newPlaySound != actionPlaySound) { actionPlaySound = newPlaySound; saveCurrent(); }
		
		bool newStopWarp = GUILayout.Toggle(actionStopWarp, " Kill Warp");
		if (newStopWarp != actionStopWarp) { actionStopWarp = newStopWarp; saveCurrent(); }
		
		bool newHideOnExit = GUILayout.Toggle(actionHideOnExit, " Hide on Exit");
		if (newHideOnExit != actionHideOnExit) { actionHideOnExit = newHideOnExit; saveCurrent(); }
		
		GUILayout.EndArea();
		y += 4 * (rowH + itemStyle.margin.vertical);

		// Extra spacing to push Close panel lower
		y += rowH;

		// Close panel button
		if (GUI.Button(new Rect(0, y, viewRect.width -16f, rowH), "Close panel", itemStyle))
		{
			var cb = OnRequestClosePanel; if (cb != null) cb();
		}
		y += rowH + itemStyle.margin.vertical;

		GUI.EndScrollView();
	}

	public void TriggerSelectedActions()
	{
		if (actionStopWarp)
		{
			try { if (TimeWarp.CurrentRateIndex !=0) TimeWarp.SetRate(0, true); } catch { }
		}

		if (actionMiniNote)
		{
			MiniNote mn = null;
			var minis = GameObject.FindObjectsOfType<MiniNote>();
			for (int i =0; i < minis.Length; i++)
			{
				if (minis[i] != null && string.Equals(minis[i].TabGuid, this.TabGuid, StringComparison.OrdinalIgnoreCase)) { mn = minis[i]; break; }
			}
			if (mn == null)
			{
				var go = new GameObject("MiniNote_Tab_" + TabGuid);
				mn = go.AddComponent<MiniNote>();
				mn.InitWithGuid(host, TabGuid);
				DontDestroyOnLoad(go);
				mn.SpawnedByAlarm = true;
				mn.Show();
				mn.BlinkTripleFast();
			}
			else
			{
				if (!mn.IsVisible)
				{
					mn.SpawnedByAlarm = true;
					mn.Show();
					mn.BlinkTripleFast();
				}
				else
				{
					mn.BlinkFast();
				}
			}
		}

		if (actionPlaySound)
		{
			SoundManager.PlayRandomKerbalVocal();
		}
	}

	private List<SituationItem> GetAvailableSituations(CelestialBody body)
	{
		var list = new List<SituationItem>();
		if (body == null) return list;

		bool hasSOI = body.sphereOfInfluence >0.0;
		bool hasAtmos = body.atmosphere;
		bool hasOcean = body.ocean;
		bool hasSurface = body.pqsController != null;
		string name = body.bodyName ?? string.Empty;
		bool isKerbin = body.isHomeWorld || string.Equals(name, "Kerbin", StringComparison.OrdinalIgnoreCase);
		bool isJool = string.Equals(name, "Jool", StringComparison.OrdinalIgnoreCase);
		bool isSun = string.Equals(name, "Sun", StringComparison.OrdinalIgnoreCase);

		if (hasSOI) list.Add(new SituationItem { Label = "Orbiting", Situation = Vessel.Situations.ORBITING });
		if (hasSOI) list.Add(new SituationItem { Label = "Sub-orbital", Situation = Vessel.Situations.SUB_ORBITAL });
		if (hasSOI) list.Add(new SituationItem { Label = "Exiting SOI", Situation = Vessel.Situations.ESCAPING });
		if (isKerbin) list.Add(new SituationItem { Label = "Prelaunch", Situation = Vessel.Situations.PRELAUNCH });
		if (hasAtmos && !isSun) list.Add(new SituationItem { Label = "Flying (in atmosphere)", Situation = Vessel.Situations.FLYING });
		if (hasSurface && !isJool && !isSun) list.Add(new SituationItem { Label = "Landed (on surface)", Situation = Vessel.Situations.LANDED });
		if (hasOcean) list.Add(new SituationItem { Label = "Splashed down", Situation = Vessel.Situations.SPLASHED });
		list.Add(new SituationItem { Label = "Docked", Situation = Vessel.Situations.DOCKED });

		return list;
	}

	private string EllipsizeToFit(GUIStyle style, string text, float maxWidth)
	{
		if (string.IsNullOrEmpty(text) || style == null) return text ?? string.Empty;
		if (style.CalcSize(new GUIContent(text)).x <= maxWidth) return text;
		const string dots = "…";
		int len = Math.Max(0, text.Length -1);
		while (len >0)
		{
			string candidate = text.Substring(0, len) + dots;
			if (style.CalcSize(new GUIContent(candidate)).x <= maxWidth)
				return candidate;
			len--;
		}
		return dots;
	}

	private void UpdateInputLock()
	{
		try
		{
			var e = Event.current;
			Vector2 mouse = (e != null) ? e.mousePosition : Vector2.zero;
			bool hover = showWindow && windowRect.Contains(mouse);
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
