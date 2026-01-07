using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
 private static SoundManager _instance;
 private object _source; // UnityEngine.AudioSource via reflection
 private object _cachedDefaultClip; // UnityEngine.AudioClip
 private Type _audioSourceType;
 private Type _audioClipType;

 private MethodInfo _stopMethod;
 private MethodInfo _playMethod;
 private MethodInfo _playOneShotMethod;
 private MethodInfo _playOneShotWithVolumeMethod;
 private PropertyInfo _isPlayingProperty;
 private static MethodInfo _getAudioClipMethod;

 // PullUp playback model
 // - Requests are reference-counted (Acquire/Release)
 // - While active: play full clip
 // - When released: if clip started, cut after 3.5s
 // - No overlap: never restart while already playing; restart only after clip fully ends and still active
 private int _pullUpHolds;
 private float _pullUpCutAtRealtime = -1f;
 private float _pullUpRestartAtRealtime = -1f;
 private bool _pullUpPlaying;
 private object _pullUpClip;

 // Kerbal vocal clips paths (without extension)
 private static readonly string[] KerbalVocalPaths = new[]
 {
 	"KerbNoteLite/Sounds/A-haa",
 	"KerbNoteLite/Sounds/Ahhh",
 	"KerbNoteLite/Sounds/Awaaa",
 	"KerbNoteLite/Sounds/Mhm_mhm",
 	"KerbNoteLite/Sounds/Mhm-aha",
 };
 private object[] _kerbalClips; // cached loaded clips

 private static object _cachedGearWarningClip;
 private static object _cachedGearBeepClip;
 private static object _cachedSinkRateClip;
 private static object _cachedLandedClip;
 private static object _cachedStallWarningClip;

 private static readonly System.Collections.Generic.Dictionary<int, object> _cachedAltitudeCallouts = new System.Collections.Generic.Dictionary<int, object>();

 // Volume multiplier for terrain alarm sounds (set by TerrainAlarmRunner)
 private static float _terrainVolume = 1.0f;

 public static void Init()
 {
 if (_instance != null) return;
 var go = new GameObject("KerbNote_SoundManager");
 _instance = go.AddComponent<SoundManager>();
 DontDestroyOnLoad(go);
 }

 private void Awake()
 {
 _audioSourceType = Type.GetType("UnityEngine.AudioSource, UnityEngine.AudioModule")
 				 ?? Type.GetType("UnityEngine.AudioSource, UnityEngine");
 _audioClipType = Type.GetType("UnityEngine.AudioClip, UnityEngine.AudioModule")
 				 ?? Type.GetType("UnityEngine.AudioClip, UnityEngine");

 if (_audioSourceType != null)
 {
 	var addComp = typeof(GameObject).GetMethods()
 		.FirstOrDefault(m => m.Name == "AddComponent" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(Type));
 	var comp = addComp != null ? addComp.Invoke(gameObject, new object[] { _audioSourceType }) : null;
 	_source = comp;
 	if (_source != null)
 	{
 		SetProp(_source, "spatialBlend", 0f);
 		SetProp(_source, "playOnAwake", false);
 		SetProp(_source, "loop", false);
 		SetProp(_source, "priority", 64);
 		SetProp(_source, "volume", 1f);

 		_stopMethod = _source.GetType().GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
 		_playMethod = _source.GetType().GetMethod("Play", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
 		_playOneShotMethod = _source.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
 			.FirstOrDefault(m => m.Name == "PlayOneShot" && m.GetParameters().Length == 1);
 		_playOneShotWithVolumeMethod = _source.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
 			.FirstOrDefault(m => m.Name == "PlayOneShot" && m.GetParameters().Length == 2);
 		_isPlayingProperty = _source.GetType().GetProperty("isPlaying", BindingFlags.Public | BindingFlags.Instance);
 	}
 }
 }

 private void Update()
 {
 	if (_source == null) return;

 	float now = Time.realtimeSinceStartup;

 	// If we were asked to cut (trigger left), stop at cut time.
 	if (_pullUpCutAtRealtime > 0f && now >= _pullUpCutAtRealtime)
 	{
 		_pullUpCutAtRealtime = -1f;
 		try { _stopMethod?.Invoke(_source, null); } catch { }
 		_pullUpPlaying = false;
 		// if still held, schedule immediate restart (non-overlapping)
 		if (_pullUpHolds > 0) _pullUpRestartAtRealtime = now;
 	}

 	// Check if clip ended naturally (we query isPlaying)
 	if (_pullUpPlaying)
 	{
 		bool isPlaying = false;
 		try
 		{
 			if (_isPlayingProperty != null) isPlaying = (bool)_isPlayingProperty.GetValue(_source, null);
 		}
 		catch { }
 		if (!isPlaying)
 		{
 			_pullUpPlaying = false;
 			if (_pullUpHolds > 0) _pullUpRestartAtRealtime = now; // queue next loop
 		}
 	}

 	// Restart if queued and still held
 	if (_pullUpRestartAtRealtime >= 0f && now >= _pullUpRestartAtRealtime)
 	{
 		_pullUpRestartAtRealtime = -1f;
 		if (_pullUpHolds > 0 && !_pullUpPlaying)
 		{
 			PlayPullUpInternal();
 		}
 	}
 }

 private static void SetProp(object obj, string name, object value)
 {
 if (obj == null) return;
 var t = obj.GetType();
 var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
 if (p != null && p.CanWrite) { try { p.SetValue(obj, value, null); } catch { } return; }
 var f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
 if (f != null) { try { f.SetValue(obj, value); } catch { } }
 }

 // Random kerbal vocal
 public static void PlayRandomKerbalVocal()
 {
 if (_instance == null) Init();
 _instance.EnsureKerbalClipsLoaded();
 var clips = _instance._kerbalClips;
 if (clips != null && clips.Length > 0)
 {
 	int idx = UnityEngine.Random.Range(0, clips.Length);
 	PlayClip(clips[idx]);
 }
 else
 {
 	PlayDefaultAlarm();
 }
 }

 private void EnsureKerbalClipsLoaded()
 {
 if (_kerbalClips != null) return;
 var list = new List<object>();
 foreach (var path in KerbalVocalPaths)
 {
 	var clip = TryLoadClip(path);
 	if (clip != null) list.Add(clip);
 }
 _kerbalClips = list.ToArray();
 }

 public static void PlayDefaultAlarm()
 {
 if (_instance == null) Init();
 object clip = _instance._cachedDefaultClip;
 if (clip == null)
 {
 	clip = TryLoadClip("KerbNoteLite/Sounds/Alarm") ?? GenerateBeepClip(0.14f, 1200f);
 	_instance._cachedDefaultClip = clip;
 }
 PlayClip(clip);
 }

 public static void PlayByPath(string dbPath)
 {
 if (_instance == null) Init();
 var clip = TryLoadClip(dbPath) ?? GenerateBeepClip(0.10f, 1000f);
 PlayClip(clip);
 }

 private static void PlayClip(object clip, float volume = 1.0f)
 {
 if (_instance == null || clip == null || _instance._source == null) return;
 
 // Try PlayOneShot with 2 parameters (clip, volume)
 if (_instance._playOneShotWithVolumeMethod != null)
 {
  try { _instance._playOneShotWithVolumeMethod.Invoke(_instance._source, new object[] { clip, volume }); return; } catch { }
 }
 
 // Fallback: PlayOneShot with 1 parameter (no volume control)
 try { _instance._playOneShotMethod?.Invoke(_instance._source, new object[] { clip }); } catch { }
 }

 private static object TryLoadClip(string dbPath)
 {
 if (string.IsNullOrEmpty(dbPath) || GameDatabase.Instance == null) return null;
 try
 {
			// OPTIMIZATION: Cache MethodInfo to avoid repeated reflection
			if (_getAudioClipMethod == null)
			{
				_getAudioClipMethod = typeof(GameDatabase).GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.FirstOrDefault(mm => mm.Name == "GetAudioClip" && mm.GetParameters().Length == 1 && mm.GetParameters()[0].ParameterType == typeof(string));
			}
			return _getAudioClipMethod != null ? _getAudioClipMethod.Invoke(GameDatabase.Instance, new object[] { dbPath }) : null;
 }
 catch { return null; }
 }

 private static object GenerateBeepClip(float durationSec, float freq)
 {
 try
 {
 	var audioClipType = _instance?._audioClipType;
 	if (audioClipType == null) 
 	{
 		audioClipType = Type.GetType("UnityEngine.AudioClip, UnityEngine.AudioModule") ?? Type.GetType("UnityEngine.AudioClip, UnityEngine");
 	}
 	if (audioClipType == null) return null;

 	int sampleRate = 44100;
 	int samples = Mathf.Max(1, Mathf.RoundToInt(durationSec * sampleRate));
 	var data = new float[samples];
 	double inc = 2.0 * Math.PI * freq / sampleRate;
 	double phase = 0.0;
 	for (int i = 0; i < samples; i++)
 	{
 		float env = 1f;
 		if (i < 64) env = i / 64f;
 		else if (i > samples - 256) env = Mathf.Clamp01((samples - i) / 256f);
 		data[i] = (float)Math.Sin(phase) * env * 0.8f;
 		phase += inc;
 	}

 	var create = audioClipType.GetMethods(BindingFlags.Public | BindingFlags.Static)
 		.FirstOrDefault(mi => mi.Name == "Create" && mi.GetParameters().Length == 5);
 	var clip = create?.Invoke(null, new object[] { "KerbNote_Beep", samples, 1, sampleRate, false });
 	var setData = audioClipType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
 		.FirstOrDefault(mi => mi.Name == "SetData" && mi.GetParameters().Length == 2 && mi.GetParameters()[0].ParameterType == typeof(float[]));
 	setData?.Invoke(clip, new object[] { data, 0 });
 	return clip;
 }
 catch { return null; }
 }

 private void EnsurePullUpLoaded()
 {
 	if (_pullUpClip != null) return;
 	_pullUpClip = TryLoadClip("KerbNoteLite/Sounds/Pull_Up")
			   ?? TryLoadClip("KerbNoteLite/Sounds/pull_up");
 }

 private void PlayPullUpInternal()
 {
 	EnsurePullUpLoaded();
 	if (_pullUpClip == null || _source == null) return;

 	_pullUpCutAtRealtime = -1f;
 	try
 	{
 		SetProp(_source, "clip", _pullUpClip);
 		SetProp(_source, "time", 0f);
 		SetProp(_source, "volume", _terrainVolume); // Apply terrain volume
 		_playMethod?.Invoke(_source, null);
 		_pullUpPlaying = true;
 	}
 	catch
 	{
 		try { _playOneShotMethod?.Invoke(_source, new object[] { _pullUpClip }); } catch { }
 		_pullUpPlaying = true;
 	}
 }

 // Acquire: call while alarm condition is TRUE (e.g., inside trigger)
 public static void PullUpAcquire()
 {
 	if (_instance == null) Init();
 	_instance.EnsurePullUpLoaded();
 	_instance._pullUpHolds++;
 	if (_instance._pullUpHolds == 1)
 	{
 		// start immediately
 		_instance._pullUpRestartAtRealtime = Time.realtimeSinceStartup;
 	}
 }

 // Release: call when alarm condition becomes FALSE
 public static void PullUpRelease()
 {
 	if (_instance == null) return;
 	if (_instance._pullUpHolds > 0) _instance._pullUpHolds--;
 	if (_instance._pullUpHolds == 0)
 	{
 		// If currently playing, cut after 3.5s from now.
 		if (_instance._pullUpPlaying)
 		{
 			_instance._pullUpCutAtRealtime = Time.realtimeSinceStartup + 3.5f;
 		}
 		_instance._pullUpRestartAtRealtime = -1f;
 	}
 }

 // Legacy: immediate one-shot trimmed
 public static void PlayPullUp()
 {
 	PullUpAcquire();
 	PullUpRelease();
 }

 public static void PlayGearBeep(float volume = 1.0f)
 {
 	if (_instance == null) Init();
 	if (_cachedGearWarningClip == null)
 	{
 		_cachedGearWarningClip = TryLoadClip("KerbNoteLite/Sounds/Too_Low_Gear")
 							  ?? TryLoadClip("KerbNoteLite/Sounds/too_low_gear")
 							  ?? GenerateBeepClip(0.12f, 900f);
 	}
 	PlayClip(_cachedGearWarningClip, volume);
 }

 public static void PlayAltitudeCallout(int meters, float volume = 1.0f)
 {
 	if (_instance == null) Init();
 	object clip;
 	if (!_cachedAltitudeCallouts.TryGetValue(meters, out clip) || clip == null)
 	{
 		clip = TryLoadClip($"KerbNoteLite/Sounds/{meters}");
 		_cachedAltitudeCallouts[meters] = clip;
 	}
 	if (clip != null)
 	{
 		PlayClip(clip, volume);
 	}
 	else
 	{
 		// fallback: audible confirmation even if asset is missing
 		PlayClip(GenerateBeepClip(0.05f, 1400f), volume);
 	}
 	// Note: Screen message moved to caller (TerrainAlarmRunner) for separate control
 }

 public static void PlaySinkRate(float volume = 1.0f)
 {
 	if (_instance == null) Init();
 	if (_cachedSinkRateClip == null)
 	{
 		_cachedSinkRateClip = TryLoadClip("KerbNoteLite/Sounds/Sink_Rate")
  						  ?? TryLoadClip("KerbNoteLite/Sounds/sink_rate")
  						  ?? GenerateBeepClip(0.08f, 600f);
 	}
 	PlayClip(_cachedSinkRateClip, volume);
 }

 public static void PlayLandedCallout(float volume = 1.0f)
 {
 	if (_instance == null) Init();
 	if (_cachedLandedClip == null)
 	{
 		_cachedLandedClip = TryLoadClip("KerbNoteLite/Sounds/Landed")
                            ?? TryLoadClip("KerbNoteLite/Sounds/landed")
                            ?? GenerateBeepClip(0.07f, 1100f);
 	}
 	PlayClip(_cachedLandedClip, volume);
 }

 public static void PlayStallWarning(float volume = 1.0f)
 {
 	if (_instance == null) Init();
 	if (_cachedStallWarningClip == null)
 	{
 		_cachedStallWarningClip = TryLoadClip("KerbNoteLite/Sounds/Stall")
                                  ?? TryLoadClip("KerbNoteLite/Sounds/stall")
                                  ?? TryLoadClip("KerbNoteLite/Sounds/STALL")
                                  ?? GenerateBeepClip(0.15f, 800f); // Fallback: deeper beep for stall
  	}
  	PlayClip(_cachedStallWarningClip, volume);
 }

 public static void SetTerrainVolume(float volume)
 {
 	_terrainVolume = Mathf.Clamp01(volume);
 }
}
