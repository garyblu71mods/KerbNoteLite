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
- **Per-save data storage** in `GameData/KerbNoteLite/AlarmsAndNotes/`
- Compatible with **.NET Framework 4.8** and **C# 7.3**

## Installation

1. Download the latest release from the [Releases page](https://github.com/garyblu71mods/KerbNoteLite/releases)
2. Extract the `KerbNoteLite` folder from the downloaded ZIP file
3. Copy the `KerbNoteLite` folder to your KSP `GameData` directory
4. The final structure should be: `GameData/KerbNoteLite/`
5. Launch KSP and enjoy!

**Note:** Download the pre-built release package, not the source code. The release contains the compiled mod ready to use.

## Usage

### Opening KerbNote
- Use the application launcher button or assigned hotkey to open the main window

### Creating Notes
1. Click the **"+"** button to add a new tab
2. Enter your notes in the text area
3. Use **Tab** to insert spacing (10 spaces)
4. Use **Ctrl+Z** to undo changes
5. Click the **AAA** button below the note to change zoom level

### Using the Settings Panel
A collapsible **Settings** panel is available at the bottom of the KerbNote window. Click the **Settings** bar to expand/collapse it.

#### Notes Option
- **Purpose**: Switch between different game saves
- **How it works**:
  1. Click **Notes** button in the Settings panel
  2. Select a save from the list (up to 5 visible, scroll for more)
  3. The mod will load notes and alarms specific to that save
  4. Each save has its own separate notes and alarm configurations
  5. Click **Back** to return to Settings menu

**Features:**
- Automatically creates note files for each save with default green skin
- Moves orphaned files (from deleted saves) to `DelatedSaves` folder
- Ensures data integrity across save games

#### Skin Option
- **Purpose**: Change the visual theme of the KerbNote interface
- **How it works**:
  1. Click **Skin** button in the Settings panel
  2. Browse available skin packs (up to 5 visible, scroll for more)
  3. Select a skin to apply it immediately
  4. The skin choice is saved in your notes header
  5. Click **Back** to return to Settings menu

**Available skins** are located in:
```
GameData/KerbNoteLite/texture_pack/[SkinName]/Textures/
```

**Skin affects:**
- Window backgrounds and textures
- Button styles (tabs, hover, click states)
- Note area appearance
- Mini-note visual style
- Overall color scheme

#### About/Help Option
- **Purpose**: View documentation and help information
- **How it works**:
  1. Click **About/Help** button in the Settings panel
  2. A modal window appears with markdown-formatted help text
  3. Scroll through the content using the scroll bar
  4. Click **X** button to close

**Help file** is loaded from:
```
GameData/KerbNoteLite/About_Help (or About_Help.txt, .md)
```

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
KerbNoteLite/
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
└── GameData/KerbNoteLite/
    └── AlarmsAndNotes/            # Per-save data storage
```

## Data Storage

### Notes
Notes are stored per-save in:
```
GameData/KerbNoteLite/AlarmsAndNotes/Notes_[SaveName].txt
```

### Alarms
Alarms are stored per-save in:
```
GameData/KerbNoteLite/AlarmsAndNotes/Alarms_[SaveName].txt
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
1. Open `KerbNoteLite.csproj` in Visual Studio
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
