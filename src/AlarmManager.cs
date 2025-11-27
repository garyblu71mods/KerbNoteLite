using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class AlarmDefinition
{
    public string TabGuid;
    public string BodyName;
    public Vessel.Situations Situation;
    public bool MiniNote;
    public bool PlaySound;
    public bool StopWarp;
    public bool HideOnExit;
    public bool Enabled;

    public string GetKey()
    {
        return ((TabGuid ?? string.Empty) + "|" + (BodyName ?? string.Empty) + "|" + Situation.ToString()).ToLowerInvariant();
    }
    
    // Check if TabGuid looks like a valid GUID (not a number from old format)
    public bool HasValidGuid()
    {
        if (string.IsNullOrEmpty(TabGuid)) return false;
        // Old format used numbers (0, 1, 2, etc.) - these are invalid
        int dummy;
        if (int.TryParse(TabGuid, out dummy)) return false;
        // Valid GUID should contain hyphens
        return TabGuid.Contains("-");
    }
}

public static class AlarmManager
{
    private static readonly List<AlarmDefinition> alarms = new List<AlarmDefinition>();
    private static string filePath;
    private static bool initialized;

    public static IReadOnlyList<AlarmDefinition> Alarms => alarms;

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
            Debug.LogError("[KerbNote][AlarmManager] Init error: " + ex.Message);
            initialized = true;
        }
    }

    public static void SaveOrUpdateAlarm(AlarmDefinition def)
    {
        if (!initialized) Init();
        EnsureCurrentPath();
        if (def == null) return;
        string key = def.GetKey();
        var existing = alarms.FirstOrDefault(a => a.GetKey() == key);
        if (existing == null)
        {
            alarms.Add(def);
        }
        else
        {
            existing.MiniNote = def.MiniNote;
            existing.PlaySound = def.PlaySound;
            existing.StopWarp = def.StopWarp;
            existing.HideOnExit = def.HideOnExit;
            existing.Enabled = def.Enabled;
        }
        SaveInternal();
    }

    public static void RemoveAlarm(string tabGuid, string bodyName, Vessel.Situations situation)
    {
        if (!initialized) Init();
        EnsureCurrentPath();
        string key = ((tabGuid ?? string.Empty) + "|" + (bodyName ?? string.Empty) + "|" + situation.ToString()).ToLowerInvariant();
        int removed = alarms.RemoveAll(a => a.GetKey() == key);
        if (removed >0) SaveInternal();
    }

    public static IEnumerable<AlarmDefinition> GetAlarmsForTab(string tabGuid)
    {
        if (!initialized) Init();
        EnsureCurrentPath();
        if (string.IsNullOrEmpty(tabGuid)) return Enumerable.Empty<AlarmDefinition>();
        return alarms.Where(a => string.Equals(a.TabGuid, tabGuid, StringComparison.OrdinalIgnoreCase));
    }

    public static void RemoveAllAlarmsForTab(string tabGuid)
    {
        if (!initialized) Init();
        EnsureCurrentPath();
        if (string.IsNullOrEmpty(tabGuid)) return;
        int removed = alarms.RemoveAll(a => string.Equals(a.TabGuid, tabGuid, StringComparison.OrdinalIgnoreCase));
        if (removed >0)
        {
            SaveInternal();
            Debug.Log("[KerbNote][AlarmManager] Removed " + removed + " alarm(s) for tab guid " + tabGuid);
        }
    }
    
    // NEW: Remove orphaned alarms (alarms whose tab no longer exists)
    public static int CleanupOrphanedAlarms(KerbNote host)
    {
        if (!initialized) Init();
        EnsureCurrentPath();
        if (host == null) return 0;
        
        // Get all valid tab GUIDs from host
        var validGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < host.TabCount; i++)
        {
            string guid = host.GetTabGuid(i);
            if (!string.IsNullOrEmpty(guid))
                validGuids.Add(guid);
        }
        
        // Remove alarms with invalid GUIDs or GUIDs not in valid set
        int removed = alarms.RemoveAll(a => 
        {
            if (!a.HasValidGuid())
            {
                Debug.Log($"[KerbNote][AlarmManager] Removing alarm with invalid GUID format: '{a.TabGuid}' (Body: {a.BodyName})");
                return true;
            }
            if (!validGuids.Contains(a.TabGuid ?? string.Empty))
            {
                Debug.Log($"[KerbNote][AlarmManager] Removing orphaned alarm for non-existent tab GUID: '{a.TabGuid}' (Body: {a.BodyName})");
                return true;
            }
            return false;
        });
        
        if (removed > 0)
        {
            SaveInternal();
            Debug.Log($"[KerbNote][AlarmManager] Cleaned up {removed} orphaned alarm(s)");
        }
        return removed;
    }

    private static void EnsureCurrentPath()
    {
        try
        {
            string newPath = ComputeAlarmPath();
            if (!string.Equals(newPath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                filePath = newPath;
                MigrateLegacyAlarmsIfNeeded();
                alarms.Clear();
                LoadInternal();
            }
            else if (alarms.Count ==0 && File.Exists(filePath))
            {
                LoadInternal();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote][AlarmManager] EnsureCurrentPath failed: " + ex.Message);
        }
    }

    private static string ComputeAlarmPath()
    {
        try
        {
            string folder = KerbNote.ActiveSaveOverride;
            if (string.IsNullOrEmpty(folder)) folder = HighLogic.SaveFolder;
            string modDir = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbCalcProject", "AlarmsAndNotes");
            if (!string.IsNullOrEmpty(folder))
            {
                string upper = Path.Combine(modDir, $"Alarms_{folder}.txt");
                if (File.Exists(upper)) return upper;
                string lower = Path.Combine(modDir, $"alarms_{folder}.txt");
                if (File.Exists(lower)) return lower;
                return upper;
            }
        }
        catch { }
        return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbCalcProject", "AlarmsAndNotes", "alarms.txt");
    }

    private static void MigrateLegacyAlarmsIfNeeded()
    {
        try
        {
            if (File.Exists(filePath)) return;

            var targetDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            string perSaveOld = string.Empty;
            try
            {
                string folder = KerbNote.ActiveSaveOverride;
                if (string.IsNullOrEmpty(folder)) folder = HighLogic.SaveFolder;
                if (!string.IsNullOrEmpty(folder))
                {
                    perSaveOld = Path.Combine(KSPUtil.ApplicationRootPath, "saves", folder, "KerbCalcProject", "alarms.txt");
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(perSaveOld) && File.Exists(perSaveOld))
            {
                File.Copy(perSaveOld, filePath, true);
                Debug.Log("[KerbNote][AlarmManager] Migrated per-save alarms from " + perSaveOld + " to " + filePath);
                return;
            }

            string legacyUpper = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbCalcProject", "Alarms.txt");
            string legacyLower = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbCalcProject", "alarms.txt");
            if (File.Exists(legacyUpper))
            {
                File.Copy(legacyUpper, filePath, true);
                Debug.Log("[KerbNote][AlarmManager] Migrated legacy alarms from " + legacyUpper + " to " + filePath);
                return;
            }
            if (File.Exists(legacyLower))
            {
                File.Copy(legacyLower, filePath, true);
                Debug.Log("[KerbNote][AlarmManager] Migrated legacy alarms from " + legacyLower + " to " + filePath);
                return;
            }
            try
            {
                string folder = KerbNote.ActiveSaveOverride;
                if (string.IsNullOrEmpty(folder)) folder = HighLogic.SaveFolder;
                if (!string.IsNullOrEmpty(folder))
                {
                    string oldUpperPerSave = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbCalcProject", $"Alarms_{folder}.txt");
                    string oldLowerPerSave = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbCalcProject", $"alarms_{folder}.txt");
                    if (File.Exists(oldUpperPerSave))
                    {
                        File.Copy(oldUpperPerSave, filePath, true);
                        Debug.Log("[KerbNote][AlarmManager] Migrated legacy per-save alarms from " + oldUpperPerSave + " to " + filePath);
                        return;
                    }
                    if (File.Exists(oldLowerPerSave))
                    {
                        File.Copy(oldLowerPerSave, filePath, true);
                        Debug.Log("[KerbNote][AlarmManager] Migrated legacy per-save alarms from " + oldLowerPerSave + " to " + filePath);
                        return;
                    }
                }
            }
            catch { }

            SaveInternal();
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote][AlarmManager] Migration failed: " + ex.Message);
        }
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
            int skipped = 0;
            foreach (var raw in lines)
            {
                var line = (raw ?? string.Empty).Trim();
                if (line.Length ==0) continue;
                if (line.StartsWith("#")) continue;
                var parts = line.Split('|');
                if (parts.Length <8) continue;
                if (!string.Equals(parts[0], "Alarm", StringComparison.OrdinalIgnoreCase)) continue;
                string tabGuid = parts[1];
                
                // Skip alarms with invalid GUID format (old numeric indices)
                int dummyInt;
                if (string.IsNullOrEmpty(tabGuid) || int.TryParse(tabGuid, out dummyInt) || !tabGuid.Contains("-"))
                {
                    skipped++;
                    Debug.LogWarning($"[KerbNote][AlarmManager] Skipping alarm with invalid GUID: '{tabGuid}' (old format?)");
                    continue;
                }
                
                string body = parts[2];
                Vessel.Situations sit;
                try { sit = (Vessel.Situations)Enum.Parse(typeof(Vessel.Situations), parts[3], true); }
                catch { continue; }
                bool mini = ParseBool(parts[4]);
                bool ps = ParseBool(parts[5]);
                bool sw = ParseBool(parts[6]);
                bool hideOnExit = false;
                bool en;
                if (parts.Length >=9)
                {
                    hideOnExit = ParseBool(parts[7]);
                    en = ParseBool(parts[8]);
                }
                else
                {
                    en = ParseBool(parts[7]);
                }
                alarms.Add(new AlarmDefinition
                {
                    TabGuid = tabGuid,
                    BodyName = body,
                    Situation = sit,
                    MiniNote = mini,
                    PlaySound = ps,
                    StopWarp = sw,
                    HideOnExit = hideOnExit,
                    Enabled = en
                });
            }
            if (skipped > 0)
            {
                Debug.Log($"[KerbNote][AlarmManager] Skipped {skipped} alarm(s) with invalid GUID format. These will be removed on next save.");
                // Auto-save to remove invalid entries
                SaveInternal();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote][AlarmManager] Load error: " + ex.Message);
        }
    }

    private static void SaveInternal()
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("# KerbNote alarms file");
            sb.AppendLine("# Format: Alarm|TabGuid|BodyName|Situation|MiniNote|PlaySound|StopWarp|HideOnExit|Enabled");
            foreach (var a in alarms)
            {
                sb.Append("Alarm|");
                sb.Append(a.TabGuid ?? string.Empty).Append('|');
                sb.Append(a.BodyName ?? string.Empty).Append('|');
                sb.Append(a.Situation.ToString()).Append('|');
                sb.Append(BoolTo01(a.MiniNote)).Append('|');
                sb.Append(BoolTo01(a.PlaySound)).Append('|');
                sb.Append(BoolTo01(a.StopWarp)).Append('|');
                sb.Append(BoolTo01(a.HideOnExit)).Append('|');
                sb.Append(BoolTo01(a.Enabled)).AppendLine();
            }
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, sb.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError("[KerbNote][AlarmManager] Save error: " + ex.Message);
        }
    }

    private static bool ParseBool(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        s = s.Trim();
        if (s == "1") return true;
        if (s == "0") return false;
        bool b; if (bool.TryParse(s, out b)) return b; else return false;
    }

    private static string BoolTo01(bool b) => b ? "1" : "0";
}
