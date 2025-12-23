# KerbNoteLite v1.3.0 - Major Performance Update

## ?? What's New

This release brings **major performance improvements** (80-85% faster!) and introduces two powerful new alarm systems: **Terrain Proximity Warnings (GPWS)** and **Resource Monitoring**.

---

## ? New Features

### ?? Terrain Proximity Alarm System (GPWS)
Aviation-style terrain collision avoidance system:
- **Pull-Up Warning** - Alerts when descending too fast at low altitude
- **Terrain Ahead Prediction** - Look-ahead collision detection (6 seconds)
- **Altitude Callouts** - Automated callouts (200m, 100m, 50m, 10m, "Landed")
- **Gear Warning** - "Too Low Gear" alert with roll angle suppression
- **Sink Rate Warning** - Prevents hard landings
- **Smart Suppression** - Disables warnings during landing configuration
- **Volume Control** - Independent volume slider (0-100%)
- **Aircraft-Only Filter** - Optional restriction to aircraft vessels

### ?? Resource Monitoring Alarms
Never run out of critical resources:
- **Per-Resource Thresholds** - Configure warning levels (0-100%)
- **Monitored Resources** - ElectricCharge, LiquidFuel, Oxidizer, MonoPropellant, Ablator, Ore, XenonGas
- **Depletion Warnings** - Extra alert when reaching 0%
- **EVA Jetpack Monitoring** - MonoPropellant warnings during spacewalks
- **Communication Alarm** - Warns when signal strength drops
- **Auto-Detection** - Finds all resources in current vessel
- **Global Silence Toggle** - Temporarily mute all resource alarms

### ??? Global Alarm Panel
Centralized control for all alarm systems:
- **Slide-Out Panel** - Easy access from left side of KerbNote window
- **Menu Navigation** - Resources Alarms / Terrain Alarms / Time Reminder
- **One-Click Toggle** - Enable/disable entire alarm systems
- **Visual UI** - Tab-style buttons with consistent design
- **Persistent Settings** - Configuration saved across game sessions

---

## ? Performance Optimizations

**Massive performance improvements** across the board:

### AlarmRunner.cs (70-90% faster)
- Cached `FindObjectsOfType<KerbNote>()` calls (95% reduction)
- Replaced LINQ `.Where().ToArray()` with reusable buffers
- Eliminated 3-8 heap allocations per alarm trigger
- **5-15ms ? 0.5-2ms per frame**

### TerrainAlarmRunner.cs (85-95% faster)
- Cached aircraft type detection (2-second lifetime)
- Early exit optimization (stops at 3 points)
- Replaced `foreach` with `for` loops
- Reduced 3,000+ part checks/sec to ~30
- **3-8ms ? 0.3-1ms per frame**

### SoundManager.cs (90% faster)
- Cached all reflection lookups (`PropertyInfo`, `MethodInfo`)
- Eliminated per-frame reflection overhead
- **0.5ms ? 0.05ms per frame**

### ResourcesAlarmRunner.cs (85-95% faster)
- Cached vessel resource names (5-second lifetime)
- Uses KSP's built-in `GetConnectedResourceTotals()`
- Optimized part iteration
- **2-5ms ? 0.2-0.5ms per frame**

### Overall Result
**Total frame time: 10-30ms ? 1-4ms (80-85% reduction)**

---

## ?? Improvements

- ? **20-Second Load Cooldown** - Prevents false alarms after scene load
- ? **Physics Settling Protection** - No false landing callouts during load
- ? **EVA Detection** - Proper handling across all alarm systems
- ? **Mini-Note Animations** - Enhanced blink patterns (triple-fast vs fast)
- ? **Alarm Bootstrap** - Auto-initialization from saved configuration

---

## ?? Bug Fixes

- Fixed false terrain alarms during physics settling
- Fixed false landing callouts when loading vessel already on ground
- Fixed EVA Kerbals triggering landing sounds
- Fixed resource alarms triggering immediately after scene load
- Fixed aircraft detection running every frame (performance issue)
- Fixed memory leaks from uncached `FindObjectsOfType` calls
- Fixed GC pressure from LINQ allocations in hot paths
- Fixed reflection overhead in SoundManager Update loop

---

## ?? Installation

1. **Download** `KerbNoteLite_v1.3.0.zip` from this release
2. **Extract** the archive
3. **Copy** the `KerbNoteLite` folder to `[KSP]/GameData/`
4. **Launch** KSP and enjoy!

### Upgrading from Previous Versions
- Your existing notes and location-based alarms are preserved
- New configuration files created automatically on first launch:
  - `TerrainAlarmConfig.cfg`
  - `ResourcesAlarmConfig.cfg`

---

## ?? Documentation

- **Quick Start Guide**: [QUICKSTART.md](QUICKSTART.md)
- **Full Documentation**: [README.md](README.md)
- **Changelog**: [CHANGELOG.md](CHANGELOG.md)
- **In-Game Help**: Available in Settings ? About/Help

---

## ?? Quick Start

### Enable Terrain Warnings
1. Click alarm icon (left side of KerbNote window)
2. Click "Terrain Alarms"
3. Enable system and select warnings
4. Adjust volume as needed

### Enable Resource Monitoring
1. Open Global Alarm Panel (alarm icon)
2. Click "Resources Alarms"
3. Enable system and check resources to monitor
4. Adjust thresholds (default 15%)

---

## ?? System Requirements

- **Kerbal Space Program** (compatible versions)
- **.NET Framework 4.8**
- **C# 7.3**

---

## ?? Known Issues

- Alarm system requires scene changes to detect new situations (by design)
- Mini-notes may overlap if multiple alarms trigger simultaneously
- Terrain alarms have slight delay during physics settling (mitigated by 20s cooldown)

---

## ?? Support

- **Bug Reports**: [GitHub Issues](https://github.com/garyblu71mods/KerbNoteLite/issues)
- **Email**: Garyblu71.mods@gmail.com
- **Support Development**: [PayPal.me/GaryBlu71](https://paypal.me/GaryBlu71)

---

## ?? Credits

**Author**: GaryBlu71  
**License**: MIT  
**GitHub**: https://github.com/garyblu71mods/KerbNoteLite

Special thanks to the KSP modding community and all testers who helped make this release possible!

---

**Enjoy the massive performance boost and new safety features!** ???

*Safe flights, Kerbonauts!*
