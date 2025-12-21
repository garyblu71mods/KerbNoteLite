using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[KSPAddon(KSPAddon.Startup.Flight, false)]
public class AlarmRunner : MonoBehaviour
{
	private readonly Dictionary<string, float> lastTriggerTimes = new Dictionary<string, float>();
	private const float TriggerThrottleSeconds =2f;
	private readonly Dictionary<string, float> lastBodyTriggerTimes = new Dictionary<string, float>();
	private const float BodyDebounceSeconds =8f;
	private struct VesselState { public string Body; public Vessel.Situations Situation; }
	private readonly Dictionary<string, VesselState> lastStateByVessel = new Dictionary<string, VesselState>();
	private string lastActiveSaveOverride = null;
	private static bool forceNextEvaluation = false;
	
	// Global load cooldown - suppress all alarms for first 20 seconds after scene load
	private float _sceneLoadTime;
	private const float LoadCooldownSeconds = 20f;

	// Track last matching alarms per vessel to detect exit from alarm condition
	private readonly Dictionary<string, List<AlarmDefinition>> lastMatchedAlarmsByVessel = new Dictionary<string, List<AlarmDefinition>>();

	private void Awake()
	{
		GameEvents.onVesselSituationChange.Add(OnVesselSituationChange);
		GameEvents.onVesselSOIChanged.Add(OnVesselSOIChanged);
		GameEvents.onFlightReady.Add(OnFlightReady);
		GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
		GameEvents.onDockingComplete.Add(OnDockingComplete);
		GameEvents.onPartCouple.Add(OnPartCouple);
		GameEvents.onVesselChange.Add(OnVesselSwitched);
		
		// Initialize scene load time
		_sceneLoadTime = Time.realtimeSinceStartup;
	}
	
	private void OnVesselSwitched(Vessel v)
	{
		// Reset cooldown timer when switching vessels to prevent false alarms
		_sceneLoadTime = Time.realtimeSinceStartup;
	}
	
