using System;
using System.Collections;
using System.Linq;
using UnityEngine;

// Triggers tab alarms marked as SpaceCenter when entering the Space Center scene
[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
public class AlarmSpaceCenterRunner : MonoBehaviour
{
	private const string SPACE_CENTER_SPECIAL = "SpaceCenter";
	private bool triggered = false;

	private void Start()
	{
		GameEvents.onGameSceneLoadRequested.Add(OnSceneLoadRequested);
		StartCoroutine(DelayedTrigger());
	}

	private void OnDestroy()
	{
		try { HideSpaceCenterSpawnedNotesForHideOnExit(); } catch { }
		GameEvents.onGameSceneLoadRequested.Remove(OnSceneLoadRequested);
	}

	private IEnumerator DelayedTrigger()
	{
		yield return new WaitForSecondsRealtime(0.2f);
		TryTriggerSpaceCenterAlarms();
		yield return new WaitForSecondsRealtime(0.8f);
		TryTriggerSpaceCenterAlarms();
	}

	private void TryTriggerSpaceCenterAlarms()
	{
		if (triggered) return;
		try
		{
			var all = AlarmManager.Alarms;
			if (all == null || all.Count ==0) return;
			var matches = all.Where(a => a.Enabled && string.Equals(a.BodyName, SPACE_CENTER_SPECIAL, StringComparison.OrdinalIgnoreCase)).ToArray();
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
			Debug.LogError("[KerbNote][AlarmSpaceCenterRunner] Error triggering SpaceCenter alarm: " + ex.Message);
		}
	}

	private void OnSceneLoadRequested(GameScenes scene)
	{
		try
		{
			if (scene != GameScenes.SPACECENTER)
			{
				HideSpaceCenterSpawnedNotesForHideOnExit();
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[KerbNote][AlarmSpaceCenterRunner] OnSceneLoadRequested error: " + ex.Message);
		}
	}

	private void HideSpaceCenterSpawnedNotesForHideOnExit()
	{
		try
		{
			var all = AlarmManager.Alarms;
			if (all == null || all.Count ==0) return;
			var exitAlarms = all.Where(a => a.Enabled && a.MiniNote && a.HideOnExit && string.Equals(a.BodyName, SPACE_CENTER_SPECIAL, StringComparison.OrdinalIgnoreCase)).ToArray();
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
