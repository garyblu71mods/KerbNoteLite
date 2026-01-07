# ? POPRAWIONY LINK DO CKAN v1.3.0

## Problem zosta? naprawiony!

**Rzeczywisty link do pobrania:**
```
https://github.com/garyblu71mods/KerbNoteLite/releases/download/v1.3.0/KerbNoteLite.zip
```

**Zaktualizowane pliki:**
- ? `KerbNoteLite.ckan` - poprawiony URL download
- ? `KerbNoteLite-1.3.0.ckan` - zaktualizowana kopia
- ? `KerbNoteLite.netkan` - ju? u?ywa automatycznego $kref (nie wymaga zmiany)

---

## ?? CO ZROBI? TERAZ:

### KROK 1: Sprawd? link
Otwórz w przegl?darce i sprawd?, czy pobiera plik:
```
https://github.com/garyblu71mods/KerbNoteLite/releases/download/v1.3.0/KerbNoteLite.zip
```

? Je?li plik si? pobiera - przejd? do KROKU 2

---

### KROK 2: Fork NetKAN i dodaj plik

1. **Fork repozytorium:**
   https://github.com/KSP-CKAN/NetKAN
   
2. **W swoim forku, dodaj plik do folderu `NetKAN/`:**
   - Plik: `KerbNoteLite.netkan` (z folderu projektu)
   - Commit message: `Add KerbNoteLite NetKAN metadata`

3. **Utwórz Pull Request:**
   - Title: `Add KerbNoteLite v1.3.0`
   - Description:
   ```
   Updates KerbNoteLite to version 1.3.0

   New features:
   - Terrain Proximity Alarms (GPWS)
   - Resource Monitoring System
   - Global Alarm Panel
   - 80-85% performance improvement

   Release: https://github.com/garyblu71mods/KerbNoteLite/releases/tag/v1.3.0
   Download: https://github.com/garyblu71mods/KerbNoteLite/releases/download/v1.3.0/KerbNoteLite.zip
   ```

---

### KROK 3: Czekaj na zatwierdzenie (1-24h)

Bot NetKAN:
1. Zwaliduje plik `KerbNoteLite.netkan`
2. Pobierze ZIP z podanego linku
3. Utworzy plik `.ckan` automatycznie
4. Doda do CKAN-meta

Po zatwierdzeniu, mod b?dzie dost?pny w CKAN!

---

## ?? Alternatywa: Discord CKAN

Je?li wolisz szybsz? pomoc:

1. Discord: https://discord.gg/Y9vFGvy
2. Kana?: `#ckan-development`
3. Napisz:
```
Hi! I've released KerbNoteLite v1.3.0 with major updates.

Release: https://github.com/garyblu71mods/KerbNoteLite/releases/tag/v1.3.0
Download: https://github.com/garyblu71mods/KerbNoteLite/releases/download/v1.3.0/KerbNoteLite.zip

Could someone help with CKAN indexing? Thanks!
```

---

## ? Checklist:

- [x] Release v1.3.0 opublikowany
- [x] Plik ZIP dost?pny: `KerbNoteLite.zip`
- [x] Link poprawiony w metadanych CKAN
- [x] Plik `KerbNoteLite.netkan` gotowy
- [ ] Fork NetKAN repo
- [ ] Dodanie pliku .netkan
- [ ] Utworzenie Pull Request
- [ ] Zatwierdzenie przez maintainerów (1-24h)

---

**NAST?PNY KROK: Fork NetKAN i dodaj plik**

**URL:** https://github.com/KSP-CKAN/NetKAN

**Plik do dodania:** `KerbNoteLite.netkan`

**Folder docelowy:** `NetKAN/`
