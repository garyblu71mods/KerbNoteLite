using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Checks vessel resources and triggers an alarm when any drops below its threshold
public class ResourcesAlarmRunner : MonoBehaviour
{
    // Per-resource thresholds [0..1]
    public readonly Dictionary<string, float> ThresholdByResource = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
    // Enabled flags per resource
    public readonly HashSet<string> EnabledResources = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    // Track resources currently below threshold to avoid repeat alarms
    private readonly HashSet<string> _belowThreshold = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    private float lastAlarmTs;
    private const float ThrottleSeconds = 8f;

    public void Enable() 
    { 
        enabled = true; 
        // Seed defaults only once if nothing enabled yet
        if (EnabledResources.Count == 0)
        {
            EnabledResources.Add("ElectricCharge");
            EnabledResources.Add("LiquidFuel");
            EnabledResources.Add("Oxidizer");
            EnabledResources.Add("MonoPropellant");
        }
    }
    public void Disable() { enabled = false; }

    // Ensure thresholds exist (do not force-enable)
    private void EnsureDefaults()
    {
        EnsureDefault("ElectricCharge", 0.15f);
        EnsureDefault("LiquidFuel", 0.15f);
        EnsureDefault("Oxidizer", 0.15f);
        EnsureDefault("MonoPropellant", 0.15f);
        EnsureDefault("Ablator", 0.10f);
        EnsureDefault("Ore", 0.15f);
        EnsureDefault("XenonGas", 0.20f);
    }
    private void EnsureDefault(string name, float value)
    {
        if (!ThresholdByResource.ContainsKey(name)) ThresholdByResource[name] = value;
    }

    public string[] GetCurrentVesselResourceNames()
    {
        var v = FlightGlobals.ActiveVessel; if (v == null || v.parts == null) return new string[0];
        var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var p in v.parts)
            {
                if (p == null || p.Resources == null) continue;
                foreach (PartResource r in p.Resources)
                {
                    if (r == null) continue;
                    if (!string.IsNullOrEmpty(r.resourceName)) set.Add(r.resourceName);
                }
            }
        }
        catch { }
        // Ensure threshold defaults included as editable entries
        EnsureDefaults();
        foreach (var k in ThresholdByResource.Keys) set.Add(k);
        return set.ToArray();
    }

    void Update()
    {
        if (!enabled) return;
        var v = FlightGlobals.ActiveVessel; if (v == null) return;
        if (Time.realtimeSinceStartup - lastAlarmTs < ThrottleSeconds) return;

        EnsureDefaults();
        var names = GetCurrentVesselResourceNames();
        foreach (var n in names)
        {
            if (!EnabledResources.Contains(n)) { _belowThreshold.Remove(n); continue; } // disabled resets state
            double amt, max;
            GetResourceTotals(v, n, out amt, out max);
            if (max > 0.0001)
            {
                double frac = amt / max;
                float thr;
                if (!ThresholdByResource.TryGetValue(n, out thr)) thr = 0.15f;
                bool isBelow = frac <= thr;
                bool wasBelow = _belowThreshold.Contains(n);
                if (isBelow && !wasBelow)
                {
                    TriggerAlarm(n, frac);
                    lastAlarmTs = Time.realtimeSinceStartup;
                    _belowThreshold.Add(n); // mark below to suppress repeats
                    break;
                }
                else if (!isBelow && wasBelow)
                {
                    _belowThreshold.Remove(n); // reset once resource recovers
                }
            }
            else
            {
                // No capacity -> treat as recovered
                _belowThreshold.Remove(n);
            }
        }
    }

    private static void GetResourceTotals(Vessel v, string resName, out double amount, out double maxAmount)
    {
        amount = 0; maxAmount = 0;
        try
        {
            var parts = v.parts; if (parts == null) return;
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i]; if (p == null || p.Resources == null) continue;
                var pr = p.Resources[resName];
                if (pr != null)
                {
                    amount += pr.amount;
                    maxAmount += pr.maxAmount;
                }
            }
        }
        catch { }
    }

    private void TriggerAlarm(string res, double frac)
    {
        try
        {
            ScreenMessages.PostScreenMessage($"[Global Alarm] Low {res}: {(int)(frac*100)}%", 12f, ScreenMessageStyle.UPPER_CENTER);
            SoundManager.PlayRandomKerbalVocal();
        }
        catch { }
    }
}
