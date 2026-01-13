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
                r.UseKerbalVoiceAlarm = true;
                r.UseBeepAlarm = false;
                r.EnableCommAlarm = false;
                r.EnableEvaMonoPropAlarm = true;
                r.EnsureDefaults();
                return;
            }

            var node = ConfigNode.Load(path);
            if (node == null)
            {
                r.enabled = true;
                r.UseKerbalVoiceAlarm = true;
                r.UseBeepAlarm = false;
                r.EnableCommAlarm = false;
                r.EnableEvaMonoPropAlarm = true;
                r.EnsureDefaults();
                return;
            }
            var n = node.GetNode("RESOURCES_ALARM");
            if (n == null)
            {
                r.enabled = true;
                r.UseKerbalVoiceAlarm = true;
                r.UseBeepAlarm = false;
                r.EnableCommAlarm = false;
                r.EnableEvaMonoPropAlarm = true;
                r.EnsureDefaults();
                return;
            }

            // Default to TRUE (runner should be active unless explicitly disabled)
            bool enabledState = GetBool(n, "RunnerEnabled", true);
            r.enabled = enabledState;

            // Load alarm sound mode (only one can be true)
            r.UseKerbalVoiceAlarm = GetBool(n, "UseKerbalVoiceAlarm", true);
            r.UseBeepAlarm = GetBool(n, "UseBeepAlarm", false);
            // Ensure mutual exclusivity: if both are true, prefer Kerbal Voice
            if (r.UseKerbalVoiceAlarm && r.UseBeepAlarm)
            {
                r.UseBeepAlarm = false;
            }
            // If both are false, default to Kerbal Voice
            if (!r.UseKerbalVoiceAlarm && !r.UseBeepAlarm)
            {
                r.UseKerbalVoiceAlarm = true;
            }
            
            // Load communication alarm flag
            r.EnableCommAlarm = GetBool(n, "EnableCommAlarm", false);
            r.MuteCommAlarm = GetBool(n, "MuteCommAlarm", false);
            
            // Load communication signal threshold (default 25%)
            r.CommSignalThreshold = GetFloat(n, "CommSignalThreshold", 0.25f);
            
            // Load EVA MonoPropellant alarm flag (default true)
            r.EnableEvaMonoPropAlarm = GetBool(n, "EnableEvaMonoPropAlarm", true);
            r.MuteEvaMonoPropAlarm = GetBool(n, "MuteEvaMonoPropAlarm", false);
            
            // Load EVA MonoPropellant threshold (default 15%)
            r.EvaMonoPropThreshold = GetFloat(n, "EvaMonoPropThreshold", 0.15f);
            
            // Load Delta-V alarm flag (default false)
            r.EnableDeltaVAlarm = GetBool(n, "EnableDeltaVAlarm", false);
            r.MuteDeltaVAlarm = GetBool(n, "MuteDeltaVAlarm", false);
            
            // Load Delta-V threshold (default 100 m/s)
            r.DeltaVThreshold = GetFloat(n, "DeltaVThreshold", 100f);

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
            
            // Load muted resources
            r.MutedResources.Clear();
            string mutedList = n.GetValue("MutedResources");
            if (!string.IsNullOrEmpty(mutedList))
            {
                var names = mutedList.Split(',');
                foreach (var name in names)
                {
                    string trimmed = name.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        r.MutedResources.Add(trimmed);
                }
            }

            // Ensure defaults are present
            r.EnsureDefaults();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KerbNote][ResourcesAlarmConfig] Load failed: " + ex.Message);
            r.enabled = true;
            r.UseKerbalVoiceAlarm = true;
            r.UseBeepAlarm = false;
            r.EnableCommAlarm = false;
            r.EnableEvaMonoPropAlarm = true;
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
            n.AddValue("UseKerbalVoiceAlarm", r.UseKerbalVoiceAlarm);
            n.AddValue("UseBeepAlarm", r.UseBeepAlarm);
            n.AddValue("EnableCommAlarm", r.EnableCommAlarm);
            n.AddValue("MuteCommAlarm", r.MuteCommAlarm);
            n.AddValue("CommSignalThreshold", r.CommSignalThreshold);
            n.AddValue("EnableEvaMonoPropAlarm", r.EnableEvaMonoPropAlarm);
            n.AddValue("MuteEvaMonoPropAlarm", r.MuteEvaMonoPropAlarm);
            n.AddValue("EvaMonoPropThreshold", r.EvaMonoPropThreshold);
            n.AddValue("EnableDeltaVAlarm", r.EnableDeltaVAlarm);
            n.AddValue("MuteDeltaVAlarm", r.MuteDeltaVAlarm);
            n.AddValue("DeltaVThreshold", r.DeltaVThreshold);

            // Save enabled resources as comma-separated list
            if (r.EnabledResources.Count > 0)
            {
                string enabledList = string.Join(",", r.EnabledResources.ToArray());
                n.AddValue("EnabledResources", enabledList);
            }
            
            // Save muted resources as comma-separated list
            if (r.MutedResources.Count > 0)
            {
                string mutedList = string.Join(",", r.MutedResources.ToArray());
                n.AddValue("MutedResources", mutedList);
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
