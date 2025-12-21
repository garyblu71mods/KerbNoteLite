using UnityEngine;

/// <summary>
/// Automatically initializes alarm runners at flight scene start based on saved configuration.
/// This ensures alarms are active even if the Global Alarm Panel is never opened.
/// </summary>
[KSPAddon(KSPAddon.Startup.Flight, false)]
public class AlarmSystemBootstrap : MonoBehaviour
{
    private static bool _initialized = false;
    
    void Start()
    {
        // Prevent duplicate initialization if multiple instances somehow exist
        if (_initialized)
        {
            Destroy(gameObject);
            return;
        }
        
        _initialized = true;
        
        // Initialize ResourcesAlarmRunner if it was enabled in config
        InitializeResourcesAlarm();
        
        // Initialize TerrainAlarmRunner if it was enabled in config
        InitializeTerrainAlarm();
        
        Debug.Log("[AlarmSystemBootstrap] Alarm systems initialized from configuration");
    }
    
    private void InitializeResourcesAlarm()
    {
        try
        {
            // Check if ResourcesAlarmRunner is already in the scene (e.g., created by GlobalAlarmPanel)
            var existing = FindObjectOfType<ResourcesAlarmRunner>();
            if (existing != null)
            {
                // Already exists, just ensure config is loaded
                ResourcesAlarmConfig.LoadInto(existing);
                return;
            }
            
            // Check config directly to see if runner should be enabled
            bool shouldBeEnabled = ResourcesAlarmConfig.ShouldRunnerBeEnabled();
            
            // Only create permanent runner if it was enabled in config
            if (shouldBeEnabled)
            {
                var go = new GameObject("ResourcesAlarmRunner");
                var runner = go.AddComponent<ResourcesAlarmRunner>();
                DontDestroyOnLoad(go);
                ResourcesAlarmConfig.LoadInto(runner);
                Debug.Log("[AlarmSystemBootstrap] ResourcesAlarmRunner initialized and enabled");
            }
            else
            {
                Debug.Log("[AlarmSystemBootstrap] ResourcesAlarmRunner disabled in config, not creating");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AlarmSystemBootstrap] Failed to initialize ResourcesAlarmRunner: {ex.Message}");
        }
    }
    
    private void InitializeTerrainAlarm()
    {
        try
        {
            // Check if TerrainAlarmRunner is already in the scene
            var existing = FindObjectOfType<TerrainAlarmRunner>();
            if (existing != null)
            {
                // Already exists, just ensure config is loaded
                TerrainAlarmConfig.LoadInto(existing);
                return;
            }
            
            // Check config directly to see if runner should be enabled
            bool shouldBeEnabled = TerrainAlarmConfig.ShouldRunnerBeEnabled();
            
            // Only create permanent runner if it was enabled in config
            if (shouldBeEnabled)
            {
                var go = new GameObject("TerrainAlarmRunner");
                var runner = go.AddComponent<TerrainAlarmRunner>();
                DontDestroyOnLoad(go);
                TerrainAlarmConfig.LoadInto(runner);
                Debug.Log("[AlarmSystemBootstrap] TerrainAlarmRunner initialized and enabled");
            }
            else
            {
                Debug.Log("[AlarmSystemBootstrap] TerrainAlarmRunner disabled in config, not creating");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AlarmSystemBootstrap] Failed to initialize TerrainAlarmRunner: {ex.Message}");
        }
    }
    
    void OnDestroy()
    {
        _initialized = false;
    }
}
