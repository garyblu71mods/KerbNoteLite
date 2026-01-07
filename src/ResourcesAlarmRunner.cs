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
    // Muted flags per resource (enabled but sound muted)
    public readonly HashSet<string> MutedResources = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    // Track resources currently below threshold to avoid repeat alarms
    private readonly HashSet<string> _belowThreshold = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    
    // Track resources completely depleted (separate from threshold alarms)
    private readonly HashSet<string> _depleted = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    // Global silence flag for all resource alarms
    public bool SilenceAlarms = false;
    
    // EVA MonoPropellant alarm (independent from regular MonoPropellant alarm)
    public bool EnableEvaMonoPropAlarm = true;
    public bool MuteEvaMonoPropAlarm = false; // Mute sound but keep screen message
    public float EvaMonoPropThreshold = 0.15f;
    private float _lastEvaCheckTime = 0f;
    private const float EvaCheckInterval = 2f; // Check EVA fuel every 2 seconds (faster than before)
    
    // Communication/antenna alarm
    public bool EnableCommAlarm = false;
    public bool MuteCommAlarm = false; // Mute sound but keep screen message
    public float CommSignalThreshold = 0.25f; // Default: warn below 25% signal strength
    private bool _lastCommState = true;
    private bool _commBelowThreshold = false;
    private float _lastCommCheckTime = 0f;
    private const float CommCheckInterval = 2f; // Check every 2 seconds
    
    // Delta-V alarm (tracks remaining delta-v in m/s)
    public bool EnableDeltaVAlarm = false;
    public bool MuteDeltaVAlarm = false; // Mute sound but keep screen message
    public float DeltaVThreshold = 100f; // Default: warn below 100 m/s
    private bool _deltaVBelowThreshold = false;
    private float _lastDeltaVCheckTime = 0f;
    private const float DeltaVCheckInterval = 1f; // Check every 1 second (fast response during burns)
    
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
        
        // Check Delta-V status (skip during EVA)
        if (!isEVA && EnableDeltaVAlarm && Time.realtimeSinceStartup - _lastDeltaVCheckTime > DeltaVCheckInterval)
        {
            _lastDeltaVCheckTime = Time.realtimeSinceStartup;
            CheckDeltaV(v, isInCooldown);
        }
        
        // Handle EVA MonoPropellant separately with its own checkbox and throttle
        if (isEVA && EnableEvaMonoPropAlarm && Time.realtimeSinceStartup - _lastEvaCheckTime > EvaCheckInterval)
        {
            _lastEvaCheckTime = Time.realtimeSinceStartup;
            CheckEvaMonoPropellant(v, isInCooldown);
        }
        
        // Skip regular resource checks during EVA
        if (isEVA)
        {
            return;
        }
        
        // Global throttle: don't check resources too frequently (but check ALL resources when we do check)
        if (Time.realtimeSinceStartup - lastAlarmTs < ThrottleSeconds) return;

        EnsureDefaults();
        var names = GetCurrentVesselResourceNames();
        
        // Track if we triggered any alarm this frame (for throttling next check)
        bool anyAlarmTriggered = false;
        
        foreach (var n in names)
        {
            // This code should never be reached during EVA (we return early above)
            // but keep the safety check just in case
            if (isEVA)
            {
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
    
    private void CheckEvaMonoPropellant(Vessel v, bool isInCooldown)
    {
        try
        {
            double amt = 0;
            double max = 0;
            bool found = false;
            
            if (v.isEVA)
            {
                // Method 1: KerbalEVA module (fastest and most reliable)
                if (v.rootPart != null)
                {
                    var evaModule = v.rootPart.FindModuleImplementing<KerbalEVA>();
                    if (evaModule != null)
                    {
                        try
                        {
                            var fuelProperty = evaModule.GetType().GetProperty("Fuel");
                            if (fuelProperty != null)
                            {
                                var fuelValue = fuelProperty.GetValue(evaModule, null);
                                if (fuelValue != null)
                                {
                                    amt = System.Convert.ToDouble(fuelValue);
                                    max = 5.0;
                                    found = true;
                                }
                            }
                        }
                        catch { }
                    }
                }
                
                // Method 2: Direct part iteration (fallback)
                if (!found && v.parts != null && v.parts.Count > 0)
                {
                    foreach (var part in v.parts)
                    {
                        if (part?.Resources != null && part.Resources.Count > 0)
                        {
                            foreach (PartResource res in part.Resources)
                            {
                                if (res.resourceName == "EVA Propellant" || res.resourceName == "MonoPropellant")
                                {
                                    amt = res.amount;
                                    max = res.maxAmount;
                                    found = true;
                                    break;
                                }
                            }
                            if (found) break;
                        }
                    }
                }
                
                // Method 3: ProtoVessel (last resort, slowest)
                if (!found && v.protoVessel?.protoPartSnapshots != null)
                {
                    foreach (var protoPart in v.protoVessel.protoPartSnapshots)
                    {
                        if (protoPart.resources != null)
                        {
                            foreach (var protoRes in protoPart.resources)
                            {
                                if (protoRes.resourceName == "EVA Propellant" || protoRes.resourceName == "MonoPropellant")
                                {
                                    amt = protoRes.amount;
                                    max = protoRes.maxAmount;
                                    found = true;
                                    break;
                                }
                            }
                            if (found) break;
                        }
                    }
                }
            }
            
            if (max > 0.0001)
            {
                double frac = amt / max;
                bool isBelow = frac <= EvaMonoPropThreshold;
                bool wasBelow = _belowThreshold.Contains("EVA_MonoPropellant");
                
                bool isDepleted = (amt <= 0.0001);
                bool wasDepleted = _depleted.Contains("EVA_MonoPropellant");
                
                if (isDepleted && !wasDepleted)
                {
                    TriggerDepletedAlarm("EVA Jetpack Fuel", isInCooldown);
                    _depleted.Add("EVA_MonoPropellant");
                }
                else if (!isDepleted && wasDepleted)
                {
                    _depleted.Remove("EVA_MonoPropellant");
                }
                
                if (isBelow && !wasBelow)
                {
                    TriggerEvaAlarm("EVA Jetpack Fuel", frac, isInCooldown);
                    _belowThreshold.Add("EVA_MonoPropellant");
                }
                else if (!isBelow && wasBelow)
                {
                    _belowThreshold.Remove("EVA_MonoPropellant");
                }
            }
            else
            {
                _belowThreshold.Remove("EVA_MonoPropellant");
                _depleted.Remove("EVA_MonoPropellant");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ResourcesAlarmRunner] EVA check error: {ex.Message}");
        }
    }
    
    private void CheckDeltaV(Vessel v, bool isInCooldown)
    {
        try
        {
            // Get vessel's Delta-V from VesselDeltaV component (stock KSP feature since 1.9+)
            double currentDeltaV = 0.0;
            
            // Try to access VesselDeltaV (available in KSP 1.9+)
            if (v.VesselDeltaV != null)
            {
                // Get total delta-v for all stages
                currentDeltaV = v.VesselDeltaV.TotalDeltaVActual;
            }
            
            // Check if below threshold
            bool isBelowThreshold = currentDeltaV > 0 && currentDeltaV <= DeltaVThreshold;
            bool wasBelowThreshold = _deltaVBelowThreshold;
            
            // Trigger alarm when dropping below threshold
            if (isBelowThreshold && !wasBelowThreshold)
            {
                TriggerDeltaVAlarm(currentDeltaV, isInCooldown);
                _deltaVBelowThreshold = true;
            }
            // Clear when recovering above threshold
            else if (!isBelowThreshold && wasBelowThreshold)
            {
                _deltaVBelowThreshold = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ResourcesAlarmRunner] Delta-V check error: {ex.Message}");
        }
    }
    
    private void TriggerDeltaVAlarm(double deltaV, bool isSilent)
    {
        try
        {
            ScreenMessages.PostScreenMessage($"[Global Alarm] Low Delta-V: {deltaV:F0} m/s remaining", 12f, ScreenMessageStyle.UPPER_CENTER);
            
            // Play sound only if NOT in cooldown, NOT globally silenced, and NOT DeltaV-muted
            if (!isSilent && !SilenceAlarms && !MuteDeltaVAlarm)
            {
                SoundManager.PlayRandomKerbalVocal();
            }
        }
        catch { }
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
            
            // Check if this specific resource is muted
            bool isResourceMuted = MutedResources.Contains(res);
            
            // Play sound only if NOT in cooldown, NOT globally silenced, and NOT resource-muted
            if (!isSilent && !SilenceAlarms && !isResourceMuted)
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
    
    private void TriggerEvaAlarm(string res, double frac, bool isSilent)
    {
        try
        {
            ScreenMessages.PostScreenMessage($"[Global Alarm] Low {res}: {(int)(frac*100)}%", 12f, ScreenMessageStyle.UPPER_CENTER);
            
            // Play sound only if NOT in cooldown, NOT globally silenced, and NOT EVA-muted
            if (!isSilent && !SilenceAlarms && !MuteEvaMonoPropAlarm)
            {
                SoundManager.PlayRandomKerbalVocal();
            }
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
            
            // Play sound only if NOT in cooldown, NOT globally silenced, and NOT comm-muted
            if (!isSilent && !SilenceAlarms && !MuteCommAlarm)
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
            
            // Play sound only if NOT in cooldown, NOT globally silenced, and NOT comm-muted
            if (!isSilent && !SilenceAlarms && !MuteCommAlarm)
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
        _deltaVBelowThreshold = false; // Reset Delta-V tracking
        
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
