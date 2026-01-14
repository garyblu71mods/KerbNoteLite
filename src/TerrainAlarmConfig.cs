using System;
using System.IO;
using UnityEngine;

internal static class TerrainAlarmConfig
{
    private const string FileName = "KerbNoteLite_TerrainAlarm.cfg";

    private static string GetPath()
    {
        try
        {
            // Same style as other persisted data in this mod: under KSP root
            return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbNoteLite", FileName);
        }
        catch
        {
            return FileName;
        }
    }

    /// <summary>
    /// Check if runner should be enabled according to config file (without creating a runner instance)
    /// </summary>
    public static bool ShouldRunnerBeEnabled()
    {
        try
        {
            string path = GetPath();
            if (!File.Exists(path))
            {
                // No config file: default to disabled (terrain alarms off by default)
                return false;
            }

            var node = ConfigNode.Load(path);
            if (node == null) return false;

            var n = node.GetNode("TERRAIN_ALARM");
            if (n == null) return false;

            // Read RunnerEnabled flag (default false if not specified)
            return GetBool(n, "RunnerEnabled", false);
        }
        catch
        {
            // On error, default to disabled
            return false;
        }
    }

    public static void LoadInto(TerrainAlarmRunner r)
    {
        if (r == null) return;
        try
        {
            string path = GetPath();
            if (!File.Exists(path))
            {
                // No config file: default to enabled
                r.enabled = true;
                return;
            }

            var node = ConfigNode.Load(path);
            if (node == null)
            {
                r.enabled = true;
                return;
            }
            var n = node.GetNode("TERRAIN_ALARM");
            if (n == null)
            {
                r.enabled = true;
                return;
            }

            // Default to TRUE (runner should be active unless explicitly disabled)
            bool enabledState = GetBool(n, "RunnerEnabled", true);
            r.enabled = enabledState;

            r.AltitudeAGL = GetFloat(n, "AltitudeAGL", r.AltitudeAGL);
            r.DescentSpeed = GetFloat(n, "DescentSpeed", r.DescentSpeed);

            r.EnableGearAlarm = GetBool(n, "EnableGearAlarm", r.EnableGearAlarm);
            r.GearAlarmAGL = GetFloat(n, "GearAlarmAGL", r.GearAlarmAGL);
            r.GearAlarmMaxSpeed = GetFloat(n, "GearAlarmMaxSpeed", r.GearAlarmMaxSpeed);
            r.GearAlarmMaxRollDegrees = GetFloat(n, "GearAlarmMaxRollDegrees", r.GearAlarmMaxRollDegrees);

            r.EnableTerrainAhead = GetBool(n, "EnableTerrainAhead", r.EnableTerrainAhead);
            r.EnableTerrainBase = GetBool(n, "EnableTerrainBase", r.EnableTerrainBase);
            r.TerrainAheadMarginMeters = GetFloat(n, "TerrainAheadMarginMeters", r.TerrainAheadMarginMeters);
            r.TerrainAheadMaxTime = GetFloat(n, "TerrainAheadMaxTime", r.TerrainAheadMaxTime);
            r.TerrainAheadStep = GetFloat(n, "TerrainAheadStep", r.TerrainAheadStep);
            r.TerrainAheadMinSpeed = GetFloat(n, "TerrainAheadMinSpeed", r.TerrainAheadMinSpeed);

            r.EnableAltitudeCallouts = GetBool(n, "EnableAltitudeCallouts", r.EnableAltitudeCallouts);
            
            // Landed callout (separate toggle, defaults to true for backward compatibility)
            r.EnableLandedCallout = GetBool(n, "EnableLandedCallout", true);

            r.EnableSinkRate = GetBool(n, "EnableSinkRate", r.EnableSinkRate);
            r.SinkRateAGL = GetFloat(n, "SinkRateAGL", r.SinkRateAGL);
            r.SinkRateMinDescent = GetFloat(n, "SinkRateMinDescent", r.SinkRateMinDescent);

            // Stall Warning
            r.EnableStallWarning = GetBool(n, "EnableStallWarning", r.EnableStallWarning);
            string stallModeStr = GetString(n, "StallWarningMode", r.StallWarningMode.ToString());
            if (System.Enum.TryParse<TerrainAlarmRunner.StallMode>(stallModeStr, out var stallMode))
            {
                r.StallWarningMode = stallMode;
            }
            r.StallMinHorizontalSpeed = GetFloat(n, "StallMinHorizontalSpeed", r.StallMinHorizontalSpeed);
            r.StallAngleThreshold = GetFloat(n, "StallAngleThreshold", r.StallAngleThreshold);
            r.StallMinAGL = GetFloat(n, "StallMinAGL", r.StallMinAGL);
            r.StallMaxAltitudeASL = GetFloat(n, "StallMaxAltitudeASL", r.StallMaxAltitudeASL);
            r.StallMinHorizontalSpeedAuto = GetFloat(n, "StallMinHorizontalSpeedAuto", r.StallMinHorizontalSpeedAuto);

            r.AircraftOnly = GetBool(n, "AircraftOnly", r.AircraftOnly);
            
            r.Volume = GetFloat(n, "Volume", 1.0f);
            r.Volume = Mathf.Clamp01(r.Volume); // ensure 0-1 range
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KerbNote][TerrainAlarmConfig] Load failed: " + ex.Message);
            // On error, default to enabled
            r.enabled = true;
        }
    }

    public static void SaveFrom(TerrainAlarmRunner r)
    {
        if (r == null) return;
        try
        {
            var root = new ConfigNode("KERBNOTE");
            var n = root.AddNode("TERRAIN_ALARM");

            n.AddValue("RunnerEnabled", r.enabled);

            n.AddValue("AltitudeAGL", r.AltitudeAGL);
            n.AddValue("DescentSpeed", r.DescentSpeed);

            n.AddValue("EnableGearAlarm", r.EnableGearAlarm);
            n.AddValue("GearAlarmAGL", r.GearAlarmAGL);
            n.AddValue("GearAlarmMaxSpeed", r.GearAlarmMaxSpeed);
            n.AddValue("GearAlarmMaxRollDegrees", r.GearAlarmMaxRollDegrees);

            n.AddValue("EnableTerrainAhead", r.EnableTerrainAhead);
            n.AddValue("EnableTerrainBase", r.EnableTerrainBase);
            n.AddValue("TerrainAheadMarginMeters", r.TerrainAheadMarginMeters);
            n.AddValue("TerrainAheadMaxTime", r.TerrainAheadMaxTime);
            n.AddValue("TerrainAheadStep", r.TerrainAheadStep);
            n.AddValue("TerrainAheadMinSpeed", r.TerrainAheadMinSpeed);

            n.AddValue("EnableAltitudeCallouts", r.EnableAltitudeCallouts);
            
            n.AddValue("EnableLandedCallout", r.EnableLandedCallout);

            n.AddValue("EnableSinkRate", r.EnableSinkRate);
            n.AddValue("SinkRateAGL", r.SinkRateAGL);
            n.AddValue("SinkRateMinDescent", r.SinkRateMinDescent);

            // Stall Warning
            n.AddValue("EnableStallWarning", r.EnableStallWarning);
            n.AddValue("StallWarningMode", r.StallWarningMode.ToString());
            n.AddValue("StallMinHorizontalSpeed", r.StallMinHorizontalSpeed);
            n.AddValue("StallAngleThreshold", r.StallAngleThreshold);
            n.AddValue("StallMinAGL", r.StallMinAGL);
            n.AddValue("StallMaxAltitudeASL", r.StallMaxAltitudeASL);
            n.AddValue("StallMinHorizontalSpeedAuto", r.StallMinHorizontalSpeedAuto);

            n.AddValue("AircraftOnly", r.AircraftOnly);
            
            n.AddValue("Volume", r.Volume);

            string path = GetPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            root.Save(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KerbNote][TerrainAlarmConfig] Save failed: " + ex.Message);
        }
    }

    private static float GetFloat(ConfigNode n, string key, float fallback)
    {
        try
        {
            if (!n.HasValue(key)) return fallback;
            float v;
            if (float.TryParse(n.GetValue(key), out v)) return v;
        }
        catch { }
        return fallback;
    }

    private static bool GetBool(ConfigNode n, string key, bool fallback)
    {
        try
        {
            if (!n.HasValue(key)) return fallback;
            bool v;
            if (bool.TryParse(n.GetValue(key), out v)) return v;
        }
        catch { }
        return fallback;
    }

    private static string GetString(ConfigNode n, string key, string fallback)
    {
        try
        {
            if (!n.HasValue(key)) return fallback;
            return n.GetValue(key) ?? fallback;
        }
        catch { }
        return fallback;
    }
}
