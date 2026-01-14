using UnityEngine;

public class TerrainAlarmRunner : MonoBehaviour
{
    public float AltitudeAGL = 750f;
    public float DescentSpeed = -30f;

    public bool EnableGearAlarm = true;
    public float GearAlarmAGL = 200f;
    public float GearAlarmMaxSpeed = 200f;
    public float GearAlarmMaxRollDegrees = 45f;

    // Terrain ahead (time-to-impact)
    public bool EnableTerrainAhead = false;
    public float TerrainAheadMarginMeters = 0f;  // Default: 0 meters margin
    public float TerrainAheadMaxTime = 6f;   // seconds to look ahead
    public float TerrainAheadStep = 0.25f;   // seconds per sample
    public float TerrainAheadMinSpeed = 20f; // m/s

    // Suppress terrain warnings when configured for landing
    public float LandingSuppressMaxSpeed = 150f;
    public float LandingSuppressMaxAGL = 300f;

    public bool EnableAltitudeCallouts = false;
    
    // Separate toggle for "Landed" callout (can be annoying on repeated landings)
    public bool EnableLandedCallout = true;

    public bool EnableSinkRate = true;

    // Base terrain pull-up warning toggle
    public bool EnableTerrainBase = true;

    // Sink rate callout
    public float SinkRateAGL = 70f;
    public float SinkRateMinDescent = 7f;

    // Stall Warning (Loss of airspeed / energy decay detection)
    public bool EnableStallWarning = false;
    public enum StallMode { Auto, Manual }
    public StallMode StallWarningMode = StallMode.Auto;
    public float StallMinHorizontalSpeed = 50f;      // Manual mode: min safe horizontal speed (m/s)
    public float StallAngleThreshold = 45f;          // Auto mode: angle between nose and velocity vector (degrees, 0-90)
    public float StallMinAGL = 100f;                 // Don't alarm below this altitude (prevents landing warnings)
    public float StallMaxAltitudeASL = 25000f;       // Maximum altitude ASL for stall warning (atmosphere boundary)
    public float StallMinHorizontalSpeedAuto = 20f;  // Auto mode: minimum speed to consider (avoid false alarms when slow)

    // Filter: aircraft only vs all vessels
    public bool AircraftOnly = false;

    // Volume control for all terrain alarm sounds (0.0 to 1.0)
    public float Volume = 1.0f;

    private float lastAlarmTs;
    private const float ThrottleSeconds = 6f;

    private bool _wasGearAlarm;

    private float? _lastAgl;
    private float _lastAglTs;

    private bool _holdBase;
    private bool _holdAhead;
    private bool _holdGear;

    private float _lastGearBeepTs;
    private const float GearBeepCooldown = 1.0f;

    private float _lastSinkRateTs;
    private const float SinkRateCooldown = 1.6f;
    
    private float _lastStallWarningTs;
    private const float StallWarningCooldown = 2.5f;

    // Callouts
    private float _lastCalloutTs;
    private const float CalloutCooldown = 0.6f;
    private static readonly int[] CalloutThresholds = { 200, 100, 90, 80, 70, 60, 50, 40, 30, 20, 10 };
    private float? _prevCalloutAgl;
    private readonly System.Collections.Generic.Dictionary<int, float> _lastCalloutByTh = new System.Collections.Generic.Dictionary<int, float>();

    // Touchdown callout (1s confirm)
    private bool _touchdownPending;
    private float _touchdownPendingTs;
    private bool _touchdownPlayed;
    
    // Track previous landed state to detect transitions (not initial state)
    private bool? _wasTouchingLastFrame;
    
    // Track if we've seen vessel airborne at least once after loading (to prevent false landing on load)
    private bool _hasBeenAirborneAfterLoad;
    
    // Track maximum AGL reached after load (to detect if vessel ever actually flew)
    private float _maxAglAfterLoad;

    private float _runnerEnabledTs;

    private const float TouchdownRearmAglMeters = 10f;
    private const float LoadPhysicsSettleTime = 20f; // Ignore first landing within this time if never airborne
    private const float MinAglToConfirmFlight = 5f; // Vessel must reach at least 5m AGL to confirm it was flying

    // Cache for expensive aircraft type check
    private bool? _cachedIsAircraft;
    private float _lastAircraftCheckTime;
    private const float AircraftCheckCacheTime = 2f; // Cache for 2 seconds

