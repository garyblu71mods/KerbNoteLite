using System;
using System.IO;
using System.Linq;
using UnityEngine;

internal static class ResourcesAlarmConfig
{
    private const string FileName = "KerbNoteLite_ResourcesAlarm.cfg";

    private static string GetPath()
    {
        try
        {
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
                // No config file: default to enabled
                return true;
            }

            var node = ConfigNode.Load(path);
            if (node == null) return true;
            
            var n = node.GetNode("RESOURCES_ALARM");
            if (n == null) return true;

            // Read RunnerEnabled flag (default true if not specified)
            return GetBool(n, "RunnerEnabled", true);
        }
        catch
        {
            // On error, default to enabled
            return true;
        }
    }

    public static void LoadInto(ResourcesAlarmRunner r)
    {
        if (r == null) return;
        try
        {
            string path = GetPath();
            if (!File.Exists(path))
            {
                // No config file: default to enabled with standard resources
                r.enabled = true;
                r.SilenceAlarms = false;
                r.EnableCommAlarm = false;
                r.EnsureDefaults();
                return;
            }

            var node = ConfigNode.Load(path);
            if (node == null)
            {
                r.enabled = true;
                r.SilenceAlarms = false;
                r.EnableCommAlarm = false;
                r.EnsureDefaults();
                return;
            }
            var n = node.GetNode("RESOURCES_ALARM");
            if (n == null)
            {
                r.enabled = true;
                r.SilenceAlarms = false;
                r.EnableCommAlarm = false;
                r.EnsureDefaults();
                return;
            }

            // Default to TRUE (runner should be active unless explicitly disabled)
            bool enabledState = GetBool(n, "RunnerEnabled", true);
            r.enabled = enabledState;

            // Load silence flag
            r.SilenceAlarms = GetBool(n, "SilenceAlarms", false);
            
            // Load communication alarm flag
            r.EnableCommAlarm = GetBool(n, "EnableCommAlarm", false);
            
            // Load communication signal threshold (default 25%)
            r.CommSignalThreshold = GetFloat(n, "CommSignalThreshold", 0.25f);

            // Load enabled resources
            r.EnabledResources.Clear();
            string enabledList = n.GetValue("EnabledResources");
            if (!string.IsNullOrEmpty(enabledList))
            {
                var names = enabledList.Split(',');
                foreach (var name in names)
                {
                    string trimmed = name.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        r.EnabledResources.Add(trimmed);
                }
            }

            // Load thresholds
            r.ThresholdByResource.Clear();
            var thresholdNodes = n.GetNodes("THRESHOLD");
            foreach (var tn in thresholdNodes)
            {
                string resName = tn.GetValue("Name");
                float threshold = GetFloat(tn, "Value", 0.15f);
                if (!string.IsNullOrEmpty(resName))
                    r.ThresholdByResource[resName] = threshold;
            }

            // Ensure defaults are present
            r.EnsureDefaults();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KerbNote][ResourcesAlarmConfig] Load failed: " + ex.Message);
            r.enabled = true;
            r.SilenceAlarms = false;
            r.EnableCommAlarm = false;
            r.EnsureDefaults();
        }
    }

    public static void SaveFrom(ResourcesAlarmRunner r)
    {
        if (r == null) return;
        try
        {
            var root = new ConfigNode("KERBNOTE");
            var n = root.AddNode("RESOURCES_ALARM");

            n.AddValue("RunnerEnabled", r.enabled);
            n.AddValue("SilenceAlarms", r.SilenceAlarms);
            n.AddValue("EnableCommAlarm", r.EnableCommAlarm);
            n.AddValue("CommSignalThreshold", r.CommSignalThreshold);

            // Save enabled resources as comma-separated list
            if (r.EnabledResources.Count > 0)
            {
                string enabledList = string.Join(",", r.EnabledResources.ToArray());
                n.AddValue("EnabledResources", enabledList);
            }

            // Save thresholds
            foreach (var kvp in r.ThresholdByResource)
            {
                var tn = n.AddNode("THRESHOLD");
                tn.AddValue("Name", kvp.Key);
                tn.AddValue("Value", kvp.Value);
            }

            string path = GetPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            root.Save(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KerbNote][ResourcesAlarmConfig] Save failed: " + ex.Message);
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
