# KerbNote - User Guide

**Version:** 1.3.1  
**Author:** GaryBlu71  
**GitHub:** https://github.com/garyblu71mods/KerbNoteLite  
**License:** MIT  
**Support:** PayPal.me/GaryBlu71  
**Contact:** Garyblu71.mods@gmail.com

---

## What is KerbNote?

KerbNote is a comprehensive note-taking and alarm system for Kerbal Space Program. Write mission notes, set location-based alarms, monitor resources, and get terrain warnings - all integrated seamlessly into your game.

---

## Note System

### Basic Usage
- **Click toolbar icon** to open KerbNote window
- **Click "+"** to create a new note tab
- **Type freely** - notes save automatically
- **Click tab name** to rename it
- **Click "X" on tab** to delete (with confirmation)

### Editing Features
- **Tab key** - Insert 10 spaces for formatting
- **Ctrl+Z** - Undo last change
- **AAA button** - Cycle through 5 zoom levels
- **Auto-scroll** - Window follows cursor while typing

### Organizing Notes
- **Multiple tabs** - Unlimited tabs per save
- **Per-save storage** - Each game save has its own notes
- **Switch saves** - Settings ? Notes ? Select save
- Notes stored in: `GameData/KerbNoteLite/AlarmsAndNotes/`

---

## Location-Based Alarms

### How They Work
Alarms trigger when your vessel reaches specific **celestial body + situation** combinations.

### Setting Up an Alarm
1. **Open alarm panel** - Click sidebar (right edge of window)
2. **Select body** - Choose planet/moon (e.g., "Mun")
3. **Select situation** - Choose condition:
   - **Orbiting** - Stable orbit
   - **Flying** - Atmospheric flight
   - **Landed** - On solid ground
   - **Splashed** - In water
   - **Sub-Orbital** - Falling trajectory
   - **Escaping** - Leaving sphere of influence
   - **Docked** - Attached to another vessel
   - **VAB/SPH** - In vehicle editor
   - **Space Center** - At KSC
4. **Choose actions**:
   - ? **MiniNote** - Show floating note
   - ? **Play Sound** - Kerbal vocal alert
   - ? **Stop Warp** - Stop time acceleration
   - ? **Hide on Exit** - Auto-hide when condition ends
5. **Enable alarm** - Check the enable box

### Examples
- **Mun + Orbiting** - Reminder to start landing burn
- **Kerbin + Landed** - Mission debrief checklist
- **VAB** - Rocket design notes
- **Duna + Flying** - Atmospheric entry procedure

---

## MiniNotes

### What Are They?
Floating semi-transparent windows that show your notes during flight.

### Features
- **Auto-spawn** - Triggered by alarms
- **Draggable** - Position anywhere on screen
- **Click-through** - Doesn't block game interaction
- **Multiple active** - Several can be visible at once
- **Blink animations** - Indicates alarm triggers

### Behavior
- **Triple-fast blink** - First appearance or re-show
- **Fast blink** - Alarm re-triggered while visible
- **Persistent** - Stays visible across scene changes (unless "Hide on Exit" enabled)

---

## Terrain Proximity Alarms (GPWS)

Aviation-style warnings to prevent crashes.

### Opening Terrain Alarms
1. **Click alarm icon** (left side of KerbNote window)
2. **Click "Terrain Alarms"**
3. **Enable system** with toggle switch

### Available Warnings

#### Pull-Up (Base Terrain)
- **Triggers:** Low altitude + fast descent
- **Sound:** Continuous "Pull Up" callout
- **Default:** 750m AGL, -30 m/s descent
- **Use:** Prevents crashing into terrain

#### Terrain Ahead
- **Triggers:** Predicts collision in next 6 seconds
- **Sound:** "Terrain ahead! Impact in X seconds"
- **Default:** 6s look-ahead, 0m margin
- **Use:** Warns before hitting mountains/obstacles