    private void ResetCalloutSequence()
    {
        _prevCalloutAgl = null;
        _lastCalloutTs = 0f;
        _lastCalloutByTh.Clear();
    }

    public void Enable() { enabled = true; }
    public void Disable() { enabled = false; ReleaseAll(); }

    void Start()
    {
        // Load persisted settings (including RunnerEnabled). This is safe even if the component starts disabled.
        TerrainAlarmConfig.LoadInto(this);
        ResetCalloutSequence();
        _runnerEnabledTs = Time.realtimeSinceStartup;
        
        // Listen for vessel switches to reset cooldown
        GameEvents.onVesselChange.Add(OnVesselChange);
    }
    
    private void OnVesselChange(Vessel v)
    {
        // Reset cooldown timer when switching vessels to prevent false alarms
        _runnerEnabledTs = Time.realtimeSinceStartup;
        
        // Reset all tracking states for fresh detection on new vessel
        ResetCalloutSequence();
        _touchdownPlayed = false;
        _touchdownPending = false;
        _wasTouchingLastFrame = null;
        _hasBeenAirborneAfterLoad = false;
        _maxAglAfterLoad = 0f;
        _lastAgl = null;
        _lastStallWarningTs = 0f;
        
        // Invalidate aircraft cache on vessel change
        _cachedIsAircraft = null;
        
        // Release any active alarms
        ReleaseAll();
    }

    void OnEnable()
    {
        // Do not reload config here; UI/LoadInto has already set state. Reloading would override enabled flag after user re-enables.
        ResetCalloutSequence();
        _runnerEnabledTs = Time.realtimeSinceStartup;
    }

    void OnDisable()
    {
        ReleaseAll();
        // Persist state on teardown (game quit / scene unload) so RunnerEnabled survives restart.
        TerrainAlarmConfig.SaveFrom(this);
        ResetCalloutSequence();
    }
    
    void OnDestroy()
    {
        // Clean up event listener
        GameEvents.onVesselChange.Remove(OnVesselChange);
        ReleaseAll();
    }

    private void ReleaseAll()
    {
        ReleaseHold(ref _holdGear);
        ReleaseHold(ref _holdBase);
        ReleaseHold(ref _holdAhead);
    }

    private void ReleaseHold(ref bool holdFlag)
    {
        if (holdFlag)
        {
            holdFlag = false;
            SoundManager.PullUpRelease();
        }
    }

