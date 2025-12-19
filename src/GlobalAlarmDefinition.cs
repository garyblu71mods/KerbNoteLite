using System;

/// <summary>
/// Definicja globalnego alarmu (zasobów, terenu, harmonogramu)
/// </summary>
public class GlobalAlarmDefinition
{
    public enum AlarmType
    {
        Resources,     // Alarm zasobów (paliwo, elektryczno??, etc.)
        Terrain,       // Alarm terenu (zbyt niska wysoko??, szybka opadanie)
        Schedule       // Alarm harmonogramu (przypomnienie o okre?lonym czasie)
    }

    public string Name { get; set; }
    public AlarmType Type { get; set; }
    public bool Enabled { get; set; }
    
    // Resources alarm
    public float ResourceThreshold { get; set; }  // % poni?ej którego wyzwala alarm
    public string ResourceName { get; set; }      // np. "LiquidFuel", "ElectricCharge"
    
    // Terrain alarm
    public float MinAltitude { get; set; }        // minimalna wysoko?? nad terenem (metry)
    public float MinVerticalSpeed { get; set; }   // minimalna pr?dko?? opadania (m/s, ujemna)
    
    // Schedule alarm
    public int ScheduleYear { get; set; }
    public int ScheduleMonth { get; set; }
    public int ScheduleDay { get; set; }
    public int ScheduleHour { get; set; }
    public int ScheduleMinute { get; set; }
    public string ScheduleMessage { get; set; }
    
    // Alarm actions
    public bool PlaySound { get; set; }
    public bool ShowScreenMessage { get; set; }
    public bool StopTimeWarp { get; set; }
    
    public string GetKey()
    {
        return (Name ?? string.Empty).ToLowerInvariant();
    }

    public GlobalAlarmDefinition()
    {
        Name = "Alarm";
        Type = AlarmType.Resources;
        Enabled = true;
        ResourceThreshold = 10f;
        MinAltitude = 750f;
        MinVerticalSpeed = -30f;
        PlaySound = true;
        ShowScreenMessage = true;
        StopTimeWarp = false;
    }
}
