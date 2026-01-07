# Stall Warning Sound Installation

## Required File
The Stall Warning system requires an audio file to play the "SPEED! SPEED!" or "STALL!" callout.

## Installation Steps

1. **Prepare your audio file:**
   - Format: `.ogg` (Ogg Vorbis)
   - Recommended duration: 1-2 seconds
   - Naming: `Stall.ogg` or `stall.ogg`
   - Quality: 44.1kHz, mono or stereo

2. **Create the Sounds directory:**
   ```
   GameData/KerbNoteLite/Sounds/
   ```

3. **Copy your audio file:**
   - Place `Stall.ogg` into the Sounds directory
   - Final path: `GameData/KerbNoteLite/Sounds/Stall.ogg`

4. **Directory structure:**
   ```
   GameData/
   ??? KerbNoteLite/
       ??? About_Help.md
       ??? Sounds/
       ?   ??? Stall.ogg          ? Your stall warning sound
       ?   ??? Sink_Rate.ogg      (if you have it)
       ?   ??? Landed.ogg         (if you have it)
       ?   ??? ... other sounds
       ??? KerbNoteLite.dll
   ```

## Fallback Behavior
If the audio file is not found, the system will automatically generate a fallback beep sound (800Hz, 0.15s duration) so the warning still works.

## Testing
1. Enable Stall Warning in Global Alarm Panel ? Terrain Alarms
2. Take off in an aircraft
3. Reduce throttle and pitch up to test the warning
4. You should hear your custom sound when conditions are met

## Supported File Names
The system will try to load in this order:
1. `KerbNoteLite/Sounds/Stall`
2. `KerbNoteLite/Sounds/stall`
3. `KerbNoteLite/Sounds/STALL`
4. Fallback: Generated beep

## Notes
- The file extension `.ogg` is added automatically by KSP's GameDatabase
- Case sensitivity depends on your operating system
- Make sure the file is not corrupted (test it in an audio player first)