    void Update()
    {
        if (!enabled) return;
        var v = FlightGlobals.ActiveVessel;
        if (v == null || v.mainBody == null) { ReleaseAll(); return; }
        
        // Global load cooldown - skip ALL terrain alarms for first 20 seconds after scene load
        // This prevents false alarms from physics settling after vessel load
        if (Time.realtimeSinceStartup - _runnerEnabledTs < LoadPhysicsSettleTime)
        {
            return; // Skip all terrain alarm processing during cooldown
        }
        
        // Skip all terrain alarms during EVA (Kerbals on spacewalk/jetpack)
        if (v.isEVA)
        {
            ReleaseAll(); // Release any active alarms
            _cachedIsAircraft = null; // Invalidate cache
            return; // Skip all terrain alarm processing during EVA
        }

        // Apply volume to SoundManager for all terrain alarm sounds
        SoundManager.SetTerrainVolume(Volume);

        float agl = 0f;
        try { agl = (float)v.radarAltitude; } catch { agl = 0f; }

        // If we already played touchdown, only re-arm after climbing above 10m AGL
        if (_touchdownPlayed && agl > TouchdownRearmAglMeters)
        {
            _touchdownPlayed = false;
            _touchdownPending = false;
        }

        // Touchdown detection: when we first transition into landed/splashed, wait 1s and confirm.
        bool isTouching = false;
        try { isTouching = v.LandedOrSplashed || v.situation == Vessel.Situations.LANDED || v.situation == Vessel.Situations.SPLASHED; } catch { }

        // Initialize tracking on first frame
        if (!_wasTouchingLastFrame.HasValue)
        {
            _wasTouchingLastFrame = isTouching;
            _maxAglAfterLoad = 0f; // Start tracking max AGL
            // If vessel is already touching on first frame (loaded on ground), mark touchdown as already played
            // to prevent any "Landed" callout during the initial physics settling period
            if (isTouching)
            {
                _touchdownPlayed = true;
                _hasBeenAirborneAfterLoad = false; // Not airborne yet
            }
            else
            {
                _hasBeenAirborneAfterLoad = true; // Started airborne, normal flight
            }
        }
        
        // Track maximum AGL reached (to confirm vessel actually flew vs just physics settling)
        if (agl > _maxAglAfterLoad)
        {
            _maxAglAfterLoad = agl;
        }

        if (!isTouching)
        {
            // When airborne: cancel pending touchdown, but don't reset _touchdownPlayed
            // (it only resets after climbing above 10m AGL, handled above)
            _touchdownPending = false;
            _wasTouchingLastFrame = false;
            
            // Mark that we've been airborne (vessel is confirmed flying, not just physics settling)
            // Only if we've reached significant altitude (5m+)
            if (!_hasBeenAirborneAfterLoad && _maxAglAfterLoad >= MinAglToConfirmFlight)
            {
                _hasBeenAirborneAfterLoad = true;
            }
        }
        else
        {
            // Only trigger touchdown callout if we TRANSITION from not-touching to touching
            // (prevents callout when loading a vessel that's already on the ground)
            bool justTouched = !_wasTouchingLastFrame.Value && isTouching;
            
            // Additional filter: don't play Landed callout for EVA kerbals
            bool isEVA = false;
            try { isEVA = (v.vesselType == VesselType.EVA); } catch { }
            
            // Ignore landing if:
            // 1. We've never been airborne (confirmed by reaching 5m+ AGL) AND
            // 2. We're within load settling time (20s) AND
            // 3. We never reached 5m AGL (stayed on/near ground the whole time)
            bool isPhysicsSettling = !_hasBeenAirborneAfterLoad && 
                                     (Time.realtimeSinceStartup - _runnerEnabledTs) < LoadPhysicsSettleTime &&
                                     _maxAglAfterLoad < MinAglToConfirmFlight;
            
            // Only process touchdown if NOT already played AND not EVA AND not physics settling
            if (!_touchdownPlayed && !isEVA && !isPhysicsSettling)
            {
                // Start pending timer only on actual transition (justTouched)
                if (!_touchdownPending && justTouched)
                {
                    _touchdownPending = true;
                    _touchdownPendingTs = Time.realtimeSinceStartup;
                }
                // Confirm after delay (longer for water landing stability)
                else if (_touchdownPending && Time.realtimeSinceStartup - _touchdownPendingTs >= 2.0f)
                {
                    // Confirm still touching after delay (prevents bounce false positives and water surface oscillation)
                    bool stillTouching = false;
                    try { stillTouching = v.LandedOrSplashed || v.situation == Vessel.Situations.LANDED || v.situation == Vessel.Situations.SPLASHED; } catch { }
                    if (stillTouching)
                    {
                        // Do not play immediately after vessel load/scene start (15s cooldown)
                        bool pastLoadCooldown = (Time.realtimeSinceStartup - _runnerEnabledTs) >= 15.0f;
                        if (pastLoadCooldown)
                        {
                            // Always show screen message
                            try
                            {
                                ScreenMessages.PostScreenMessage("Landed", 2f, ScreenMessageStyle.UPPER_CENTER);
                            }
                            catch { }
                            
                            // Play sound only if both callouts AND landed callout are enabled
                            if (EnableAltitudeCallouts && EnableLandedCallout)
                            {
                                SoundManager.PlayLandedCallout(Volume);
                            }
                        }
                        // Mark as played - this prevents repeats until vessel climbs above 10m AGL
                        _touchdownPlayed = true;
                    }
                    // Always clear pending after delay check (whether played or not)
                    _touchdownPending = false;
                }
            }
            else
            {
                // If already played or EVA, ensure pending is cleared
                _touchdownPending = false;
            }
            
            _wasTouchingLastFrame = true;

            // When touching ground/water we do not run terrain warnings.
            ReleaseAll();
            ResetCalloutSequence();
            return;
        }

        float vsAsl = (float)v.verticalSpeed;

        // AGL rate (used by gear alarm only)
        float now = Time.realtimeSinceStartup;
        float aglRate = 0f;
        if (_lastAgl.HasValue)
        {
            float dt = now - _lastAglTs;
            if (dt > 0.001f) aglRate = (agl - _lastAgl.Value) / dt;
        }
        _lastAgl = agl;
        _lastAglTs = now;

        // Read gear + speed once
        bool gearDeployed = false;
        try { gearDeployed = v.ActionGroups != null && v.ActionGroups[KSPActionGroup.Gear]; } catch { }
        double spd = 0;
        try { spd = v.srfSpeed; } catch { spd = v.horizontalSrfSpeed; }

        // Suppression: when gear is out, slow, and low, do not warn (except gear-alarm itself)
        bool suppressTerrain = gearDeployed && (spd < LandingSuppressMaxSpeed) && (agl < LandingSuppressMaxAGL);

        // Altitude callouts (pure crossing): ALWAYS run for all vessels (ignore AircraftOnly filter)
        // Screen messages always shown, sound only if enabled
        if (_prevCalloutAgl.HasValue)
        {
            float prev = _prevCalloutAgl.Value;

            // Check from lowest->highest so we announce the most relevant threshold if we skip multiple.
            for (int idx = CalloutThresholds.Length - 1; idx >= 0; idx--)
            {
                int th = CalloutThresholds[idx];
                if (prev > th && agl <= th)
                {
                    float lastThTs;
                    _lastCalloutByTh.TryGetValue(th, out lastThTs);
                    if (Time.realtimeSinceStartup - lastThTs > CalloutCooldown)
                    {
                        _lastCalloutByTh[th] = Time.realtimeSinceStartup;
                        _lastCalloutTs = Time.realtimeSinceStartup;
                        
                        // Always show screen message
                        try
                        {
                            ScreenMessages.PostScreenMessage($"{th}", 1.5f, ScreenMessageStyle.UPPER_CENTER);
                        }
                        catch { }
                        
                        // Play sound only if enabled
                        if (EnableAltitudeCallouts)
                        {
                            SoundManager.PlayAltitudeCallout(th, Volume);
                        }
                    }
                    break;
                }
            }
        }

        // Always update previous AGL for tracking
        _prevCalloutAgl = agl;

        // Aircraft filter: skip terrain alarms (Pull Up, Gear, Terrain Ahead) if configured and vessel is not aircraft
        // Callouts above run regardless of filter.
        if (AircraftOnly)
        {
            // Cache aircraft type check to avoid expensive part iteration every frame
            float nowTime = Time.realtimeSinceStartup;
            if (!_cachedIsAircraft.HasValue || (nowTime - _lastAircraftCheckTime) > AircraftCheckCacheTime)
            {
                _cachedIsAircraft = IsAircraftType(v);
                _lastAircraftCheckTime = nowTime;
            }
            
            if (!_cachedIsAircraft.Value)
            {
                ReleaseAll();
                // Don't reset callout sequence here - callouts should continue
                return;
            }
        }

        // Additional suppression: never play Pull_Up terrain warnings when gear is deployed
        bool suppressPullUp = gearDeployed;

        // Roll suppression for gear alarm (ignore when banked)
        float rollDeg = 0f;
        try
        {
            // Compute signed bank angle around the vessel forward axis relative to surface up.
            // (+) right wing down, (-) left wing down.
            Vector3 up = (v.transform.position - v.mainBody.position).normalized;
            Vector3 right = v.transform.right;
            Vector3 fwd = v.transform.forward;
            // remove forward component so we measure pure sideways tilt relative to horizon
            Vector3 rightProj = (right - Vector3.Dot(right, fwd) * fwd).normalized;
            float sin = Vector3.Dot(Vector3.Cross(up, rightProj), fwd);
            float cos = Vector3.Dot(rightProj, Vector3.Cross(fwd, up));
            rollDeg = Mathf.Atan2(sin, cos) * Mathf.Rad2Deg;
        }
        catch
        {
            try
            {
                rollDeg = v.transform.rotation.eulerAngles.z;
                if (rollDeg > 180f) rollDeg -= 360f;
            }
            catch { rollDeg = 0f; }
        }
        bool suppressGear = Mathf.Abs(rollDeg) > GearAlarmMaxRollDegrees;

        bool gearCond = false;
        bool gearCondForMessage = false; // Always check condition for screen message
        
        // Check gear alarm condition regardless of EnableGearAlarm (for screen messages)
        if (!suppressGear)
        {
            // Use actual vertical speed for descent detection (more reliable than radar AGL rate).
            bool descending = vsAsl < -0.1f;
            gearCondForMessage = descending && (agl < GearAlarmAGL) && (spd < GearAlarmMaxSpeed) && !gearDeployed;
            
            // Sound only plays if enabled
            gearCond = EnableGearAlarm && gearCondForMessage;
        }

        // Gear warning: do not share global throttle with terrain alarms
        if (gearCondForMessage) // Show message always when condition met
        {
            // play gear warning periodically while condition holds
            if (Time.realtimeSinceStartup - _lastGearBeepTs > 2.0f)
            {
                _lastGearBeepTs = Time.realtimeSinceStartup;
                
                // Play sound only if enabled
                if (EnableGearAlarm)
                {
                    SoundManager.PlayGearBeep(Volume);
                }
                
                ScreenMessages.PostScreenMessage($"Gear! AGL={agl:F0}m", 2.5f, ScreenMessageStyle.UPPER_CENTER);
            }
        }
        _wasGearAlarm = gearCond;

        // Check base terrain condition for messages (always) and sounds (if enabled)
        bool baseCondForMessage = !suppressTerrain && !suppressPullUp && (agl < AltitudeAGL && vsAsl < DescentSpeed);
        bool baseCond = EnableTerrainBase && baseCondForMessage;

        bool aheadCond = false;
        bool aheadCondForMessage = false;
        string aheadMsg = null;
        if (!suppressTerrain && !suppressPullUp)
        {
            if (spd >= TerrainAheadMinSpeed)
            {
                aheadCondForMessage = TryGetTerrainAheadImpactTTI(v, out float impactTime, out float impactDist);
                if (aheadCondForMessage)
                {
                    aheadMsg = $"Terrain ahead! Impact in {impactTime:F1}s ({impactDist:F0}m)";
                    aheadCond = EnableTerrainAhead && aheadCondForMessage; // Sound only if enabled
                }
            }
        }

        // Holds: only for Pull_Up alarms (sound controlled by Enable flags)
        UpdateHold(ref _holdBase, baseCond);
        UpdateHold(ref _holdAhead, aheadCond);

        // Sink rate callout: gear deployed, low AGL, high descent rate
        // Keep this independent of terrain message throttle.
        bool sinkRateCond = gearDeployed && agl < SinkRateAGL && vsAsl < -Mathf.Abs(SinkRateMinDescent);
        if (sinkRateCond)
        {
            if (Time.realtimeSinceStartup - _lastSinkRateTs > SinkRateCooldown)
            {
                _lastSinkRateTs = Time.realtimeSinceStartup;
                // Play sound only if enabled
                if (EnableSinkRate)
                {
                    SoundManager.PlaySinkRate(Volume);
                }
                // Screen message always shown when condition met
                ScreenMessages.PostScreenMessage("Sink Rate!", 2f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        // Stall Warning: detects loss of lift by comparing nose direction vs actual flight direction
        // ONLY works in atmosphere and below max altitude
        if (EnableStallWarning && !suppressPullUp && !suppressTerrain)
        {
            bool stallCondition = false;
            
            // Check if we're in atmosphere and below max altitude
            bool inAtmosphere = false;
            double altitudeASL = 0;
            try 
            { 
                altitudeASL = v.altitude; 
                // Check if vessel is in atmosphere (dynamic pressure > 0 means we have air resistance)
                inAtmosphere = v.atmDensity > 0.001; // Small threshold to account for thin atmosphere
            } 
            catch { }
            
            bool withinAltitudeRange = (altitudeASL <= StallMaxAltitudeASL);
            
            // Only check stall if in atmosphere and within altitude range
            if (inAtmosphere && withinAltitudeRange)
            {
                // Manual mode: simple speed check
                if (StallWarningMode == StallMode.Manual)
                {
                    stallCondition = (spd < StallMinHorizontalSpeed) && (agl > StallMinAGL);
                }
                // Auto mode: angle between nose direction and velocity vector
                else
                {
                    bool aboveMinAltitude = (agl > StallMinAGL);
                    
                    // Get velocity vector (direction vessel is actually moving)
                    Vector3 velocityVector = Vector3.zero;
                    try { velocityVector = v.srf_velocity; } catch { }
                    
                    // Get horizontal speed (to filter out slow taxi)
                    double horizontalSpeed = 0;
                    try { horizontalSpeed = v.horizontalSrfSpeed; } catch { }
                    bool movingForward = horizontalSpeed > StallMinHorizontalSpeedAuto;
                    
                    // Calculate angle between nose and velocity only if moving forward at reasonable speed
                    bool angleAlarm = false;
                    if (movingForward && aboveMinAltitude && velocityVector.sqrMagnitude > 0.1f)
                    {
                        try
                        {
                            // Get vessel's forward direction (nose)
                            Vector3 vesselForward = v.transform.up; // In KSP, "up" is vessel's forward axis
                            
                            // Calculate angle between nose direction and velocity vector
                            float angle = Vector3.Angle(vesselForward, velocityVector);
                            
                            // Clamp to 0-90 degrees (we only care about deviation, not if it's up/down)
                            angle = Mathf.Clamp(angle, 0f, 90f);
                            
                            // Trigger alarm if angle exceeds threshold
                            angleAlarm = angle >= StallAngleThreshold;
                        }
                        catch { }
                    }
                    
                    stallCondition = angleAlarm;
                }
            }

            // Play stall warning with cooldown
            if (stallCondition)
            {
                if (Time.realtimeSinceStartup - _lastStallWarningTs > StallWarningCooldown)
                {
                    _lastStallWarningTs = Time.realtimeSinceStartup;
                    SoundManager.PlayStallWarning(Volume);
                    ScreenMessages.PostScreenMessage("Stall Warning!", 2f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

        // Terrain messages throttled (screen messages always shown when condition met)
        bool allowTerrainMsg = (Time.realtimeSinceStartup - lastAlarmTs) >= ThrottleSeconds;

        // Show messages regardless of Enable flags (but sound is controlled by them)
        if (allowTerrainMsg && baseCondForMessage)
        {
            ScreenMessages.PostScreenMessage($"Terrain warning! AGL={agl:F0}m, VSpeed={vsAsl:F0} m/s", 4f, ScreenMessageStyle.UPPER_CENTER);
            lastAlarmTs = Time.realtimeSinceStartup;
        }
        else if (allowTerrainMsg && aheadCondForMessage && !string.IsNullOrEmpty(aheadMsg))
        {
            ScreenMessages.PostScreenMessage(aheadMsg, 4f, ScreenMessageStyle.UPPER_CENTER);
            lastAlarmTs = Time.realtimeSinceStartup;
        }
    }

    private void UpdateHold(ref bool holdFlag, bool cond)
    {
        if (cond)
        {
            if (!holdFlag)
            {
                holdFlag = true;
                SoundManager.PullUpAcquire();
            }
        }
        else
        {
            if (holdFlag)
            {
                holdFlag = false;
                SoundManager.PullUpRelease();
            }
        }
    }

    // Predict straight-line motion for TerrainAheadMaxTime and report earliest time where vessel altitude <= terrain+margin.
    private bool TryGetTerrainAheadImpactTTI(Vessel v, out float impactTime, out float impactDist)
    {
        impactTime = 0f;
        impactDist = 0f;
        try
        {
            var body = v.mainBody;
            if (body == null) return false;

            float maxTime = Mathf.Clamp(TerrainAheadMaxTime, 0.5f, 30f);
            float step = Mathf.Clamp(TerrainAheadStep, 0.05f, 2f);
            float margin = Mathf.Clamp(TerrainAheadMarginMeters, 0f, 5000f);

            Vector3d vel = v.srf_velocity;
            if (vel.sqrMagnitude < 1.0) return false;

            // Use current surface-relative velocity as straight-line predictor
            Vector3d origin = v.transform.position;

            for (float t = step; t <= maxTime + 0.0001f; t += step)
            {
                Vector3d pos = origin + vel * t;

                double lat = body.GetLatitude(pos);
                double lon = body.GetLongitude(pos);
                double terrainASL = body.TerrainAltitude(lat, lon);

                // Vessel altitude at predicted point
                double vesselASL = body.GetAltitude(pos);

                if (vesselASL <= terrainASL + margin)
                {
                    impactTime = t;
                    impactDist = (float)(vel.magnitude * t);
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    // OPTIMIZATION: Cache aircraft type detection results
    // Check if vessel is aircraft-type: first check VesselType, then use scoring system
    private bool IsAircraftType(Vessel v)
    {
        if (v == null || v.parts == null) return false;
        
        // Definitive check: if vessel type is set to Plane, it's 100% aircraft
        try
        {
            if (v.vesselType == VesselType.Plane) return true;
        }
        catch { }
        
        // Fallback: scoring system if type is not explicitly Plane
        try
        {
            int score = 0;
            bool hasWings = false;
            bool hasControlSurfaces = false;
            bool hasAirBreathingEngine = false;
            bool hasPropeller = false;
            bool hasAircraftCockpit = false;
            bool hasWheelLandingGear = false;
            bool hasIntakeAir = false;

            // OPTIMIZATION: Use for loop instead of foreach to reduce allocator overhead
            var parts = v.parts;
            for (int idx = 0; idx < parts.Count; idx++)
            {
                var part = parts[idx];
                if (part == null) continue;
                var modules = part.Modules;
                if (modules == null) continue;
                
                // Early exit optimization: if we've already hit the threshold (3 points), stop checking
                if (score >= 3) return true;
                
                // Wings (strong signal: +2)
                if (!hasWings && modules.Contains("ModuleLiftingSurface"))
                {
                    hasWings = true;
                    score += 2;
                    if (score >= 3) return true;
                }
                
                // Aircraft cockpit (strong signal: +2, higher priority than control surfaces)
                if (!hasAircraftCockpit && modules.Contains("ModuleCommand"))
                {
                    string nameLower = (part.name ?? "").ToLower();
                    string titleLower = (part.partInfo?.title ?? "").ToLower();
                    if (nameLower.Contains("cockpit") || nameLower.Contains("aircraft") || 
                        titleLower.Contains("cockpit") || titleLower.Contains("aircraft"))
                    {
                        hasAircraftCockpit = true;
                        score += 2;
                        if (score >= 3) return true;
                    }
                }
                
                // Control surfaces (medium signal: +1, since rockets can have them too)
                if (!hasControlSurfaces && modules.Contains("ModuleControlSurface"))
                {
                    hasControlSurfaces = true;
                    score += 1;
                }
                
                // Air-breathing jet engines (medium signal: +1)
                if (!hasAirBreathingEngine)
                {
                    if (modules.Contains("ModuleEngines"))
                    {
                        var engine = part.FindModuleImplementing<ModuleEngines>();
                        if (engine != null && engine.atmChangeFlow)
                        {
                            hasAirBreathingEngine = true;
                            score += 1;
                        }
                    }
                    else if (modules.Contains("ModuleEnginesFX"))
                    {
                        var engineFX = part.FindModuleImplementing<ModuleEnginesFX>();
                        if (engineFX != null && engineFX.atmChangeFlow)
                        {
                            hasAirBreathingEngine = true;
                            score += 1;
                        }
                    }
                }
                
                // Propeller engines (medium signal: +1)
                if (!hasPropeller && part.name != null)
                {
                    string nameLower = part.name.ToLower();
                    if (nameLower.Contains("propeller") || nameLower.Contains("prop") && nameLower.Contains("engine"))
                    {
                        hasPropeller = true;
                        score += 1;
                    }
                }
                
                // Wheel landing gear (weak signal: +1, conditional)
                if (!hasWheelLandingGear && modules.Contains("ModuleWheelBase"))
                {
                    // Check if it's retractable gear (aircraft-style)
                    if (modules.Contains("ModuleWheelDeployment"))
                    {
                        hasWheelLandingGear = true;
                        // Only add score if we already have other aircraft indicators
                        if (hasWings || hasControlSurfaces || hasAirBreathingEngine || hasAircraftCockpit)
                            score += 1;
                    }
                }
                
                // Air intakes (weak signal: counted but low weight)
                if (!hasIntakeAir && modules.Contains("ModuleResourceIntake"))
                {
                    var intake = part.FindModuleImplementing<ModuleResourceIntake>();
                    if (intake != null && intake.resourceName == "IntakeAir")
                    {
                        hasIntakeAir = true;
                        // Only valuable if combined with other aircraft parts
                        if (hasWings || hasAirBreathingEngine)
                            score += 1;
                    }
                }
            }
            
            // Threshold: need at least 3 points to qualify as aircraft
            return score >= 3;
        }
        catch { }
        return false;
    }
}
