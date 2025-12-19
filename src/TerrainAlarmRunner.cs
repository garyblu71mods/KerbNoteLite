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
    public float TerrainAheadMarginMeters = 300f;
    public float TerrainAheadMaxTime = 6f;   // seconds to look ahead
    public float TerrainAheadStep = 0.25f;   // seconds per sample
    public float TerrainAheadMinSpeed = 20f; // m/s

    // Suppress terrain warnings when configured for landing
    public float LandingSuppressMaxSpeed = 150f;
    public float LandingSuppressMaxAGL = 300f;

    public bool EnableAltitudeCallouts = false;

    public bool EnableSinkRate = true;

    // Base terrain pull-up warning toggle
    public bool EnableTerrainBase = true;

    // Sink rate callout
    public float SinkRateAGL = 70f;
    public float SinkRateMinDescent = 7f;

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

    // Callouts
    private float _lastCalloutTs;
    private const float CalloutCooldown = 0.6f;
    private static readonly int[] CalloutThresholds = { 50, 40, 30, 20, 10 };
    private float? _prevCalloutAgl;
    private readonly System.Collections.Generic.Dictionary<int, float> _lastCalloutByTh = new System.Collections.Generic.Dictionary<int, float>();

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
    }

    void OnEnable()
    {
        // Do not reload config here; UI/LoadInto has already set state. Reloading would override enabled flag after user re-enables.
        ResetCalloutSequence();
    }

    void OnDisable()
    {
        ReleaseAll();
        // Persist state on teardown (game quit / scene unload) so RunnerEnabled survives restart.
        TerrainAlarmConfig.SaveFrom(this);
        ResetCalloutSequence();
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

        try
        {
            if (v.LandedOrSplashed || v.situation == Vessel.Situations.LANDED || v.situation == Vessel.Situations.SPLASHED)
            {
                ReleaseAll();
                ResetCalloutSequence();
                return;
            }
        }
        catch { }

        float agl = (float)v.radarAltitude;
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

        // Altitude callouts (pure crossing):
        // - if prevAgl > th and current agl <= th => play
        // - only protection is a short cooldown
        if (EnableAltitudeCallouts)
        {
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
                            SoundManager.PlayAltitudeCallout(th);
                        }
                        break;
                    }
                }
            }

            _prevCalloutAgl = agl;
        }
        else
        {
            _prevCalloutAgl = null;
            _lastCalloutByTh.Clear();
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
        if (EnableGearAlarm && !suppressGear)
        {
            // Use actual vertical speed for descent detection (more reliable than radar AGL rate).
            bool descending = vsAsl < -0.1f;
            gearCond = descending && (agl < GearAlarmAGL) && (spd < GearAlarmMaxSpeed) && !gearDeployed;
        }

        // Gear warning: do not share global throttle with terrain alarms
        if (gearCond)
        {
            // play gear warning periodically while condition holds
            if (Time.realtimeSinceStartup - _lastGearBeepTs > 2.0f)
            {
                _lastGearBeepTs = Time.realtimeSinceStartup;
                SoundManager.PlayGearBeep();
                ScreenMessages.PostScreenMessage($"Gear! AGL={agl:F0}m", 2.5f, ScreenMessageStyle.UPPER_CENTER);
            }
        }
        _wasGearAlarm = gearCond;

        bool baseCond = EnableTerrainBase && !suppressTerrain && !suppressPullUp && (agl < AltitudeAGL && vsAsl < DescentSpeed);

        bool aheadCond = false;
        string aheadMsg = null;
        if (!suppressTerrain && !suppressPullUp && EnableTerrainAhead)
        {
            if (spd >= TerrainAheadMinSpeed)
            {
                aheadCond = TryGetTerrainAheadImpactTTI(v, out float impactTime, out float impactDist);
                if (aheadCond)
                    aheadMsg = $"Terrain ahead! Impact in {impactTime:F1}s ({impactDist:F0}m)";
            }
        }

        // Holds: only for Pull_Up alarms
        UpdateHold(ref _holdBase, baseCond);
        UpdateHold(ref _holdAhead, aheadCond);

        // Sink rate callout: gear deployed, low AGL, high descent rate
        // Keep this independent of terrain message throttle.
        if (EnableSinkRate && gearDeployed && agl < SinkRateAGL && vsAsl < -Mathf.Abs(SinkRateMinDescent))
        {
            if (Time.realtimeSinceStartup - _lastSinkRateTs > SinkRateCooldown)
            {
                _lastSinkRateTs = Time.realtimeSinceStartup;
                SoundManager.PlaySinkRate();
            }
        }

        // Terrain messages throttled (screen messages only)
        bool allowTerrainMsg = (Time.realtimeSinceStartup - lastAlarmTs) >= ThrottleSeconds;

        if (allowTerrainMsg && baseCond)
        {
            ScreenMessages.PostScreenMessage($"Terrain warning! AGL={agl:F0}m, VSpeed={vsAsl:F0} m/s", 4f, ScreenMessageStyle.UPPER_CENTER);
            lastAlarmTs = Time.realtimeSinceStartup;
            return;
        }

        if (allowTerrainMsg && aheadCond)
        {
            ScreenMessages.PostScreenMessage(aheadMsg ?? "Terrain ahead!", 4f, ScreenMessageStyle.UPPER_CENTER);
            lastAlarmTs = Time.realtimeSinceStartup;
            return;
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
}
