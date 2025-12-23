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
    
    // Track resources completely depleted (separate from threshold alarms)
    private readonly HashSet<string> _depleted = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    // Global silence flag for all resource alarms
    public bool SilenceAlarms = false;
    
    // Communication/antenna alarm
    public bool EnableCommAlarm = false;
    public float CommSignalThreshold = 0.25f; // Default: warn below 25% signal strength
    private bool _lastCommState = true;
    private bool _commBelowThreshold = false;
    private float _lastCommCheckTime = 0f;
    private const float CommCheckInterval = 2f; // Check every 2 seconds
    
    // Global load cooldown - silence all alarms for first 20 seconds after scene load
    private float _sceneLoadTime;
    private const float LoadCooldownSeconds = 20f;

    private float lastAlarmTs;
    private const float ThrottleSeconds = 8f;

	// OPTIMIZATION: Cache vessel resource names to avoid repeated part iteration
	private string[] _cachedResourceNames;
	private float _lastResourceNamesCacheTime;
	private const float ResourceNamesCacheLifetime = 5f;

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

    // Ensure thresholds exist (do not force-enable) - now public so config can call it
    public void EnsureDefaults()
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
		// OPTIMIZATION: Return cached result if still valid
		float now = Time.realtimeSinceStartup;
		if (_cachedResourceNames != null && (now - _lastResourceNamesCacheTime) < ResourceNamesCacheLifetime)
		{
			return _cachedResourceNames;
		}
		
		var v = FlightGlobals.ActiveVessel; if (v == null || v.parts == null) return new string[0];
		var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
		try
		{
			// OPTIMIZATION: Use for loop instead of foreach
			var parts = v.parts;
			for (int i = 0; i < parts.Count; i++)
			{
				var p = parts[i];
				if (p == null || p.Resources == null) continue;
				
				// OPTIMIZATION: Direct iteration over PartResourceList
				var resources = p.Resources;
				for (int j = 0; j < resources.Count; j++)
				{
					var r = resources[j];
					if (r == null) continue;
					if (!string.IsNullOrEmpty(r.resourceName)) set.Add(r.resourceName);
				}
			}
		}
		catch { }
		// Ensure threshold defaults included as editable entries
		EnsureDefaults();
		foreach (var k in ThresholdByResource.Keys) set.Add(k);
		
		_cachedResourceNames = set.ToArray();
		_lastResourceNamesCacheTime = now;
		return _cachedResourceNames;
	}

    void Update()
    {
        if (!enabled) return;
        var v = FlightGlobals.ActiveVessel; if (v == null) return;
        
        // Global load cooldown - during first 20 seconds after scene load, alarms are SILENT (no sound)
        // but still check resources and show screen messages
        bool isInCooldown = (Time.realtimeSinceStartup - _sceneLoadTime < LoadCooldownSeconds);
        
        // During EVA, only allow MonoPropellant alarm (jetpack fuel)
        // All other resource alarms are skipped
        bool isEVA = v.isEVA;
        
        // Check communication status (skip during EVA - no comm on EVA)
        if (!isEVA && EnableCommAlarm && Time.realtimeSinceStartup - _lastCommCheckTime > CommCheckInterval)
        {
            _lastCommCheckTime = Time.realtimeSinceStartup;
            CheckCommStatus(v, isInCooldown);
        }
        
        // Global throttle: don't check resources too frequently (but check ALL resources when we do check)
        if (Time.realtimeSinceStartup - lastAlarmTs < ThrottleSeconds) return;

        EnsureDefaults();
        var names = GetCurrentVesselResourceNames();
        
        // Track if we triggered any alarm this frame (for throttling next check)
        bool anyAlarmTriggered = false;
        
        foreach (var n in names)
        {
            // During EVA: only check MonoPropellant (jetpack fuel), skip all others
            if (isEVA && !string.Equals(n, "MonoPropellant", System.StringComparison.OrdinalIgnoreCase))
            {
                // Clear tracking for non-MonoPropellant resources during EVA
                _belowThreshold.Remove(n);
                _depleted.Remove(n);
                continue;
            }
            
            if (!EnabledResources.Contains(n)) 
            { 
                _belowThreshold.Remove(n);
                _depleted.Remove(n);
                continue; 
            }
            
            double amt, max;
            GetResourceTotals(v, n, out amt, out max);
            if (max > 0.0001)
            {
                double frac = amt / max;
                float thr;
                if (!ThresholdByResource.TryGetValue(n, out thr)) thr = 0.15f;
                bool isBelow = frac <= thr;
                bool wasBelow = _belowThreshold.Contains(n);
                
                // Check if completely depleted (0% remaining)
                bool isDepleted = (amt <= 0.0001);
                bool wasDepleted = _depleted.Contains(n);
                
                if (isDepleted && !wasDepleted)
                {
                    // Resource just ran out completely
                    TriggerDepletedAlarm(n, isInCooldown);
                    _depleted.Add(n);
                    anyAlarmTriggered = true;
                    // DON'T break - check other resources too!
                }
                else if (!isDepleted && wasDepleted)
                {
                    // Resource restored from depletion
                    _depleted.Remove(n);
                }
                
                if (isBelow && !wasBelow)
                {
                    TriggerAlarm(n, frac, isInCooldown);
                    _belowThreshold.Add(n);
                    anyAlarmTriggered = true;
                    // DON'T break - check other resources too!
                }
                else if (!isBelow && wasBelow)
                {
                    _belowThreshold.Remove(n);
                }
            }
            else
            {
                // No capacity -> treat as recovered
                _belowThreshold.Remove(n);
                _depleted.Remove(n);
            }
        }
        
        // Update throttle timestamp only if we triggered at least one alarm
        if (anyAlarmTriggered)
        {
            lastAlarmTs = Time.realtimeSinceStartup;
        }
    }

	private static void GetResourceTotals(Vessel v, string resName, out double amount, out double maxAmount)
	{
		amount = 0; maxAmount = 0;
		try
		{
			// OPTIMIZATION: Use vessel.resourcePartSet if available (faster than iterating all parts)
			v.GetConnectedResourceTotals(PartResourceLibrary.Instance.GetDefinition(resName).id, out amount, out maxAmount);
		}
		catch
		{
			// Fallback to manual iteration if GetConnectedResourceTotals fails
			try
			{
				var parts = v.parts; if (parts == null) return;
				for (int i = 0; i < parts.Count; i++)
				{
					var p = parts[i]; if (p == null || p.Resources == null) continue;
					var pr = p.Resources[resName];
					if (pr != null) { amount += pr.amount; maxAmount += pr.maxAmount; }
				}
			}
			catch { }
		}
	}

    private void TriggerAlarm(string res, double frac, bool isSilent)
    {
        try
        {
            ScreenMessages.PostScreenMessage($"[Global Alarm] Low {res}: {(int)(frac*100)}%", 12f, ScreenMessageStyle.UPPER_CENTER);
            
            // Play sound only if NOT in cooldown and NOT manually silenced
            if (!isSilent && !SilenceAlarms)
            {
                SoundManager.PlayRandomKerbalVocal();
            }
        }
        catch { }
    }
    
    private void TriggerDepletedAlarm(string res, bool isSilent)
    {
        try
        {
            ScreenMessages.PostScreenMessage($"[Global Alarm] {res} depleted (0%)", 12f, ScreenMessageStyle.UPPER_CENTER);
            // Silent alarm - no sound, just message (even when not in cooldown)
        }
        catch { }
    }
    
    private void CheckCommStatus(Vessel v, bool isInCooldown)
    {
        try
        {
            bool hasComm = false;
            double signalStrength = 0.0;
            
            // Check if CommNet is available and vessel has connection
            if (CommNet.CommNetScenario.Instance != null)
            {
                var node = v.Connection;
                if (node != null)
                {
                    hasComm = node.IsConnected;
                    // Get signal strength (0.0 to 1.0)
                    signalStrength = node.SignalStrength;
                }
            }
            else
            {
                // Fallback: check if vessel has any antennas
                hasComm = v.parts.Any(p => p.Modules.Contains("ModuleDataTransmitter"));
                signalStrength = hasComm ? 1.0 : 0.0;
            }
            
            // Check if signal is below threshold
            bool isBelowThreshold = hasComm && signalStrength < CommSignalThreshold;
            bool wasBelowThreshold = _commBelowThreshold;
            
            // Trigger alarm on connection loss (transition from true to false)
            if (!hasComm && _lastCommState)
            {
                TriggerCommLostAlarm(isInCooldown);
                lastAlarmTs = Time.realtimeSinceStartup;
                _commBelowThreshold = false; // Reset threshold tracking
            }
            // Trigger recovery message on connection restored (transition from false to true)
            // Skip this message during cooldown (first 20s after load/vessel change)
            else if (hasComm && !_lastCommState)
            {
                if (!isInCooldown)
                {
                    TriggerCommRestoredMessage();
                }
                _commBelowThreshold = isBelowThreshold; // Update threshold state
            }
            // Trigger alarm when signal drops below threshold
            else if (isBelowThreshold && !wasBelowThreshold && hasComm)
            {
                TriggerCommWeakSignalAlarm(signalStrength, isInCooldown);
                lastAlarmTs = Time.realtimeSinceStartup;
                _commBelowThreshold = true;
            }
            // Reset when signal recovers above threshold
            else if (!isBelowThreshold && wasBelowThreshold && hasComm)
            {
                _commBelowThreshold = false;
            }
            
            _lastCommState = hasComm;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[ResourcesAlarmRunner] Communication check error: " + ex.Message);
        }
    }
    
    private void TriggerCommLostAlarm(bool isSilent)
    {
        try
        {
            ScreenMessages.PostScreenMessage("[Global Alarm] Communication lost!", 12f, ScreenMessageStyle.UPPER_CENTER);
            
            // Play sound only if NOT in cooldown and NOT manually silenced
            if (!isSilent && !SilenceAlarms)
            {
                SoundManager.PlayRandomKerbalVocal();
            }
        }
        catch { }
    }
    
    private void TriggerCommRestoredMessage()
    {
        try
        {
            ScreenMessages.PostScreenMessage("[Global Alarm] Communication restored", 6f, ScreenMessageStyle.UPPER_CENTER);
        }
        catch { }
    }
    
    private void TriggerCommWeakSignalAlarm(double strength, bool isSilent)
    {
        try
        {
            int percent = (int)(strength * 100);
            ScreenMessages.PostScreenMessage($"[Global Alarm] Weak signal: {percent}%", 12f, ScreenMessageStyle.UPPER_CENTER);
            
            // Play sound only if NOT in cooldown and NOT manually silenced
            if (!isSilent && !SilenceAlarms)
            {
                SoundManager.PlayRandomKerbalVocal();
            }
        }
        catch { }
    }
    
    void Start()
    {
        // Load persisted settings (including RunnerEnabled)
        ResourcesAlarmConfig.LoadInto(this);
        
        // Initialize scene load time
        _sceneLoadTime = Time.realtimeSinceStartup;
        
        // Listen for vessel switches to reset cooldown
        GameEvents.onVesselChange.Add(OnVesselChange);
    }
    
    private void OnVesselChange(Vessel v)
    {
        // Reset cooldown timer when switching vessels to prevent false alarms
        _sceneLoadTime = Time.realtimeSinceStartup;
        
        // Clear tracking states to allow fresh detection on new vessel
        _belowThreshold.Clear();
        _depleted.Clear();
        _commBelowThreshold = false;
        
        // Reset comm state to prevent false "restored" messages
        // Assume comm is available on new vessel (will be updated in next check)
        _lastCommState = true;
    }
    void OnDisable()
    {
        // Persist state on teardown so RunnerEnabled survives restart
        ResourcesAlarmConfig.SaveFrom(this);
    }
    
    void OnDestroy()
    {
        // Clean up event listener
        GameEvents.onVesselChange.Remove(OnVesselChange);
    }
}