#### Gear Warning
- **Triggers:** Landing without gear deployed
- **Sound:** "Too Low Gear" beeps
- **Default:** 200m AGL, <200 m/s speed
- **Smart:** Ignores warning when banked >45°
- **Use:** Prevents gear-up landings

#### Altitude Callouts
- **Triggers:** Crossing altitude thresholds
- **Sound:** "200... 100... 50... 10... Landed"
- **Callouts:** 200, 100, 90, 80, 70, 60, 50, 40, 30, 20, 10 meters
- **Use:** Situational awareness during landing

#### Sink Rate Warning
- **Triggers:** Fast descent with gear deployed
- **Sound:** "Sink Rate"
- **Default:** <70m AGL, -7 m/s descent
- **Use:** Prevents hard landings

#### Stall Warning (NEW in v1.3.1)
- **Triggers:** Loss of airspeed / energy decay
- **Sound:** "STALL!" callout
- **Modes:**
  - **AUTO:** Detects energy decay (descent vs horizontal speed ratio)
  - **MANUAL:** Simple minimum speed threshold
- **Altitude Range:** Only active between min/max altitude (default: 100m-25000m)
- **Smart:** Ignores high-pitch maneuvers (cobra, loops)
- **Use:** Prevents aerodynamic stalls and crashes

### Smart Features
- **Landing suppression** - Disables Pull-Up when gear out, slow, and low
- **20-second cooldown** - No false alarms after scene load
- **Aircraft-only filter** - Optional restriction to planes only
- **EVA safe** - Automatically disabled during spacewalks
- **Volume control** - Independent slider (0-100%)

---

## Resource Monitoring Alarms

Get warned before running out of critical resources.

### Opening Resource Alarms
1. **Click alarm icon** (left side of KerbNote window)
2. **Click "Resources Alarms"**
3. **Enable system** with toggle switch

### Monitored Resources
- **ElectricCharge** - Battery power
- **LiquidFuel** - Rocket fuel
- **Oxidizer** - Rocket oxidizer
- **MonoPropellant** - RCS/EVA jetpack fuel
- **Ablator** - Heat shield protection
- **Ore** - Mining operations
- **XenonGas** - Ion engine fuel
- **Custom** - Any resource in your vessel

### Configuring Thresholds
1. **Check resource** to enable monitoring
2. **Adjust slider** to set warning level (0-100%)
3. **Default 15%** - Get warned at 15% remaining

### Special Features
- **Depletion warnings** - Extra alert when reaching 0%
- **EVA jetpack monitoring** - Warns about MonoPropellant during spacewalks
- **Auto-detection** - Finds all resources in current vessel
- **Communication alarm** - Warns when signal strength drops
- **Global silence** - Temporarily mute all resource alarms

---

## Customization

### Changing Skins
1. **Open Settings** (bottom of KerbNote window)
2. **Click "Skin"**
3. **Select skin** from list
4. **Click "Back"**

### Adding Custom Skins
1. Create folder: `GameData/KerbNoteLite/texture_pack/MySkin/Textures/`
2. Add PNG files:
   - `BackgroundWindow.png`
   - `Button.png`, `ButtonHover.png`, `ButtonClick.png`
   - `Tab.png`, `TabHover.png`, `TabClick.png`
3. Restart KSP
4. Select your skin in Settings ? Skin

### Switching Saves
1. **Open Settings**
2. **Click "Notes"**
3. **Select save** from list
4. Notes and alarms switch to that save

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| **Tab** | Insert 10 spaces |
| **Ctrl+Z** | Undo |
| **Double-click titlebar** | Maximize/minimize window |
| **Double-click resize icon** | Maximize/minimize window |

---

## Pro Tips

