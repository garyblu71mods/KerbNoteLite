using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Zarz?dza globalnym systemem alarmów (zasobów, terenu, harmonogramu)
/// </summary>
public static class GlobalAlarmManager
{
    private static readonly List<GlobalAlarmDefinition> alarms = new List<GlobalAlarmDefinition>();
    private static string filePath;
    private static bool initialized;

    public static IReadOnlyList<GlobalAlarmDefinition> Alarms => alarms;

    public static void Init()
    {
        if (initialized)
        {
            EnsureCurrentPath();
            return;
        }
        try
        {
            EnsureCurrentPath();
            initialized = true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote][GlobalAlarmManager] Init error: " + ex.Message);
            initialized = true;
        }
    }

    public static void SaveOrUpdateAlarm(GlobalAlarmDefinition def)
    {
        if (!initialized) Init();
        EnsureCurrentPath();
        if (def == null) return;
        
        string key = def.GetKey();
        var existing = alarms.FirstOrDefault(a => string.Equals(a.GetKey(), key, StringComparison.OrdinalIgnoreCase));
        
        if (existing == null)
        {
            alarms.Add(def);
        }
        else
        {
            // Update existing
            existing.Type = def.Type;
            existing.Enabled = def.Enabled;
            existing.ResourceThreshold = def.ResourceThreshold;
            existing.ResourceName = def.ResourceName;
            existing.MinAltitude = def.MinAltitude;
            existing.MinVerticalSpeed = def.MinVerticalSpeed;
            existing.ScheduleYear = def.ScheduleYear;
            existing.ScheduleMonth = def.ScheduleMonth;
            existing.ScheduleDay = def.ScheduleDay;
            existing.ScheduleHour = def.ScheduleHour;
            existing.ScheduleMinute = def.ScheduleMinute;
            existing.ScheduleMessage = def.ScheduleMessage;
            existing.PlaySound = def.PlaySound;
            existing.ShowScreenMessage = def.ShowScreenMessage;
            existing.StopTimeWarp = def.StopTimeWarp;
        }
        SaveInternal();
    }

    public static void RemoveAlarm(string name)
    {
        if (!initialized) Init();
        EnsureCurrentPath();
        int removed = alarms.RemoveAll(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        if (removed > 0) SaveInternal();
    }

    public static void RemoveAllAlarms()
    {
        if (!initialized) Init();
        EnsureCurrentPath();
        alarms.Clear();
        SaveInternal();
    }

    private static void EnsureCurrentPath()
    {
        try
        {
            string newPath = ComputeAlarmPath();
            if (!string.Equals(newPath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                filePath = newPath;
                alarms.Clear();
                LoadInternal();
            }
            else if (alarms.Count == 0 && File.Exists(filePath))
            {
                LoadInternal();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote][GlobalAlarmManager] EnsureCurrentPath failed: " + ex.Message);
        }
    }

    private static string ComputeAlarmPath()
    {
        try
        {
            string folder = KerbNote.ActiveSaveOverride;
            if (string.IsNullOrEmpty(folder)) folder = HighLogic.SaveFolder;
            string modDir = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbNoteLite", "AlarmsAndNotes");
            if (!string.IsNullOrEmpty(folder))
            {
                string upper = Path.Combine(modDir, $"GlobalAlarms_{folder}.txt");
                if (File.Exists(upper)) return upper;
                string lower = Path.Combine(modDir, $"globalAlarms_{folder}.txt");
                if (File.Exists(lower)) return lower;
                return upper;
            }
        }
        catch { }
        return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbNoteLite", "AlarmsAndNotes", "globalAlarms.txt");
    }

    private static void LoadInternal()
    {
        alarms.Clear();
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(filePath))
            {
                SaveInternal();
                return;
            }
            
            var lines = File.ReadAllLines(filePath);
            foreach (var raw in lines)
            {
                var line = (raw ?? string.Empty).Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#")) continue;
                
                var parts = line.Split('|');
                if (parts.Length < 3) continue;
                if (!string.Equals(parts[0], "GlobalAlarm", StringComparison.OrdinalIgnoreCase)) continue;

                var alarm = new GlobalAlarmDefinition();
                alarm.Name = parts[1] ?? "Alarm";
                
                if (int.TryParse(parts[2], out int typeInt))
                {
                    alarm.Type = (GlobalAlarmDefinition.AlarmType)typeInt;
                }
                
                if (parts.Length > 3) alarm.Enabled = ParseBool(parts[3]);
                if (parts.Length > 4) alarm.ResourceThreshold = ParseFloat(parts[4], 10f);
                if (parts.Length > 5) alarm.ResourceName = parts[5];
                if (parts.Length > 6) alarm.MinAltitude = ParseFloat(parts[6], 750f);
                if (parts.Length > 7) alarm.MinVerticalSpeed = ParseFloat(parts[7], -30f);
                if (parts.Length > 8) alarm.PlaySound = ParseBool(parts[8]);
                if (parts.Length > 9) alarm.ShowScreenMessage = ParseBool(parts[9]);
                if (parts.Length > 10) alarm.StopTimeWarp = ParseBool(parts[10]);
                
                alarms.Add(alarm);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote][GlobalAlarmManager] Load error: " + ex.Message);
        }
    }

    private static void SaveInternal()
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("# KerbNote Global Alarms file");
            sb.AppendLine("# Format: GlobalAlarm|Name|Type|Enabled|ResourceThreshold|ResourceName|MinAltitude|MinVerticalSpeed|PlaySound|ShowScreenMessage|StopTimeWarp");
            
            foreach (var a in alarms)
            {
                sb.Append("GlobalAlarm|");
                sb.Append(a.Name ?? string.Empty).Append('|');
                sb.Append((int)a.Type).Append('|');
                sb.Append(BoolTo01(a.Enabled)).Append('|');
                sb.Append(a.ResourceThreshold).Append('|');
                sb.Append(a.ResourceName ?? string.Empty).Append('|');
                sb.Append(a.MinAltitude).Append('|');
                sb.Append(a.MinVerticalSpeed).Append('|');
                sb.Append(BoolTo01(a.PlaySound)).Append('|');
                sb.Append(BoolTo01(a.ShowScreenMessage)).Append('|');
                sb.Append(BoolTo01(a.StopTimeWarp)).AppendLine();
            }
            
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, sb.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote][GlobalAlarmManager] Save error: " + ex.Message);
        }
    }

    private static bool ParseBool(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        s = s.Trim();
        if (s == "1") return true;
        if (s == "0") return false;
        bool b;
        if (bool.TryParse(s, out b)) return b;
        return false;
    }

    private static float ParseFloat(string s, float defaultValue)
    {
        if (string.IsNullOrEmpty(s)) return defaultValue;
        float f;
        if (float.TryParse(s, out f)) return f;
        return defaultValue;
    }

    private static string BoolTo01(bool b) => b ? "1" : "0";
}
