# KerbNoteLite - Quick Start Guide

Welcome to **KerbNoteLite**! This guide will get you up and running in 5 minutes.

---

## ?? Installation (30 seconds)

1. Download the latest release ZIP from [Releases](https://github.com/garyblu71mods/KerbNoteLite/releases)
2. Extract the `KerbNoteLite` folder
3. Copy it to `[KSP]/GameData/`
4. Launch KSP
5. Look for the KerbNote icon in the toolbar

? **Done!** The mod is ready to use.

---

## ?? Your First Note (1 minute)

1. **Click** the KerbNote icon in the toolbar
2. **Click** the "+" button to create a new tab
3. **Type** your mission notes
4. **Click** "AAA" below the note to zoom in/out
5. **Use Tab key** to indent text
6. **Use Ctrl+Z** to undo

?? **Tip**: Notes are saved automatically per game save.

---

## ?? Your First Alarm (2 minutes)

### Simple Location Alarm
1. Create or select a note tab
2. **Click** the sidebar (right edge of window)
3. **Select** target body (e.g., "Mun")
4. **Select** situation (e.g., "Orbiting")
5. **Enable** desired actions:
   - ? Mini-Note (shows floating note)
   - ? Play Sound (Kerbal vocal)
   - ? Stop Warp (stops time warp)
6. **Click** the enable checkbox at top
7. **Launch** to the Mun!

? When you reach Mun orbit, the alarm triggers!

---

## ?? Enable Terrain Warnings (1 minute)

Terrain alarms help prevent crashes with GPWS-style warnings.

1. **Click** the alarm icon (left side of KerbNote window)
2. **Click** "Terrain Alarms"
3. **Enable** the system with the toggle switch
4. **Enable** desired warnings:
   - ? Base Terrain Warning (Pull Up)
   - ? Gear Warning (Too Low Gear)
   - ? Altitude Callouts (200m, 100m, 50m, etc.)
   - ? Sink Rate Warning
5. **Adjust volume** slider if needed
6. **Click** "Back"

? Now you'll get warnings when flying too low!

---

## ?? Enable Resource Monitoring (1 minute)

Get warned before you run out of fuel or power.

1. **Open** Global Alarm Panel (alarm icon)
2. **Click** "Resources Alarms"
3. **Enable** the system
4. **Check** which resources to monitor:
   - ? ElectricCharge
   - ? LiquidFuel
   - ? Oxidizer
   - ? MonoPropellant
5. **Adjust** thresholds (default 15% is good)
6. **Click** "Back"

? You'll get warned when resources drop below thresholds!

---

## ?? Using Mini-Notes

Mini-notes are floating windows that show your notes.

### Automatic (via Alarms)
- Alarms can automatically show mini-notes
- They blink when triggered
- They persist across scene changes

### Manual
- Not yet implemented - use alarms to trigger them

?? **Tip**: Mini-notes can be dragged anywhere on screen.

---

## ?? Change UI Theme (Optional)

1. Click **Settings** at bottom of KerbNote
2. Click **Skin**
3. Select a skin from the list
4. Click **Back**

? The UI changes immediately!

---

## ?? Common Settings

### Terrain Alarm Settings
| Setting | Recommended | What it does |
|---------|-------------|--------------|
| **Altitude AGL** | 750m | Trigger height for Pull Up warning |
| **Descent Speed** | -30 m/s | How fast you're descending |
| **Gear Alarm AGL** | 200m | Warns if gear not deployed |
| **Volume** | 70-100% | How loud terrain sounds are |
| **Aircraft Only** | OFF | If ON, only works on planes |

### Resource Alarm Settings
| Resource | Recommended | What it does |
|----------|-------------|--------------|
| **ElectricCharge** | 15% | Warns at 15% battery |
| **LiquidFuel** | 15% | Warns at 15% fuel |
| **Oxidizer** | 15% | Warns at 15% oxidizer |
| **MonoPropellant** | 15% | Warns at 15% RCS fuel |

---

## ?? Example Use Cases

### Mission Planning
1. Create note tab: "Mun Landing Checklist"
2. List steps: approach, orbit, descent, landing
3. Set alarm: Mun + Orbiting ? show mini-note
4. During mission, mini-note pops up with checklist

### Safety Monitoring
1. Enable terrain alarms for landing approach
2. Enable resource alarms for long missions
3. Set low fuel threshold to 20% for safety margin
4. Fly with confidence!

### Science Missions
1. Create tab: "Mun Science Targets"
2. List biomes and experiments
3. Set alarms for Mun Landed and Mun Splashed
4. Get reminded when you reach each location

---

## ?? Troubleshooting

### Alarms Not Triggering?
- ? Wait 20 seconds after scene load (cooldown period)
- ? Check alarm is enabled (green indicator on tab)
- ? Verify correct body and situation
- ? For terrain/resource alarms: check Global Alarm Panel

### No Sound?
- ? Check game audio settings (not muted)
- ? For terrain alarms: check volume slider in Global Alarm Panel
- ? Sound files are in GameData/KerbNoteLite/Sounds/

### Window Won't Open?
- ? Look for icon in toolbar
- ? Try clicking it a few times
- ? Check logs: [KSP]/KSP_Data/output_log.txt

### Performance Issues?
- ? Latest version has major optimizations (80% faster)
- ? Disable unused alarm systems
- ? Reduce number of note tabs
- ? Lower terrain alarm update frequency

---

## ?? Pro Tips

1. **Use zoom levels** - AAA button cycles through 5 zoom levels for readability
2. **EVA jetpack warning** - Resource monitoring works during EVA for MonoPropellant
3. **Landing config** - Deploy gear early to suppress terrain warnings
4. **Multiple alarms** - Set alarms for same body with different situations
5. **Screen messages** - Altitude callouts show on screen even if sound disabled
6. **Roll suppression** - Gear warning ignores you when banked > 45°

---

## ?? Learn More

- **Full Documentation**: [README.md](README.md)
- **Changelog**: [CHANGELOG.md](CHANGELOG.md)
- **Report Issues**: [GitHub Issues](https://github.com/garyblu71mods/KerbNoteLite/issues)

---

## ?? Tutorial Mission

Try this simple mission to learn all features:

1. **Pre-Launch**:
   - Create note: "Mun Mission Plan"
   - Write: "1. Launch, 2. Orbit, 3. Transfer, 4. Mun Orbit, 5. Land"
   - Set alarm: Kerbin + Orbiting ? Mini-Note + Sound

2. **Launch**:
   - Enable resource alarms (10% threshold for this test)
   - Launch to orbit
   - Alarm triggers when you reach orbit!

3. **Transfer**:
   - Plan Mun transfer
   - Set alarm: Mun + Orbiting ? Mini-Note + Stop Warp

4. **Mun Approach**:
   - Enable terrain alarms
   - Approach Mun
   - Get altitude callouts during descent

5. **Landing**:
   - Deploy gear early
   - Terrain warnings guide you down
   - Get "Landed" callout on touchdown!

? **Congratulations!** You've used all major features.

---

## ? FAQ

**Q: Do notes save automatically?**  
A: Yes! Notes save automatically per game save.

**Q: Can I use this in career mode?**  
A: Yes! Works in all game modes.

**Q: Does it affect game performance?**  
A: Latest version is highly optimized (80-85% faster than before).

**Q: Can I disable alarms?**  
A: Yes! Each alarm system can be toggled in Global Alarm Panel.

**Q: Do I need to configure everything?**  
A: No! Defaults are sensible. Only configure if you want to customize.

**Q: Can I create multiple note tabs?**  
A: Yes! Unlimited tabs supported.

**Q: Will alarms work if I don't open the panel?**  
A: Yes! Alarms auto-initialize from saved configuration.

---

**Ready to fly?** ??

Open KerbNote, write your first note, and set your first alarm!

*Safe flights, Kerbanaut!* ?
