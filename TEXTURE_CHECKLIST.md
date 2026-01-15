# TEXTURE CHECKLIST for v1.3.1 Release

## Required Textures - Add to GameData/KerbNoteLite/Textures/

Core UI (5 files):
[ ] Background_window.png
[ ] NoteWindow.png  
[ ] Button.png
[ ] ButtonHover.png
[ ] ButtonClick.png

Tabs (4 files):
[ ] Tab.png
[ ] TabHover.png
[ ] TabClick.png
[ ] TabRed.png

Alarm Bars (2 files):
[ ] Alarm_bar.png (vertical - for right panel)
[ ] Alarm_bar_horizontal.png (horizontal - for bottom panel)

---
TOTAL: 11 texture files required

## Optional: Custom Skin Packs

If you have additional skins, add them to:
GameData/KerbNoteLite/texture_pack/SkinName/Textures/

Example structure:
texture_pack/
├── Green/
│   └── Textures/ (11 files)
├── Blue/
│   └── Textures/ (11 files)
└── Orange/
    └── Textures/ (11 files)

---

## After Adding Textures

1. Delete GameData/KerbNoteLite/Textures/PLACEHOLDER.txt
2. Verify all files are PNG format
3. Create ZIP:
   - Name: KerbNoteLite-v1.3.1.zip
   - Include entire GameData/ folder
   - Include cleanup scripts in root

## ZIP Structure

KerbNoteLite-v1.3.1.zip
├── cleanup_old_versions.bat
├── cleanup_old_versions.ps1
├── INSTALL.txt
├── LICENSE
├── CHANGELOG.md
└── GameData/
    └── KerbNoteLite/
        ├── Plugins/
        ├── Textures/ (add your 11 PNG files here!)
        ├── texture_pack/
        └── Sounds/

---

Ready for release after textures added! ✅