	private void OnDestroy()
	{
		GameEvents.onVesselSituationChange.Remove(OnVesselSituationChange);
		GameEvents.onVesselSOIChanged.Remove(OnVesselSOIChanged);
		GameEvents.onFlightReady.Remove(OnFlightReady);
		GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);
		GameEvents.onDockingComplete.Remove(OnDockingComplete);
		GameEvents.onPartCouple.Remove(OnPartCouple);
		GameEvents.onVesselChange.Remove(OnVesselSwitched);
	}

	private void OnVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data) => TryTriggerFor(data.host, data.to);
	private void OnVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> data)
	{
		var v = data.host;
		if (v == null || data.to == null) return;
		StartCoroutine(EvaluateAfterSOIChange(v));
	}
	private IEnumerator EvaluateAfterSOIChange(Vessel v)
	{
		yield return new WaitForSecondsRealtime(0.25f);
		if (v != null) TryTriggerFor(v, v.situation);
		yield return new WaitForSecondsRealtime(0.5f);
		if (v != null) TryTriggerFor(v, v.situation);
	}
	private void OnFlightReady() { StartCoroutine(DelayedInitialCheck()); }
	private void OnVesselGoOffRails(Vessel v) { if (v != null) TryTriggerFor(v, v.situation); }
	private void OnDockingComplete(GameEvents.FromToAction<Part, Part> data) { try { Vessel v = data.to?.vessel ?? data.from?.vessel ?? FlightGlobals.ActiveVessel; if (v != null) TryTriggerFor(v, Vessel.Situations.DOCKED); } catch { } }
	private void OnPartCouple(GameEvents.FromToAction<Part, Part> data) { try { Vessel v = data.to?.vessel ?? data.from?.vessel ?? FlightGlobals.ActiveVessel; if (v != null) TryTriggerFor(v, Vessel.Situations.DOCKED); } catch { } }

	private IEnumerator DelayedInitialCheck()
	{
		yield return new WaitForSecondsRealtime(0.2f);
		var v = FlightGlobals.ActiveVessel; if (v != null) TryTriggerFor(v, v.situation);
		yield return new WaitForSecondsRealtime(0.8f);
		v = FlightGlobals.ActiveVessel; if (v != null) TryTriggerFor(v, v.situation);
	}

	public void ReevaluateCurrentVessel()
	{
		try
		{
			var v = FlightGlobals.ActiveVessel;
			if (v != null) TryTriggerFor(v, v.situation);
		}
		catch { }
	}

	public static void ForceReevaluateNow()
	{
		if (!HighLogic.LoadedSceneIsFlight) return;
		try
		{
			var runner = GameObject.FindObjectOfType<AlarmRunner>();
			if (runner != null)
			{
				forceNextEvaluation = true;
				runner.ReevaluateCurrentVessel();
			}
		}
		catch { }
	}

	private void TryTriggerFor(Vessel vessel, Vessel.Situations newSituation)
	{
		try
		{
			if (vessel == null || FlightGlobals.ActiveVessel == null) return;
			if (vessel != FlightGlobals.ActiveVessel) return;
			
			// Global load cooldown - skip all alarm checks for first 20 seconds after scene load
			if (Time.realtimeSinceStartup - _sceneLoadTime < LoadCooldownSeconds)
			{
				return; // Skip all alarm processing during cooldown
			}

			// Clear caches when override changes to avoid stale-trigger suppression
			string activeOverride = KerbNote.ActiveSaveOverride; // may be null/empty
			if (!string.Equals(lastActiveSaveOverride ?? string.Empty, activeOverride ?? string.Empty, StringComparison.OrdinalIgnoreCase))
			{
				lastTriggerTimes.Clear();
				lastBodyTriggerTimes.Clear();
				lastStateByVessel.Clear();
				lastMatchedAlarmsByVessel.Clear();
				lastActiveSaveOverride = activeOverride;
			}

			var body = vessel.mainBody; if (body == null) return; string bodyName = body.bodyName ?? string.Empty;
			string vesselKey = vessel.id.ToString();
			VesselState last;
			bool stateChanged = true;
			if (lastStateByVessel.TryGetValue(vesselKey, out last))
			{
				if (string.Equals(last.Body, bodyName, StringComparison.OrdinalIgnoreCase) && last.Situation == newSituation) stateChanged = false;
			}
			// consume the one-shot force flag after capturing
			bool forced = forceNextEvaluation;
			if (forceNextEvaluation) forceNextEvaluation = false;

			lastStateByVessel[vesselKey] = new VesselState { Body = bodyName, Situation = newSituation };

			var all = AlarmManager.Alarms; if (all == null) return;
			var matches = all.Where(a => a.Enabled && string.Equals(a.BodyName, bodyName, StringComparison.OrdinalIgnoreCase) && a.Situation == newSituation).ToArray();

			// Compare with previous matches for this vessel to detect exit/enter
			List<AlarmDefinition> prevMatches;
			lastMatchedAlarmsByVessel.TryGetValue(vesselKey, out prevMatches);
			prevMatches = prevMatches ?? new List<AlarmDefinition>();

			// If there were previous matches and now there are none for some alarms -> exit condition for those alarms
			if (stateChanged)
			{
				if (prevMatches.Count >0)
				{
					// For each previously matched alarm, if it's no longer matched, process exit behavior
					foreach (var prev in prevMatches)
					{
						bool stillMatched = matches.Any(m => string.Equals(m.TabGuid, prev.TabGuid, StringComparison.OrdinalIgnoreCase) && string.Equals(m.BodyName, prev.BodyName, StringComparison.OrdinalIgnoreCase) && m.Situation == prev.Situation);
						if (!stillMatched)
						{
							// On exit from this alarm condition
							if (prev.MiniNote && prev.HideOnExit)
							{
								// Only hide MiniNote if the visible instance was spawned by an alarm
								var minis = GameObject.FindObjectsOfType<MiniNote>();
								for (int i =0; i < minis.Length; i++)
								{
									var mn = minis[i];
									if (mn != null && string.Equals(mn.TabGuid, prev.TabGuid, StringComparison.OrdinalIgnoreCase) && mn.IsVisible && mn.SpawnedByAlarm)
									{
										mn.Hide();
									}
								}
							}
						}
					}
				}
			} // <== FIX: close the stateChanged block before checking matches

			if (matches.Length ==0)
			{
				// update last matches and return
				lastMatchedAlarmsByVessel[vesselKey] = new List<AlarmDefinition>();
				return;
			}

			// If nothing changed and not forced, do not re-trigger the same condition (prevents re-fire on timewarp x1)
			if (!stateChanged && !forced)
			{
				lastMatchedAlarmsByVessel[vesselKey] = matches.ToList();
				return;
			}

			float nowTs = Time.realtimeSinceStartup;
			foreach (var grp in matches.GroupBy(m => m.TabGuid))
			{
				string bodyKey = grp.Key + "|" + bodyName.ToLowerInvariant();
				float lastBodyTs;
				if (lastBodyTriggerTimes.TryGetValue(bodyKey, out lastBodyTs))
				{
					if (nowTs - lastBodyTs < BodyDebounceSeconds) continue;
				}
				foreach (var a in grp)
				{
					string key = a.TabGuid + "|" + (a.BodyName ?? string.Empty) + "|" + a.Situation.ToString();
					float now = Time.realtimeSinceStartup;
					float lastTrig;
					if (lastTriggerTimes.TryGetValue(key, out lastTrig)) { if (now - lastTrig < TriggerThrottleSeconds) continue; }
					lastTriggerTimes[key] = now;
					if (a.StopWarp) { try { if (TimeWarp.CurrentRateIndex !=0) TimeWarp.SetRate(0, true); } catch { } }
					var host = GameObject.FindObjectsOfType<KerbNote>().FirstOrDefault();
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
					if (a.PlaySound) { SoundManager.PlayRandomKerbalVocal(); }
				}
				lastBodyTriggerTimes[grp.Key + "|" + bodyName.ToLowerInvariant()] = nowTs;
			}

			// store current matches
			lastMatchedAlarmsByVessel[vesselKey] = matches.ToList();
		}
		catch (Exception ex) { Debug.LogError("[KerbNote][AlarmRunner] Error triggering alarm: " + ex.Message); }
	}
}
