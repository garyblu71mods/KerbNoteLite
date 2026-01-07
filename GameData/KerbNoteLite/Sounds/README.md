# Sound Files Directory

This directory contains audio files for KerbNoteLite alarms.

## Required Files

### Stall Warning (NEW in v1.4.0)
- **File name:** `Stall.ogg` or `stall.ogg`
- **Format:** Ogg Vorbis audio
- **Purpose:** "STALL!" or "SPEED! SPEED!" warning callout
- **Fallback:** If missing, generates 800Hz beep (0.15s)

### Other Sound Files (Optional)
- `Sink_Rate.ogg` - Sink rate warning
- `Landed.ogg` - Landing confirmation
- `Pull_Up.ogg` - Terrain pull-up warning
- `Too_Low_Gear.ogg` - Gear warning
- Various altitude callouts: `200.ogg`, `100.ogg`, `50.ogg`, etc.

## Installation Instructions
See `STALL_SOUND_INSTALLATION.md` in the root directory for detailed setup.

## Audio File Specifications
- **Format:** Ogg Vorbis (`.ogg`)
- **Sample Rate:** 44.1kHz recommended
- **Channels:** Mono or stereo
- **Duration:** 1-2 seconds for warnings, shorter for callouts
- **Encoding:** Variable bitrate (VBR) quality 5-8

## Notes
- All sound files are optional - the system will use fallback beeps if files are missing
- File names are case-insensitive on Windows, case-sensitive on Linux/Mac
- Place your `Stall.ogg` file here and restart KSP to activate custom sound
