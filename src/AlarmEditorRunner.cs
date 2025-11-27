using System;
using System.Collections;
using System.Linq;
using UnityEngine;

// Triggers tab alarms marked as VAB/SPH when entering the Editor (VAB or SPH)
[KSPAddon(KSPAddon.Startup.EditorAny, false)]
public class AlarmEditorRunner : MonoBehaviour
{
	private const string EDITOR_SPECIAL = "VAB/SPH";
	private bool triggered = false;

	private void Start()
	{
		GameEvents.onGameSceneLoadRequested.Add(OnSceneLoadRequested);
		StartCoroutine(DelayedTrigger());
	}

	private void OnDestroy()
	{
		try { HideVabSpawnedNotesForHideOnExit(); } catch { }
		GameEvents.onGameSceneLoadRequested.Remove(OnSceneLoadRequested);
	}

	private IEnumerator DelayedTrigger()
	{
		yield return new WaitForSecondsRealtime(0.2f);
		TryTriggerEditorAlarms();
		yield return new WaitForSecondsRealtime(0.8f);
		TryTriggerEditorAlarms();
	}

	private void TryTriggerEditorAlarms()
	{
		if (triggered) return;
		try
		{
			var all = AlarmManager.Alarms;
			if (all == null || all.Count ==0) return;
			var matches = all.Where(a => a.Enabled && string.Equals(a.BodyName, EDITOR_SPECIAL, StringComparison.OrdinalIgnoreCase)).ToArray();
			if (matches.Length ==0) return;

			var host = GameObject.FindObjectsOfType<KerbNote>().FirstOrDefault();
			foreach (var a in matches)
			{
				// Mini Note
				if (host != null && a.MiniNote)
				{
					var minis = GameObject.FindObjectsOfType<MiniNote>();
					MiniNote mn = null;
					for (int i =0; i < minis.Length; i++)
					{
						if (minis[i] != null && string.Equals(minis[i].TabGuid, a.TabGuid, StringComparison.OrdinalIgnoreCase)) { mn = minis[i]; break; }
					}
					if (mn == null)
					{
						var go = new GameObject("MiniNote_Tab_" + a.TabGuid);
						mn = go.AddComponent<MiniNote>();
						mn.InitWithGuid(host, a.TabGuid);
						UnityEngine.Object.DontDestroyOnLoad(go);
						mn.SpawnedByAlarm = true;
						mn.Show();
						mn.BlinkTripleFast();
					}
					else
					{
						mn.SpawnedByAlarm = true;
						if (!mn.IsVisible) { mn.Show(); mn.BlinkTripleFast(); }
						else { mn.BlinkFast(); }
					}
				}

				// Play Sound
				if (a.PlaySound)
				{
					SoundManager.PlayRandomKerbalVocal();
				}
			}

			triggered = true;
		}
		catch (Exception ex)
		{
			Debug.LogError("[KerbNote][AlarmEditorRunner] Error triggering editor alarm: " + ex.Message);
		}
	}

	private void OnSceneLoadRequested(GameScenes scene)
	{
		try
		{
			if (scene != GameScenes.EDITOR)
			{
				HideVabSpawnedNotesForHideOnExit();
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[KerbNote][AlarmEditorRunner] OnSceneLoadRequested error: " + ex.Message);
		}
	}

	private void HideVabSpawnedNotesForHideOnExit()
	{
		try
		{
			var all = AlarmManager.Alarms;
			if (all == null || all.Count ==0) return;
			var exitAlarms = all.Where(a => a.Enabled && a.MiniNote && a.HideOnExit && string.Equals(a.BodyName, EDITOR_SPECIAL, StringComparison.OrdinalIgnoreCase)).ToArray();
			if (exitAlarms.Length ==0) return;
			var minis = GameObject.FindObjectsOfType<MiniNote>();
			foreach (var a in exitAlarms)
			{
				for (int i =0; i < minis.Length; i++)
				{
					var mn = minis[i];
					if (mn != null && string.Equals(mn.TabGuid, a.TabGuid, StringComparison.OrdinalIgnoreCase) && mn.IsVisible && mn.SpawnedByAlarm)
					{
						mn.Hide();
					}
				}
			}
		}
		catch { }
	}
}
