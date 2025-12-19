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

    public static void LoadInto(TerrainAlarmRunner r)
    {
        if (r == null) return;
        try
        {
            string path = GetPath();
            if (!File.Exists(path)) return;

            var node = ConfigNode.Load(path);
            if (node == null) return;
            var n = node.GetNode("TERRAIN_ALARM");
            if (n == null) return;

            bool enabledState = GetBool(n, "RunnerEnabled", false);
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

            r.EnableSinkRate = GetBool(n, "EnableSinkRate", r.EnableSinkRate);
            r.SinkRateAGL = GetFloat(n, "SinkRateAGL", r.SinkRateAGL);
            r.SinkRateMinDescent = GetFloat(n, "SinkRateMinDescent", r.SinkRateMinDescent);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KerbNote][TerrainAlarmConfig] Load failed: " + ex.Message);
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

            n.AddValue("EnableSinkRate", r.EnableSinkRate);
            n.AddValue("SinkRateAGL", r.SinkRateAGL);
            n.AddValue("SinkRateMinDescent", r.SinkRateMinDescent);

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
}