1. **Mission Checklists** - Create tab with steps, set alarm for target body
2. **Safety Net** - Enable all terrain alarms for landing approach
3. **Fuel Management** - Set resource thresholds to 20% for safety margin
4. **Multiple Alarms** - Set alarms for same body with different situations
5. **MiniNote Positioning** - Drag to corner of screen for unobtrusive reference
6. **Volume Control** - Lower terrain alarm volume if too distracting
7. **Aircraft Filter** - Enable "Aircraft Only" to disable warnings for rockets
8. **EVA Monitoring** - Keep MonoPropellant alarm enabled for jetpack warnings
9. **Screen Messages** - Altitude callouts show even if sound disabled
10. **Load Cooldown** - Wait 20 seconds after scene load for alarms to activate

---

## Troubleshooting

### Alarms Not Triggering
- Wait 20 seconds after scene load (cooldown period)
- Check alarm enabled (green indicator on tab)
- Verify correct body and situation selected
- For terrain/resource alarms: Check Global Alarm Panel enabled

### No Sound
- Check KSP audio settings (not muted)
- Check volume slider in Global Alarm Panel
- Verify sound files exist in GameData/KerbNoteLite/Sounds/

### Window Won't Open
- Look for icon in application launcher (toolbar)
- Check KSP logs: KSP_Data/output_log.txt
- Verify mod installed correctly in GameData/KerbNoteLite/

### Performance Issues
- Latest version is highly optimized (80% faster)
- Disable unused alarm systems in Global Alarm Panel
- Reduce number of active note tabs

---

## File Locations

### Notes (per save)
```
GameData/KerbNoteLite/AlarmsAndNotes/Notes_[SaveName].txt
```

### Location Alarms (per save)
```
GameData/KerbNoteLite/AlarmsAndNotes/Alarms_[SaveName].txt
```

### Terrain Config (global)
```
GameData/KerbNoteLite/AlarmsAndNotes/TerrainAlarmConfig.cfg
```

### Resource Config (global)
```
GameData/KerbNoteLite/AlarmsAndNotes/ResourcesAlarmConfig.cfg
```

---

## Quick Start Examples

### Example 1: Mun Landing Checklist
1. Create note: "Mun Landing Steps"
2. Write: "1. Orbit 10km, 2. Retrograde burn, 3. Deploy gear at 500m, 4. Land"
3. Set alarm: **Mun + Orbiting** ? MiniNote + Sound
4. Launch to Mun - alarm shows checklist when you arrive!

### Example 2: Safe Landing
1. Enable terrain alarms in Global Alarm Panel
2. Enable: Pull-Up, Gear Warning, Altitude Callouts, Sink Rate
3. Set volume to 80%
4. Approach landing site - get guided by callouts!

### Example 3: Fuel Management
1. Enable resource alarms in Global Alarm Panel
2. Enable: LiquidFuel, Oxidizer (15% threshold)
3. Launch mission
4. Get warned before running out of fuel!

---

## FAQ

**Q: Are notes saved automatically?**  
A: Yes! Every change is saved immediately.

**Q: Can I use KerbNote in all game modes?**  
A: Yes! Works in Sandbox, Science, and Career mode.

**Q: Do alarms work without opening the panel?**  
A: Yes! Alarms auto-initialize from saved configuration.

**Q: Can I disable specific alarms?**  
A: Yes! Each alarm system can be toggled independently.

**Q: Does it affect game performance?**  
A: Latest version is highly optimized - 80-85% faster than before.

**Q: How many note tabs can I create?**  
A: Unlimited! Create as many as you need.

**Q: Will terrain alarms work for all vessels?**  
A: Yes, unless "Aircraft Only" filter is enabled.

**Q: What's new in v1.3.1?**  
A: Stall Warning system with altitude filtering, KerbVision compatibility fix, and improved "Hide on Exit" alarm behavior.

---

## Support & Feedback

**Found a bug? Got an idea? Want to say hi?**

- **Email:** Garyblu71.mods@gmail.com
- **GitHub Issues:** https://github.com/garyblu71mods/KerbNoteLite/issues
- **Support Development:** PayPal.me/GaryBlu71

---

**Feedback helps KerbNote evolve!**

Your suggestions, bug reports, and feature requests make this mod better for everyone.

---

*Safe flights and happy note-taking, Kerbanaut!*
