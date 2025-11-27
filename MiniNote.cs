using UnityEngine;
using System.Collections;
using System.Linq;

// A small, draggable window that mirrors the text from the currently active KerbNote tab
public class MiniNote : MonoBehaviour
{
	private Rect windowRect = new Rect(300,300,260,140); // smaller default size
	private int windowID =987659;
	private bool showWindow = false;
	private bool positionedFromHost = false; // set position relative to host once on first Show

	private Texture2D noteWindowTex; // noteWindow.png (KerbNote.TEXTURE_NOTE_WINDOW)
	private GUIStyle contentStyle;
	private bool stylesInitialized = false;

	private const float TOP_BAR_HEIGHT =16f; // smaller bar
	private const float PADDING =8f;
	private const float MIN_WIDTH =180f;
	private const float MIN_HEIGHT =100f;
	private const float MAX_HEIGHT_RATIO =0.4f; // a bit smaller max height

	private Vector2 scroll = Vector2.zero;

	private KerbNote host; // reference to main window to pull tab text
	public string TabGuid { get; private set; } // używamy GUID zamiast indeksu
	
	// Backward compat: TabIndex property dla starych wywołań
	public int TabIndex 
	{ 
		get { return host != null ? host.GetTabIndexFromGuid(TabGuid) : -1; }
	}

	// store alpha for use inside DrawWindow
	private float currentAlpha =0.4f;
	private const float FADE_SPEED =6f; // alpha units per second

	// Blink override
	private bool alphaOverrideActive = false;
	private float alphaOverrideValue =1f;
	private Coroutine blinkRoutine;

	// Hold alpha at80% toggle
	private bool keepAlphaAtEighty = false;

	// Draw order: Main Window (default), Panel (-100), MiniNote (super top now at -2000)
	private const int MININOTE_DEPTH = -2000; // bardzo wysoki priorytet rysowania (niższa wartość = nad innymi)
	// Delay hover fade
	private bool lastHover = false;
	private float hoverChangeTimestamp = 0f;
	private float delayedTargetAlpha = 0.4f;
	private const float HOVER_DELAY = 0.2f; // sekundy opóźnienia

	// Flag telling if this MiniNote visibility was caused by an alarm trigger
	public bool SpawnedByAlarm { get; set; }
	
	// Cache for text content to avoid fetching every frame
	private string cachedText = string.Empty;
	private string cachedTabTitle = string.Empty;
	private int lastFrameUpdate = -1;
	
	// Cache for CalcSize results to avoid recalculating every frame
	private float cachedCloseWidth = 22f;
	private float cachedEditWidth = 40f;
	private float cachedRingWidth = 20f;

	// Persist and keep own position across scenes
	void Awake()
	{
		DontDestroyOnLoad(gameObject);
		GameEvents.onGameSceneLoadRequested.Add(OnSceneLoadRequested);
		// Subscribe to global skin changes to refresh MiniNote texture live
		KerbNote.SkinChanged += OnSkinChanged;
	}

	void OnDestroy()
	{
		GameEvents.onGameSceneLoadRequested.Remove(OnSceneLoadRequested);
		KerbNote.SkinChanged -= OnSkinChanged;
	}

