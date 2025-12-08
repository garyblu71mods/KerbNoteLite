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

 public static void Init()
 {
 if (_instance != null) return;
 var go = new GameObject("KerbNote_SoundManager");
 _instance = go.AddComponent<SoundManager>();
 DontDestroyOnLoad(go);
 }

 private void Awake()
 {
 // Resolve types across Unity versions
 _audioSourceType = Type.GetType("UnityEngine.AudioSource, UnityEngine.AudioModule")
 				 ?? Type.GetType("UnityEngine.AudioSource, UnityEngine");
 _audioClipType = Type.GetType("UnityEngine.AudioClip, UnityEngine.AudioModule")
 				 ?? Type.GetType("UnityEngine.AudioClip, UnityEngine");

 if (_audioSourceType != null)
 {
 	// GameObject.AddComponent(Type)
 	var addComp = typeof(GameObject).GetMethods()
 		.FirstOrDefault(m => m.Name == "AddComponent" && m.GetParameters().Length ==1 && m.GetParameters()[0].ParameterType == typeof(Type));
 	var comp = addComp != null ? addComp.Invoke(gameObject, new object[] { _audioSourceType }) : null;
 	_source = comp; // Component
 	if (_source != null)
 	{
 		SetProp(_source, "spatialBlend",0f);
 		SetProp(_source, "playOnAwake", false);
 		SetProp(_source, "loop", false);
 		SetProp(_source, "priority",64);
 		SetProp(_source, "volume",1f);
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
 if (clips != null && clips.Length >0)
 {
 	int idx = UnityEngine.Random.Range(0, clips.Length);
 	PlayClip(clips[idx]);
 }
 else
 {
 	// fallback
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
 	clip = TryLoadClip("KerbNoteLite/Sounds/Alarm") ?? GenerateBeepClip(0.14f,1200f);
 	_instance._cachedDefaultClip = clip;
 }
 PlayClip(clip);
 }

 public static void PlayByPath(string dbPath)
 {
 if (_instance == null) Init();
 var clip = TryLoadClip(dbPath) ?? GenerateBeepClip(0.10f,1000f);
 PlayClip(clip);
 }

 private static void PlayClip(object clip)
 {
 if (_instance == null || clip == null || _instance._source == null) return;
 var play = _instance._source.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
 	.FirstOrDefault(m => m.Name == "PlayOneShot" && m.GetParameters().Length ==1);
 try { play?.Invoke(_instance._source, new object[] { clip }); } catch { }
 }

 private static object TryLoadClip(string dbPath)
 {
 if (string.IsNullOrEmpty(dbPath) || GameDatabase.Instance == null) return null;
 try
 {
 	var m = typeof(GameDatabase).GetMethods(BindingFlags.Public | BindingFlags.Instance)
 		.FirstOrDefault(mm => mm.Name == "GetAudioClip" && mm.GetParameters().Length ==1 && mm.GetParameters()[0].ParameterType == typeof(string));
 	return m != null ? m.Invoke(GameDatabase.Instance, new object[] { dbPath }) : null;
 }
 catch { return null; }
 }

 private static object GenerateBeepClip(float durationSec, float freq)
 {
 try
 {
 	var audioClipType = Type.GetType("UnityEngine.AudioClip, UnityEngine.AudioModule") ?? Type.GetType("UnityEngine.AudioClip, UnityEngine");
 	if (audioClipType == null) return null;

 	int sampleRate =44100;
 	int samples = Mathf.Max(1, Mathf.RoundToInt(durationSec * sampleRate));
 	var data = new float[samples];
 	double inc =2.0 * Math.PI * freq / sampleRate;
 	double phase =0.0;
 	for (int i =0; i < samples; i++)
 	{
 		float env =1f;
 		if (i <64) env = i /64f; // quick fade in
 		else if (i > samples -256) env = Mathf.Clamp01((samples - i) /256f); // quick fade out
 		data[i] = (float)Math.Sin(phase) * env *0.8f;
 		phase += inc;
 	}

 	// AudioClip.Create(name, lengthSamples, channels, frequency, stream)
 	var create = audioClipType.GetMethods(BindingFlags.Public | BindingFlags.Static)
 		.FirstOrDefault(mi => mi.Name == "Create" && mi.GetParameters().Length ==5);
 	var clip = create?.Invoke(null, new object[] { "KerbNote_Beep", samples,1, sampleRate, false });
 	var setData = audioClipType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
 		.FirstOrDefault(mi => mi.Name == "SetData" && mi.GetParameters().Length ==2 && mi.GetParameters()[0].ParameterType == typeof(float[]));
 	setData?.Invoke(clip, new object[] { data,0 });
 	return clip;
 }
 catch { return null; }
 }
}
