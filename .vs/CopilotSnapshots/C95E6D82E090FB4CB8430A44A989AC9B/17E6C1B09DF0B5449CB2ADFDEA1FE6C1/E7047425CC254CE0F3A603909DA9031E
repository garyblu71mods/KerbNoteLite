# KerbNoteLite

KerbNoteLite is a comprehensive note-taking and alarm system mod for Kerbal Space Program that helps you manage your missions with in-game notes, mini-notes, and location-based alarms.

## Features

### 📝 Note-Taking System
- **Multi-tab notebook** with unlimited tabs for organizing your notes
- **Rich text formatting** with adjustable zoom levels (5 levels)
- **Undo support** (Ctrl+Z) for text editing
- **Tab insertion** (10 spaces) for formatted notes
- **Auto-scroll** to cursor while typing
- **Draggable window** with resizable interface
- **Per-save note persistence** - different notes for each game save

### 🔔 Alarm System
- **Location-based alarms** that trigger when entering specific situations
- Alarm triggers include:
  - Celestial body arrival (planets, moons)
  - Vessel situations (Orbiting, Flying, Landed, Splashed, etc.)
  - Editor scenes (VAB/SPH)
  - Space Center
- **Alarm actions:**
  - Show mini-note popup
  - Play Kerbal vocal sound
  - Stop time warp automatically
  - Auto-hide on scene exit
- **Per-tab alarms** - assign different alarms to different note tabs

### 📌 Mini-Notes
- **Floating mini-note windows** for quick reference
- Draggable and independently positionable
- Blinking animations when triggered by alarms
- Can be spawned automatically by alarms
- Quick access to full notes

### 🎨 Visual Features
- **Customizable UI** with texture support
- **Kerbal-themed UI elements** 
- Visual alarm indicators on note tabs
- AAA button for quick zoom changes
- Clean, intuitive interface design

### ⚙️ Technical Features
- **GUID-based tab system** for reliable tab tracking
- **Automatic alarm cleanup** removes orphaned alarms
- **Legacy data migration** from older versions
- **Per-save data storage** in `GameData/KerbCalcProject/AlarmsAndNotes/`
- Compatible with **.NET Framework 4.8** and **C# 7.3**

## Installation

1. Download the latest release
2. Extract the contents to your KSP `GameData` folder
3. The mod structure should be: `GameData/KerbCalcProject/`
4. Launch KSP and enjoy!

## Usage

### Opening KerbNote
- Use the application launcher button or assigned hotkey to open the main window

### Creating Notes
1. Click the **"+"** button to add a new tab
2. Enter your notes in the text area
3. Use **Tab** to insert spacing (10 spaces)
4. Use **Ctrl+Z** to undo changes
5. Click the **AAA** button below the note to change zoom level

### Setting Up Alarms
1. Create or select a note tab
2. Click the side bar to open alarm settings
3. Select target body and situation
4. Enable desired actions (Mini-Note, Sound, Stop Warp)
5. Enable the alarm and it will trigger when conditions are met

### Using Mini-Notes
- Mini-notes automatically appear when alarms trigger
- Drag them anywhere on screen
- Click to expand to full note view
- Mini-notes persist across scene changes unless "Hide on Exit" is enabled

## File Structure

```
KerbCalcProject/
├── src/
│   ├── AlarmManager.cs           # Core alarm management system
│   ├── AlarmRunner.cs             # In-flight alarm trigger system
│   ├── AlarmEditorRunner.cs       # VAB/SPH alarm trigger system
│   ├── AlarmSpaceCenterRunner.cs  # Space Center alarm trigger
│   ├── AlarmSelector.cs           # Alarm configuration UI
│   ├── KerbNote.UI.*.cs          # UI components
│   ├── MiniNote.cs                # Mini-note window system
│   ├── SoundManager.cs            # Sound effects
│   └── SkinAssets.cs              # UI texture management
└── GameData/KerbCalcProject/
    └── AlarmsAndNotes/            # Per-save data storage
```

## Data Storage

### Notes
Notes are stored per-save in:
```
GameData/KerbCalcProject/AlarmsAndNotes/Notes_[SaveName].txt
```

### Alarms
Alarms are stored per-save in:
```
GameData/KerbCalcProject/AlarmsAndNotes/Alarms_[SaveName].txt
```

## Alarm Situations

The following vessel situations can trigger alarms:
- **PRELAUNCH** - Vessel on the pad
- **FLYING** - Atmospheric flight
- **ORBITING** - Stable orbit
- **SUB_ORBITAL** - Suborbital trajectory
- **ESCAPING** - Leaving sphere of influence
- **LANDED** - On solid ground
- **SPLASHED** - In water
- **DOCKED** - Docked to another vessel

Special locations:
- **VAB/SPH** - In the Vehicle Assembly Building or Space Plane Hangar
- **Space Center** - At KSC scene

## Development

### Building
1. Open `KerbCalcProject.csproj` in Visual Studio
2. Ensure references to KSP assemblies are correct
3. Build in Release mode
4. Output DLL will be in `bin/Release/`

### Requirements
- .NET Framework 4.8
- C# 7.3
- Kerbal Space Program (tested on compatible versions)
- Unity engine (KSP embedded version)

## Known Issues

- Alarm system requires scene changes to detect new situations
- Mini-notes may overlap if multiple alarms trigger simultaneously


## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

Created by garyblu71mods

## Support

For issues, suggestions, or questions:
- Open an issue on the [GitHub repository](https://github.com/garyblu71mods/KerbNoteLite)
- Provide KSP version, mod version, and reproduction steps for bugs


---

**Happy note-taking and safe flights, Kerbonauts!** 🚀
