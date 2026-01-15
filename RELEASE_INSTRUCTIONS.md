# 🎉 RELEASE v1.3.1 - READY FOR TEXTURES

## ✅ Co zostało przygotowane:

### 📦 Struktura GameData/ (gotowa):
```
GameData/
├── cleanup_old_versions.bat       ✅ Skrypt cleanup
├── cleanup_old_versions.ps1       ✅ Skrypt cleanup (PowerShell)
├── INSTALL.txt                    ✅ Instrukcje instalacji
├── LICENSE                        ✅ Licencja MIT
├── CHANGELOG.md                   ✅ Historia zmian
└── KerbNoteLite/
    ├── Plugins/
    │   └── KerbNote_V1.3.1.dll    ✅ DLL (v1.3.1.0)
    ├── Textures/
    │   ├── README.md              ✅ Dokumentacja tekstur
    │   └── PLACEHOLDER.txt        ⚠️ USUŃ PO DODANIU TEKSTUR
    ├── texture_pack/
    │   └── README.md              ✅ Dokumentacja skinów
    ├── Sounds/
    │   ├── README.md              ✅ Dokumentacja dźwięków
    │   └── PLACE_STALL_OGG_HERE.txt ✅
    ├── KerbNoteLite.version       ✅ CKAN metadata
    ├── About_Help.md              ✅ Pomoc w grze
    └── README.txt                 ✅ README dla użytkowników
```

---

## 📋 CO MUSISZ ZROBIĆ:

### 1. Dodaj tekstury (11 plików PNG):

**Skopiuj do:** `GameData/KerbNoteLite/Textures/`

**Core UI (5):**
- [ ] Background_window.png
- [ ] NoteWindow.png
- [ ] Button.png
- [ ] ButtonHover.png
- [ ] ButtonClick.png

**Tabs (4):**
- [ ] Tab.png
- [ ] TabHover.png
- [ ] TabClick.png
- [ ] TabRed.png

**Alarm Bars (2):**
- [ ] Alarm_bar.png (vertical)
- [ ] Alarm_bar_horizontal.png (horizontal)

### 2. (Opcjonalnie) Dodaj skórki:

**Struktura:**
```
GameData/KerbNoteLite/texture_pack/
├── Green/
│   └── Textures/ (11 plików PNG)
├── Blue/
│   └── Textures/ (11 plików PNG)
└── Orange/
    └── Textures/ (11 plików PNG)
```

### 3. Usuń placeholder:
```
Usuń: GameData/KerbNoteLite/Textures/PLACEHOLDER.txt
```

---

## 🚀 TWORZENIE RELEASE ZIP:

### Opcja A: Automatyczny skrypt (zalecane)
```cmd
create_release_zip.bat
```
✅ Sprawdza czy tekstury są dodane
✅ Tworzy `KerbNoteLite-v1.3.1.zip`
✅ Gotowy do uploadu na GitHub

### Opcja B: Ręcznie
1. Spakuj cały folder `GameData/` jako ZIP
2. Nazwij: `KerbNoteLite-v1.3.1.zip`
3. Upewnij się że struktura w ZIP to:
   ```
   KerbNoteLite-v1.3.1.zip
   ├── cleanup_old_versions.bat
   ├── cleanup_old_versions.ps1
   ├── INSTALL.txt
   ├── LICENSE
   ├── CHANGELOG.md
   └── GameData/
       └── KerbNoteLite/
   ```

---

## 📤 PUBLIKACJA RELEASE:

### GitHub Release (https://github.com/garyblu71mods/KerbNoteLite/releases/tag/v1.3.1):

1. **Edytuj release** (lub stwórz nowy)
2. **Usuń stary ZIP** (jeśli był)
3. **Upload:** `KerbNoteLite-v1.3.1.zip`
4. **Zaktualizuj opis** (dodaj sekcję o cleanup scripts)
5. **Publish release**

### Opis release do skopiowania:
```markdown
## 🚀 KerbNoteLite v1.3.1

### ✨ New Features
- Stall Warning altitude settings UI
- Tooltips for min/max altitude configuration

### 🔧 Fixed
- KerbVision compatibility (selective InputLock)
- Hide on Exit alarm premature hiding

### 🧹 For Manual Installation
**IMPORTANT:** Run `cleanup_old_versions.bat` before installing.
CKAN users: Update automatically (no action needed).

See full changelog: [CHANGELOG.md](https://github.com/garyblu71mods/KerbNoteLite/blob/main/CHANGELOG.md)
```

---

## ✅ CHECKLIST FINALNY:

- [ ] Tekstury dodane (11 plików PNG)
- [ ] PLACEHOLDER.txt usunięty
- [ ] `create_release_zip.bat` uruchomiony
- [ ] ZIP przetestowany (rozpakuj i sprawdź w KSP)
- [ ] ZIP uploadowany na GitHub
- [ ] Release notes zaktualizowane
- [ ] CKAN będzie automatycznie wykrywać (poczekaj 1-3 dni)

---

## 🎯 PLIKI HELPER:

- `TEXTURE_CHECKLIST.md` - Lista tekstur do dodania
- `create_release_zip.bat` - Automatyczne tworzenie ZIP
- `cleanup_old_versions.bat` - Dla użytkowników (już w GameData/)

---

## 📞 POTRZEBUJESZ POMOCY?

Jeśli masz pytania:
1. Sprawdź `TEXTURE_CHECKLIST.md`
2. Zobacz strukturę w `GameData/KerbNoteLite/Textures/README.md`
3. Pytaj mnie! 🤖

---

**Status:** ⏳ Czeka na tekstury
**Następny krok:** Dodaj 11 plików PNG do `GameData/KerbNoteLite/Textures/`

Powodzenia! 🚀