	private void OnSkinChanged(string skin)
	{
		// Refresh background texture from current SkinAssets
		try
		{
			noteWindowTex = SkinAssets.Get("NoteWindow") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_NOTE_WINDOW, false);
			// Force a repaint next OnGUI; styles are not texture-bound here, so no rebuild needed
		}
		catch { }
	}

	private void OnSceneLoadRequested(GameScenes scene)
	{
		// Hide on transitions to main menu or when leaving game scenes
		if (scene == GameScenes.MAINMENU)
		{
			Hide();
		}
	}

	// Backward-compat Init
	public void Init(KerbNote hostWindow)
	{
		Init(hostWindow,0);
	}

	// Preferred Init with explicit tab association
	public void Init(KerbNote hostWindow, int tabIndex)
	{
		host = hostWindow;
		TabGuid = host?.GetTabGuid(tabIndex);
		// ensure unique window id per instance
		windowID = GetInstanceID();
		if (noteWindowTex == null)
			noteWindowTex = SkinAssets.Get("NoteWindow") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_NOTE_WINDOW, false);
	}
	
	// New Init with GUID directly
	public void InitWithGuid(KerbNote hostWindow, string tabGuid)
	{
		host = hostWindow;
		TabGuid = tabGuid;
		windowID = GetInstanceID();
		if (noteWindowTex == null)
			noteWindowTex = SkinAssets.Get("NoteWindow") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_NOTE_WINDOW, false);
	}

	public void SetHost(KerbNote newHost)
	{
		host = newHost;
		// Do not reset position on scene changes; keep current location
		// positionedFromHost remains as is
	}

	public void Show()
	{
		// Position new MiniNote near the right edge, leaving room for stock toolbar and within safe vertical band
		if (!positionedFromHost)
		{
			float w = windowRect.width;
			float h = windowRect.height;

			// Stock AppLauncher icons are ~38x38; keep extra padding so we don't cover toolbar
			const float TOOLBAR_SIZE =38f;
			float rightMargin = TOOLBAR_SIZE +12f; // horizontal offset from right edge
			float x = Mathf.Max(10f, Screen.width - rightMargin - w);

			// Keep top and bottom margins equal to3x toolbar size
			float verticalMargin =3f * TOOLBAR_SIZE;
			float yMin = verticalMargin;
			float yMax = Mathf.Max(verticalMargin, Screen.height - h - verticalMargin);
			float y = Random.Range(yMin, yMax);

			windowRect.position = new Vector2(x, y);
			positionedFromHost = true;
		}
		showWindow = true;
		
		// Notify SliderWindow that a MiniNote is now visible
		SliderWindow.NotifyMiniNoteVisibilityChanged(true);
	}

	public void Hide()
	{
		showWindow = false;
		// when hidden explicitly, clear alarm origin flag to avoid incorrectly hiding a manually reopened note later
		SpawnedByAlarm = false;
		
		// Check if ANY other MiniNote is still visible before notifying
		bool anyOtherVisible = false;
		try
		{
			var allMinis = GameObject.FindObjectsOfType<MiniNote>();
			foreach (var mn in allMinis)
			{
				if (mn != null && mn != this && mn.IsVisible)
				{
					anyOtherVisible = true;
					break;
				}
			}
		}
		catch { }
		
		// Only notify if NO MiniNotes are visible anymore
		if (!anyOtherVisible)
		{
			SliderWindow.NotifyMiniNoteVisibilityChanged(false);
		}
	}

	public void Toggle()
	{
		if (!showWindow) Show(); else Hide();
	}

	public bool IsVisible => showWindow;

	// Trigger a fast single blink
	public void BlinkFast()
	{
		StartBlink(1);
	}

	// Trigger a very fast triple blink
	public void BlinkTripleFast()
	{
		StartBlink(3);
	}

	private void StartBlink(int pulses)
	{
		if (blinkRoutine != null) StopCoroutine(blinkRoutine);
		blinkRoutine = StartCoroutine(BlinkRoutine(pulses));
	}

	private IEnumerator BlinkRoutine(int pulses)
	{
		alphaOverrideActive = true;
		for (int i =0; i < pulses; i++)
		{
			alphaOverrideValue =1f; // bright
			yield return new WaitForSecondsRealtime(0.06f);
			alphaOverrideValue =0.15f; // dim
			yield return new WaitForSecondsRealtime(0.06f);
		}
		alphaOverrideActive = false;
		blinkRoutine = null;
	}

	void OnGUI()
	{
		if (!showWindow) return;
		// Hide MiniNote when About/Help modal visible
		if (SettingWindow.IsAboutVisible) return;
		
		// Only initialize styles once
		if (!stylesInitialized) InitStyles();
		if (host == null) { Hide(); return; }
		
		// Update cached text only once per frame (avoid multiple fetches in DrawWindow)
		int currentFrame = Time.frameCount;
		if (lastFrameUpdate != currentFrame)
		{
			lastFrameUpdate = currentFrame;
			int tabIdx = host.GetTabIndexFromGuid(TabGuid);
			if (tabIdx >= 0)
			{
				cachedText = host.GetTabText(tabIdx) ?? string.Empty;
				cachedTabTitle = host.GetTabName(tabIdx) ?? string.Empty;
			}
			else
			{
				cachedText = "(tab removed)";
				cachedTabTitle = "(removed)";
			}
		}
		
		int prevDepth = GUI.depth; 
		GUI.depth = MININOTE_DEPTH; // MiniNote ponad wszystkimi innymi elementami
		
		bool isHover = windowRect.Contains(Event.current.mousePosition);
		// Track hover state changes
		if (isHover != lastHover)
		{
			lastHover = isHover;
			hoverChangeTimestamp = Time.unscaledTime;
		}
		// Determine delayed target alpha (ignore delay when keepAlphaAtEighty or blink override)
		if (!keepAlphaAtEighty && !alphaOverrideActive)
		{
			if (Time.unscaledTime - hoverChangeTimestamp >= HOVER_DELAY)
			{
				delayedTargetAlpha = isHover ? 0.8f : 0.4f;
			}
		}
		else if (keepAlphaAtEighty && !alphaOverrideActive)
		{
			// Manual hold overrides delay
			delayedTargetAlpha = 0.8f;
		}
		float targetAlpha = delayedTargetAlpha;
		if (Event.current.type == EventType.Repaint)
		{
			if (alphaOverrideActive)
				currentAlpha = alphaOverrideValue;
			else
				currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.unscaledDeltaTime * FADE_SPEED);
		}
		Color prevColor = GUI.color, prevBg = GUI.backgroundColor, prevContent = GUI.contentColor;
		GUI.color = new Color(1f,1f,1f, currentAlpha);
		GUI.backgroundColor = new Color(1f,1f,1f, currentAlpha);
		GUI.contentColor = new Color(1f,1f,1f, currentAlpha);
		GUI.skin = HighLogic.Skin;
		
		windowRect = GUI.Window(windowID, windowRect, DrawWindow, string.Empty, GUIStyle.none);
		
		// FORCE MiniNote to ALWAYS be on top by bringing it to front EVERY frame
		// This ensures it stays above Main Window and Mini Button even after they call BringWindowToFront
		GUI.BringWindowToFront(windowID);
		
		GUI.color = prevColor; 
		GUI.backgroundColor = prevBg; 
		GUI.contentColor = prevContent; 
		GUI.depth = prevDepth;
	}

	private void InitStyles()
	{
		stylesInitialized = true;
		contentStyle = new GUIStyle(GUI.skin.label);
		contentStyle.wordWrap = true; contentStyle.richText = true; contentStyle.fontSize =14; contentStyle.fontStyle = FontStyle.Bold;
		contentStyle.alignment = TextAnchor.UpperLeft; contentStyle.normal.textColor = new Color(0.95f,0.95f,0.85f);
		
		// Pre-calculate button widths once during init
		GUIStyle btnStyle = GUI.skin.button;
		cachedCloseWidth = Mathf.Clamp(btnStyle.CalcSize(new GUIContent("X")).x +8f, 22f, 28f);
		cachedEditWidth = Mathf.Clamp(btnStyle.CalcSize(new GUIContent("Edit")).x +10f, 30f, 56f);
		cachedRingWidth = Mathf.Clamp(btnStyle.CalcSize(new GUIContent("O")).x +6f, 16f, 24f);
	}

	private void DrawWindow(int id)
	{
		Color prevColor = GUI.color, prevBg = GUI.backgroundColor, prevContent = GUI.contentColor;
		GUI.color = new Color(1f,1f,1f, currentAlpha);
		GUI.backgroundColor = new Color(1f,1f,1f, currentAlpha);
		GUI.contentColor = new Color(1f,1f,1f, currentAlpha);
		if (host == null)
		{
			var newHost = Object.FindObjectOfType<KerbNote>();
			if (newHost != null) host = newHost; else { Hide(); return; }
		}
		
		// Only reload texture if it's null
		if (noteWindowTex == null) 
			noteWindowTex = SkinAssets.Get("NoteWindow") ?? GameDatabase.Instance.GetTexture(KerbNote.TEXTURE_NOTE_WINDOW, false);
		if (noteWindowTex != null) 
			GUI.DrawTexture(new Rect(0,0, windowRect.width, windowRect.height), noteWindowTex);

		// Show controls when the instance alpha is at ~80%
		bool showControls = currentAlpha >=0.75f;

		// Title label (reserve space for buttons only when controls are visible) - use cached title
		var titleStyle = new GUIStyle(GUI.skin.label){ fontSize =12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.8f,0.7f,0.3f,1f) } };
		GUIStyle btnStyle = GUI.skin.button;
		
		// Use pre-cached button widths instead of recalculating every frame
		float closeW = cachedCloseWidth;
		float editW = cachedEditWidth;
		float ringW = cachedRingWidth;
		string ringLabel = keepAlphaAtEighty ? "*" : "O"; // fallback-friendly symbols
		
		float reservedRight = showControls ? (editW +4f + ringW +4f + closeW) :0f;
		GUI.Label(new Rect(PADDING,0, windowRect.width - PADDING *2 - reservedRight, TOP_BAR_HEIGHT), cachedTabTitle, titleStyle);

		// Alarm info line: show Situation-Body if an enabled alarm with MiniNote binding exists for this tab
		string alarmInfo = string.Empty;
		var alarm = AlarmManager.GetAlarmsForTab(TabGuid).FirstOrDefault(a => a.Enabled && a.MiniNote);
		if (alarm != null)
		{
			string body = string.IsNullOrEmpty(alarm.BodyName) ? "" : alarm.BodyName;
			string sit = alarm.Situation.ToString();
			alarmInfo = string.IsNullOrEmpty(body) ? sit : (sit + "-" + body);
		}
		float alarmLineHeight =0f;
		if (!string.IsNullOrEmpty(alarmInfo))
		{
			var alarmStyle = new GUIStyle(GUI.skin.label){ fontSize =11, fontStyle = FontStyle.Italic, alignment = TextAnchor.UpperLeft, normal = { textColor = new Color(0.9f,0.8f,0.5f,1f) } };
			alarmLineHeight = Mathf.Max(12f, alarmStyle.lineHeight);
			GUI.Label(new Rect(PADDING, TOP_BAR_HEIGHT -2f, windowRect.width - PADDING *2 - reservedRight, alarmLineHeight), alarmInfo, alarmStyle);
		}

		// Buttons on the right: [edit] [O/●] [X] only when controls visible
		if (showControls)
		{
			float btnH = TOP_BAR_HEIGHT -4f;
			float btnY =2f;
			float xRight = windowRect.width - PADDING;
			// X (rightmost)
			if (GUI.Button(new Rect(xRight - (closeW), btnY, closeW, btnH), "X")) Hide();
			xRight -= closeW +4f;
			// edit (next)
			if (GUI.Button(new Rect(xRight - editW, btnY, editW, btnH), "edit"))
			{
				if (host != null)
				{
					// Toggle behavior: if main window hidden or on different tab -> open; if visible on same tab -> hide
					bool hostVisible = host.IsWindowVisible;
					int activeIdx = host.GetTabIndexFromGuid(TabGuid);
					int currentActiveIdx = host.ActiveTabIndex;
					if (!hostVisible || currentActiveIdx != activeIdx)
					{
						if (activeIdx >= 0) host.OpenOnTab(activeIdx);
						// Natychmiast wysuń panel alarmów przy edycji z MiniNote
						host.ShowAlarmPanelImmediate();
					}
					else
					{
						host.HideWindow();
					}
				}
			}
			xRight -= editW +4f;
			// O/● toggle (leftmost of the right controls)
			if (GUI.Button(new Rect(xRight - ringW, btnY, ringW, btnH), ringLabel))
			{
				keepAlphaAtEighty = !keepAlphaAtEighty;
				if (keepAlphaAtEighty) currentAlpha =0.8f; // snap to desired level
			}
		}

		float maxWindowHeight = Mathf.Max(MIN_HEIGHT, Screen.height * MAX_HEIGHT_RATIO);
		float availableWidth = windowRect.width - PADDING *2f;
		float contentY = TOP_BAR_HEIGHT + (alarmLineHeight >0f ? alarmLineHeight :0f) + PADDING *0.5f;
		float availableHeight = windowRect.height - contentY - PADDING;
		
		// Use cached text instead of fetching from host again
		string text = cachedText;
		float desiredContentHeight = contentStyle.CalcHeight(new GUIContent(text), availableWidth);
		float desiredWindowHeight = Mathf.Clamp(TOP_BAR_HEIGHT + (alarmLineHeight >0f ? alarmLineHeight :0f) + PADDING + desiredContentHeight + PADDING, MIN_HEIGHT, maxWindowHeight);
		if (Mathf.Abs(desiredWindowHeight - windowRect.height) >0.5f)
		{ windowRect.height = desiredWindowHeight; availableHeight = windowRect.height - contentY - PADDING; }
		if (desiredContentHeight > availableHeight +1f)
		{ scroll = GUI.BeginScrollView(new Rect(PADDING, contentY, availableWidth, availableHeight), scroll, new Rect(0,0, availableWidth -16f, desiredContentHeight));
			GUI.Label(new Rect(0,0, availableWidth -16f, desiredContentHeight), text, contentStyle); GUI.EndScrollView(); }
		else
		{ GUI.Label(new Rect(PADDING, contentY, availableWidth, desiredContentHeight), text, contentStyle); }
		GUI.DragWindow(new Rect(0,0, windowRect.width, TOP_BAR_HEIGHT));
		GUI.color = prevColor; GUI.backgroundColor = prevBg; GUI.contentColor = prevContent;
	}
}
