using UnityEngine;
using System.Reflection;

// Schedules a simple time-based reminder using Kerbin calendar (years/days/hours/minutes)
public class TimeReminderAlarm : MonoBehaviour
{
    private double targetUT;
    private bool armed;
    private const float ThrottleSeconds = 5f;
    private float lastMsg;
    public string NoteText;

    // Kerbin: 6h per day, 426 days per year by default
    public void ScheduleKerbin(int year, int day, int hour, int minute, string note = null)
    {
        NoteText = note ?? string.Empty;
        if (year < 1) year = 1; if (day < 1) day = 1;
        hour = Mathf.Clamp(hour, 0, 5); // 0..5 (6h day)
        minute = Mathf.Clamp(minute, 0, 59);

        double secondsPerDay = 6 * 60 * 60;
        double secondsPerYear = 426 * secondsPerDay;
        targetUT = (year - 1) * secondsPerYear + (day - 1) * secondsPerDay + hour * 3600.0 + minute * 60.0;
        armed = true;
        enabled = true;
        ScreenMessages.PostScreenMessage($"[Global Alarm] Reminder set to Y{year} D{day} {hour:D2}:{minute:D2}", 4f, ScreenMessageStyle.UPPER_CENTER);
    }

    void Update()
    {
        if (!armed) return;
        double now = Planetarium.GetUniversalTime();
        if (now >= targetUT)
        {
            if (Time.realtimeSinceStartup - lastMsg > ThrottleSeconds)
            {
                TryFire();
                lastMsg = Time.realtimeSinceStartup;
            }
        }
    }

    private void TryFire()
    {
        try
        {
            // Spawn MiniNote with header and message
            var host = GameObject.FindObjectOfType<KerbNote>();
            if (host != null)
            {
                var go = new GameObject("MiniNote_TimeReminder");
                var mn = go.AddComponent<MiniNote>();
                // Bind to active tab to reuse MiniNote UI plumbing
                mn.InitWithGuid(host, host.ActiveTabGuid);
                Object.DontDestroyOnLoad(go);
                mn.SpawnedByAlarm = true;
                // Try set header/text via common APIs or reflection
                bool setDone = false;
                try
                {
                    // Common methods
                    var mTitle = typeof(MiniNote).GetMethod("SetTitle", BindingFlags.Public | BindingFlags.Instance);
                    var mText = typeof(MiniNote).GetMethod("SetText", BindingFlags.Public | BindingFlags.Instance);
                    if (mTitle != null) { mTitle.Invoke(mn, new object[] { "Time reminder" }); setDone = true; }
                    if (mText != null) { mText.Invoke(mn, new object[] { NoteText ?? string.Empty }); setDone = true; }
                }
                catch { }
                if (!setDone)
                {
                    // Try properties/fields
                    TrySetMember(mn, "Title", "Time reminder");
                    TrySetMember(mn, "Header", "Time reminder");
                    TrySetMember(mn, "Text", NoteText ?? string.Empty);
                    TrySetMember(mn, "Content", NoteText ?? string.Empty);
                }
                mn.Show();
                mn.BlinkTripleFast();
            }
            // Also play sound
            SoundManager.PlayDefaultAlarm();
            armed = false; // one-shot
            Destroy(gameObject, 0.1f);
        }
        catch { }
    }

    private void TrySetMember(object obj, string name, object value)
    {
        if (obj == null) return;
        var t = obj.GetType();
        try
        {
            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanWrite)
            {
                p.SetValue(obj, value, null);
                return;
            }
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f != null)
            {
                f.SetValue(obj, value);
                return;
            }
        }
        catch { }
    }
}
