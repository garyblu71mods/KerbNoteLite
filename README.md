# KerbNoteLite

**KerbNoteLite** is a comprehensive note-taking and advanced alarm system mod for Kerbal Space Program that helps you manage your missions with in-game notes, mini-notes, location-based alarms, terrain warnings, and resource monitoring.

[![Version](https://img.shields.io/badge/version-latest-blue.svg)](https://github.com/garyblu71mods/KerbNoteLite/releases)
[![KSP Compatible](https://img.shields.io/badge/KSP-Compatible-green.svg)](https://kerbalspaceprogram.com)
[![License](https://img.shields.io/badge/license-MIT-brightgreen.svg)](LICENSE)

---

##  Key Features

###  Advanced Note-Taking System
- **Multi-tab notebook** with unlimited tabs for organizing mission notes
- **Rich text formatting** with 5 adjustable zoom levels
- **Undo support** (Ctrl+Z) for text editing
- **Tab insertion** (10 spaces) for formatted notes
- **Auto-scroll** to cursor while typing
- **Draggable and resizable** window interface
- **Per-save note persistence** - different notes for each game save
- **Floating mini-notes** for quick reference during flight

###  Location-Based Alarm System
- **Situation-based alarms** trigger when entering specific conditions:
  - Celestial body arrival (planets, moons)
  - Vessel situations (Orbiting, Flying, Landed, Splashed, Sub-Orbital, Escaping, Docked)
  - Editor scenes (VAB/SPH)
  - Space Center
- **Alarm actions:**
  - Show mini-note popup with blinking animations
  - Play Kerbal vocal sounds
  - Stop time warp automatically
  - Auto-hide on scene exit
- **Per-tab alarms** - assign different alarms to different note tabs
- **Automatic alarm cleanup** removes orphaned alarms

###  Terrain Proximity Alarms (GPWS-style)
Advanced terrain collision avoidance system with realistic aviation-inspired warnings:

#### Base Terrain Warning
- **Pull-Up alarm** when descending too fast below configurable altitude
- Configurable altitude threshold (default: 750m AGL)
- Configurable descent rate trigger (default: -30 m/s)
- Continuous "Pull Up" audio callout while condition persists

#### Terrain Ahead Prediction
- **Look-ahead collision detection** predicts terrain impact
- Configurable prediction time (default: 6 seconds)
- Adjustable terrain margin for early warnings
- Only active above minimum speed threshold

#### Altitude Callouts
- **Automated altitude announcements** during descent:
  - 200m, 100m, 90m, 80m, 70m, 60m, 50m, 40m, 30m, 20m, 10m
- **"Landed" callout** when touchdown is confirmed
- Screen messages always shown, audio optional

#### Gear Warning System
- **"Too Low Gear"** alarm when approaching landing without gear deployed
- Configurable altitude threshold (default: 200m AGL)
- Maximum speed limit (default: 200 m/s)
- Roll angle suppression (ignores warning when banked > 45°)

#### Sink Rate Warning
- **"Sink Rate"** callout when descending too fast with gear deployed
- Configurable threshold (default: -7 m/s below 70m AGL)
- Helps prevent hard landings

#### Stall Warning System
- **Loss of airspeed detection** with two modes:
  - **AUTO mode**: Intelligent energy decay detection
    - Monitors descent-to-horizontal speed ratio
    - Example: Alarm if descending 15 m/s while flying 40 m/s horizontally (37.5% ratio)
    - Default threshold: 35% (configurable)
    - Ignores high-pitch maneuvers (cobra, loops, etc.)
    - Only active when moving forward at reasonable speed (>20 m/s)
  - **MANUAL mode**: Simple minimum speed threshold
    - User sets minimum safe horizontal speed (default: 50 m/s)
    - Alarm when below threshold
- Configurable minimum altitude (default: 100m)
- Prevents false alarms during landing approach
- Dedicated "STALL!" audio warning (custom sound file supported)
- Automatic fallback beep if sound file not present
- See `STALL_SOUND_INSTALLATION.md` for audio file setup

#### Smart Suppression
- **Landing mode detection** - suppresses alarms when gear is out and slow
- **Aircraft-only filter** - optional restriction to aircraft vessels
- **20-second load cooldown** prevents false alarms after scene load
- **EVA filtering** - disables terrain alarms during spacewalks
- **Physics settling protection** - ignores false landings during load

#### Volume Control
- Independent volume slider for all terrain alarm sounds (0-100%)
- Does not affect screen messages

### 📊 Resource Monitoring Alarms
Real-time vessel resource monitoring with customizable thresholds:

#### Monitored Resources
- **ElectricCharge** - Prevents power failures
- **LiquidFuel / Oxidizer** - Fuel depletion warnings
- **MonoPropellant** - RCS fuel monitoring (works during EVA for jetpacks!)
- **Ablator** - Heat shield protection
- **Ore** - Mining operation monitoring
- **XenonGas** - Ion engine fuel
- **Custom resources** - Any resource in current vessel

#### Smart Features
- **Per-resource thresholds** (0-100%, default 15%)
- **Enable/disable individual resources** independently
- **Depletion warnings** when resource reaches 0%
- **EVA jetpack monitoring** - MonoPropellant alarm works during spacewalks
- **Auto-detection** of vessel resources
- **8-second throttle** prevents alarm spam
- **20-second load cooldown** avoids false alarms after scene load
- **Global silence toggle** to mute all resource alarms temporarily

#### Communication Signal Alarm
- **Antenna/CommNet monitoring** warns when signal strength drops
- Configurable threshold (default: 25%)
- Helps prevent loss of control during missions

###  Visual Customization
- **Customizable UI skins** with texture pack support
- **Multiple skin presets** included
- Kerbal-themed UI elements
- Visual alarm indicators on note tabs
- Clean, intuitive interface design

###  Global Alarm Panel
Slide-out panel for managing all alarm systems:
- **Resources Alarms** - Configure resource monitoring
- **Terrain Alarms** - Adjust GPWS/terrain warning settings
- One-click enable/disable for each alarm system
- Persistent configuration across game sessions

---

##  Performance Optimizations (Latest Version)

The latest version includes **major performance improvements** for smooth gameplay:

### Optimizations Implemented
✅ **Cached FindObjectsOfType calls** - 95% reduction in expensive Unity searches  
✅ **Eliminated LINQ allocations** - Manual loops instead of Where().ToArray()  
✅ **Reflection caching** - All MethodInfo/PropertyInfo lookups cached at startup  
✅ **Aircraft type detection caching** - 2-second cache lifetime, 80-95% CPU reduction  
✅ **Resource query optimization** - Uses KSP's built-in GetConnectedResourceTotals()  
✅ **Reusable collections** - Eliminates heap allocations in hot paths  
✅ **Early exit patterns** - Stops unnecessary iterations when threshold met  

---

##  Installation

### Method 1: Release Download (Recommended)
1. Download the latest release from the [Releases page](https://github.com/garyblu71mods/KerbNoteLite/releases)
2. Extract the `KerbNoteLite` folder from the downloaded ZIP file
3. Copy the `KerbNoteLite` folder to your KSP `GameData` directory
4. The final structure should be: `GameData/KerbNoteLite/`
5. Launch KSP and enjoy!

** Important:** Download the pre-built release package, not the source code. The release contains the compiled mod ready to use.

### Method 2: Build from Source
1. Clone the repository: `git clone https://github.com/garyblu71mods/KerbNoteLite.git`
2. Open `KerbNoteLite.csproj` in Visual Studio 2019+
3. Ensure references to KSP assemblies are correct (adjust paths if needed)
4. Build in Release mode (.NET Framework 4.8, C# 7.3)
5. Copy the compiled DLL from `bin/Release/` to `GameData/KerbNoteLite/Plugins/`

---

##  Usage Guide

### Opening KerbNote
- Click the **KerbNote icon** in the application launcher (toolbar)
- The main window will appear with your notes

### Creating and Managing Notes
1. Click the **"+"** button to add a new tab
2. Enter your notes in the text area
3. Use **Tab key** to insert spacing (10 spaces)
4. Use **Ctrl+Z** to undo changes
5. Click the **AAA** button below the note to cycle zoom levels (5 levels)
6. Click the **"X"** on a tab to delete it (confirmation required)

### Using the Settings Panel
A collapsible **Settings** panel is at the bottom of the KerbNote window. Click **Settings** to expand/collapse.

####  Notes (Save Management)
- **Purpose**: Switch between different game saves
- **How to use**:
  1. Click **Notes** button in Settings panel
  2. Select a save from the list (scroll if more than 5)
  3. The mod loads notes and alarms specific to that save
  4. Each save has its own separate data
  5. Click **Back** to return to Settings menu

**Features:**
- Auto-creates note files for each save
- Moves orphaned files (from deleted saves) to `DeletedSaves` folder
- Ensures data integrity across save games

####  Skin (Visual Theme)
- **Purpose**: Change the KerbNote interface appearance
- **How to use**:
  1. Click **Skin** button in Settings panel
  2. Browse available skin packs (scroll if more than 5)
  3. Select a skin to apply immediately
  4. Skin choice is saved per-save
  5. Click **Back** to return to Settings

**Skin locations:**
```
GameData/KerbNoteLite/texture_pack/[SkinName]/Textures/
```

**Skins affect:**
- Window backgrounds and textures
- Button styles (tabs, hover, click)
- Note area appearance
- Mini-note visuals
- Overall color scheme

####  About/Help
- **Purpose**: View documentation and help
- **How to use**:
  1. Click **About/Help** button
  2. Modal window appears with markdown help text
  3. Scroll through content
  4. Click **X** to close

**Help file location:**
```
GameData/KerbNoteLite/About_Help (or About_Help.txt, .md)
```

### Setting Up Location-Based Alarms
1. Create or select a note tab
2. Click the **sidebar** (right edge of window) to open alarm settings
3. Select **target celestial body** from dropdown
4. Select **vessel situation** (Flying, Orbiting, Landed, etc.)
5. Enable desired actions:
   - ☑ **Mini-Note** - Show floating mini-note
   - ☑ **Play Sound** - Play Kerbal vocal
   - ☑ **Stop Warp** - Stop time warp
   - ☑ **Hide on Exit** - Auto-hide mini-note when condition ends
6. Enable the alarm - it will trigger when conditions are met

### Using the Global Alarm Panel
The Global Alarm Panel provides centralized control for advanced alarm systems.

#### Opening the Panel
- Click the **alarm icon** on the left side of the KerbNote window
- Panel slides out from the left edge
- Click again (or click outside) to close

#### Managing Resources Alarms
1. Click **Resources Alarms** in the panel menu
2. Toggle **Enable/Disable** switch at top
3. Configure per-resource settings:
   - **Enable checkbox** - Monitor this resource
   - **Threshold slider** - Warning level (0-100%)
4. Enable **Communication Alarm** if desired
   - Set **signal threshold** for low-signal warnings
5. Click **Silence Alarms** to temporarily mute (doesn't disable monitoring)
6. Click **Back** to return to menu

**Default thresholds:**
- ElectricCharge: 15%
- LiquidFuel/Oxidizer: 15%
- MonoPropellant: 15%
- Ablator: 10%
- XenonGas: 20%

#### Managing Terrain Alarms
1. Click **Terrain Alarms** in the panel menu
2. Toggle **Enable/Disable** switch at top
3. Configure **Base Terrain Warning**:
   - **Altitude AGL** - Warning altitude (default: 750m)
   - **Descent Speed** - Trigger rate (default: -30 m/s)
   - **Enable** checkbox
4. Configure **Terrain Ahead**:
   - **Max Time** - Look-ahead seconds (default: 6s)
   - **Step** - Sample interval (default: 0.25s)
   - **Margin** - Terrain clearance buffer (default: 0m)
   - **Min Speed** - Activation speed (default: 20 m/s)
   - **Enable** checkbox
5. Configure **Gear Warning**:
   - **AGL threshold** (default: 200m)
   - **Max Speed** (default: 200 m/s)
   - **Max Roll** - Ignore when banked (default: 45°)
   - **Enable** checkbox
6. Configure **Sink Rate Warning**:
   - **AGL threshold** (default: 70m)
   - **Min Descent** (default: -7 m/s)
   - **Enable** checkbox
7. Configure **Stall Warning**:
   - **Enable** checkbox
   - **Mode selection**:
     - **AUTO**: Energy decay detection
       - **Descent Ratio**: % threshold (default: 35%)
       - Triggers when descent/horizontal speed ratio exceeds threshold
     - **MANUAL**: Speed threshold
       - **Min Speed**: Minimum safe horizontal speed (default: 50 m/s)
   - **Min Altitude**: Don't alarm below this (default: 100m)
8. Enable **Altitude Callouts** for automated altitude announcements
9. Toggle **Aircraft Only** to restrict alarms to aircraft vessels
10. Adjust **Volume** slider (0-100%) for all terrain sounds
11. Click **Back** to return to menu

#### Landing Suppression Settings
- **Landing Suppress Max Speed**: 150 m/s (default)
- **Landing Suppress Max AGL**: 300 m (default)
- When gear is deployed, slow, and low, terrain warnings are suppressed

### Using Mini-Notes
- Mini-notes automatically appear when alarms trigger
- **Drag** them anywhere on screen
- **Click** to expand to full note view
- Mini-notes persist across scene changes unless **"Hide on Exit"** is enabled
- **Blink animations** indicate alarm triggers:
  - Triple-fast blink: First appearance or re-show
  - Fast blink: Alarm re-triggered while visible

---

##  File Structure

```
KerbNoteLite/
├── src/
│   ├── AlarmManager.cs              # Core location-based alarm system
│   ├── AlarmRunner.cs               # In-flight alarm trigger (optimized)
│   ├── AlarmEditorRunner.cs         # VAB/SPH alarm trigger
│   ├── AlarmSpaceCenterRunner.cs    # Space Center alarm trigger
│   ├── AlarmSelector.cs             # Alarm configuration UI
│   ├── AlarmSystemBootstrap.cs      # Auto-initialize alarms on load
│   ├── GlobalAlarmPanel.cs          # Slide-out alarm control panel
│   ├── GlobalAlarmManager.cs        # Global alarm coordination
│   ├── TerrainAlarmRunner.cs        # GPWS/terrain warning system (optimized)
│   ├── TerrainAlarmConfig.cs        # Terrain alarm persistence
│   ├── ResourcesAlarmRunner.cs      # Resource monitoring (optimized)
│   ├── ResourcesAlarmConfig.cs      # Resource alarm persistence
│   ├── TimeReminderAlarm.cs         # Time-based reminder system
│   ├── KerbNote.UI.*.cs             # UI components
│   ├── MiniNote.cs                  # Mini-note window system
│   ├── SoundManager.cs              # Sound effects (optimized)
│   ├── SkinAssets.cs                # UI texture management
│   └── KerbCalc.cs                  # Built-in calculator
└── GameData/KerbNoteLite/
    ├── Plugins/
    │   └── KerbNoteLite.dll         # Compiled mod
    ├── Textures/                    # UI textures
    ├── Sounds/                      # Audio files
    ├── texture_pack/                # Skin themes
    └── AlarmsAndNotes/              # Per-save data storage
        ├── Notes_[SaveName].txt
        ├── Alarms_[SaveName].txt
        ├── TerrainAlarmConfig.cfg
        └── ResourcesAlarmConfig.cfg
```

---

##  Data Storage

### Notes (Per-Save)
```
GameData/KerbNoteLite/AlarmsAndNotes/Notes_[SaveName].txt
```
Contains all note tabs with content, zoom levels, and tab names.

### Location-Based Alarms (Per-Save)
```
GameData/KerbNoteLite/AlarmsAndNotes/Alarms_[SaveName].txt
```
Stores alarm configurations for each note tab.

### Terrain Alarm Configuration (Global)
```
GameData/KerbNoteLite/AlarmsAndNotes/TerrainAlarmConfig.cfg
```
Persistent settings for GPWS/terrain warning system.

### Resource Alarm Configuration (Global)
```
GameData/KerbNoteLite/AlarmsAndNotes/ResourcesAlarmConfig.cfg
```
Persistent settings for resource monitoring system.

---

##  Alarm Situations Reference

### Vessel Situations
| Situation | Trigger Condition |
|-----------|-------------------|
| **PRELAUNCH** | Vessel on the launchpad |
| **FLYING** | Atmospheric flight |
| **SUB_ORBITAL** | Suborbital trajectory (will impact) |
| **ORBITING** | Stable orbit around body |
| **ESCAPING** | Leaving sphere of influence |
| **LANDED** | On solid ground |
| **SPLASHED** | In water |
| **DOCKED** | Docked to another vessel |

### Special Locations
| Location | Trigger Condition |
|----------|-------------------|
| **VAB** | In Vehicle Assembly Building |
| **SPH** | In Space Plane Hangar |
| **Space Center** | At KSC scene |

---

##  Development

### Requirements
- **.NET Framework 4.8**
- **C# 7.3**
- **Visual Studio 2019+** (or compatible IDE)
- **Kerbal Space Program** (tested on compatible versions)
- **Unity engine** (KSP embedded version)

### Building from Source
1. Clone the repository
2. Open `KerbNoteLite.csproj` in Visual Studio
3. Ensure KSP assembly references are correct:
   - `Assembly-CSharp.dll`
   - `UnityEngine.dll`
   - `UnityEngine.UI.dll`
   - Located in `[KSP]/KSP_Data/Managed/`
4. Build in **Release** mode
5. Output DLL will be in `bin/Release/`

### Contributing
Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Test thoroughly in KSP
5. Submit a pull request

---

##  Known Issues

- Alarm system requires scene changes to detect new situations (by design)
- Mini-notes may overlap if multiple alarms trigger simultaneously
- Terrain alarms may have slight delay during physics settling after load (mitigated by 20s cooldown)
- Resource monitoring uses 8-second throttle to prevent spam

---

##  Troubleshooting

### Alarms Not Triggering
1. Verify alarm is **enabled** in alarm settings
2. Check that you're in the correct **celestial body** and **situation**
3. Ensure **20-second load cooldown** has passed after scene load
4. For terrain alarms: Check **Global Alarm Panel** → **Terrain Alarms** → **Enable/Disable**
5. For resource alarms: Check **Global Alarm Panel** → **Resources Alarms** → **Enable/Disable**

### Performance Issues
- Latest version includes major performance optimizations (see Performance section)
- Disable unused alarm systems in Global Alarm Panel
- Reduce the number of active note tabs
- Lower terrain alarm update frequency (increase "Step" value)

### Notes Not Saving
- Check that `GameData/KerbNoteLite/AlarmsAndNotes/` folder exists
- Verify write permissions for KSP directory
- Ensure save name doesn't contain invalid characters

---

##  License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

### Third-Party Assets
- Sound effects: Original Kerbal Space Program audio (used under fair use)
- UI textures: Custom created or modified from KSP assets

---

##  Credits

**Created by:** garyblu71mods

**Special Thanks:**
- KSP Modding Community
- Unity Engine Team
- All contributors and testers

---

##  Support

For issues, suggestions, or questions:
- **GitHub Issues**: [Open an issue](https://github.com/garyblu71mods/KerbNoteLite/issues)
- **Bug Reports**: Include KSP version, mod version, and reproduction steps
- **Feature Requests**: Describe use case and desired behavior

---

##  Changelog

### Latest Version
- ✨ Added **Terrain Proximity Alarm System** (GPWS-style warnings)
- ✨ Added **Resource Monitoring Alarms** with customizable thresholds
- ✨ Added **Global Alarm Panel** for centralized alarm control
- ✨ Added **Communication Signal Monitoring**
- ✨ Added **Altitude Callouts** during landing
- ✨ Added **Gear Warning System** with roll suppression
- ✨ Added **Sink Rate Warning** for safe landings
- ✨ Added **Stall Warning System** for aerodynamic safety
- ⚡ **Major Performance Optimizations** (80-85% faster)
- 🔧 Cached expensive Unity API calls
- 🔧 Eliminated LINQ allocations in hot paths
- 🔧 Optimized resource queries using KSP built-in methods
- 🔧 Added aircraft type detection caching
- 🔧 Improved reflection performance with caching
- 🐛 Fixed false alarms during scene load (20s cooldown)
- 🐛 Fixed physics settling false positives
- 🐛 Fixed EVA jetpack MonoPropellant monitoring

### Previous Versions
See [CHANGELOG.md](CHANGELOG.md) for full version history.

---

**Happy note-taking and safe flights, Kerbonauts!** 

*Fly safe, monitor your resources, and never miss an alarm!*
