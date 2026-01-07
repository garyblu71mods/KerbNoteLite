# KROK PO KROKU: Aktualizacja KerbNoteLite v1.3.0 w CKAN

## ? Pliki przygotowane:

1. **KerbNoteLite_v1.3.0.zip** - Paczka do za??czenia w release
2. **KerbNoteLite.ckan** - Metadane CKAN
3. **KerbNoteLite-1.3.0.ckan** - Wersja dla CKAN-meta repo
4. **KerbNoteLite.netkan** - Automatyczna integracja NetKAN
5. **GITHUB_RELEASE_v1.3.0.md** - Opis release

---

## KROK 1: Publikacja Release na GitHubie (MUSISZ ZROBI? TERAZ)

### A. Przejd? do:
```
https://github.com/garyblu71mods/KerbNoteLite/releases/new
```

### B. Wype?nij formularz:

**Tag version:**
```
v1.3.0
```

**Release title:**
```
KerbNoteLite v1.3.0 - Major Performance Update
```

**Description:**
Otwórz plik `GITHUB_RELEASE_v1.3.0.md` i skopiuj CA?? zawarto?? (Ctrl+A, Ctrl+C)

### C. Za??cz plik:
1. Kliknij "Attach binaries by dropping them here or selecting them"
2. Wybierz plik: `KerbNoteLite_v1.3.0.zip` (jest w g?ównym folderze projektu)

### D. Publikuj:
- Zaznacz ? "Set as the latest release"
- Kliknij **"Publish release"**

---

## KROK 2: Zg?oszenie do CKAN (WYBIERZ METOD?)

### METODA A: Automatyczna przez NetKAN (ZALECANE - NAJ?ATWIEJSZA)

1. **Fork repozytorium NetKAN:**
   - Id? do: https://github.com/KSP-CKAN/NetKAN
   - Kliknij przycisk **"Fork"** (góra-prawo)

2. **Dodaj plik:**
   - W swoim forku, przejd? do folderu `NetKAN/`
   - Kliknij **"Add file"** ? **"Upload files"**
   - Przeci?gnij plik `KerbNoteLite.netkan` (z folderu projektu)
   - Commit message: `Add KerbNoteLite NetKAN metadata`
   - Kliknij **"Commit changes"**

3. **Utwórz Pull Request:**
   - Kliknij **"Contribute"** ? **"Open pull request"**
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
     ```
   - Kliknij **"Create pull request"**

4. **Czekaj na zatwierdzenie:**
   - Bot automatycznie zwaliduje (1-5 minut)
   - Maintainerzy zatwierdz? (1-24 godziny)
   - Mod pojawi si? w CKAN!

---

### METODA B: Manualna przez CKAN-meta (bardziej skomplikowana)

1. **Fork repozytorium CKAN-meta:**
   - Id? do: https://github.com/KSP-CKAN/CKAN-meta
   - Kliknij **"Fork"**

2. **Dodaj plik:**
   - Przejd? do folderu `KerbNoteLite/` (je?li nie istnieje, utwórz go)
   - Kliknij **"Add file"** ? **"Upload files"**
   - Przeci?gnij plik `KerbNoteLite-1.3.0.ckan`
   - Commit: `Update KerbNoteLite to v1.3.0`

3. **Pull Request:**
   - Title: `Update KerbNoteLite to v1.3.0`
   - Create PR

---

### METODA C: Discord CKAN (najszybsza dla maintainerów)

1. Do??cz do Discord: https://discord.gg/Y9vFGvy
2. Id? do kana?u: `#ckan-development`
3. Napisz:
   ```
   Hello! I've released KerbNoteLite v1.3.0 with major updates:
   - Terrain Proximity Alarms (GPWS)
   - Resource Monitoring
   - 80% performance improvement
   
   Release: https://github.com/garyblu71mods/KerbNoteLite/releases/tag/v1.3.0
   
   Could someone update CKAN metadata? Thanks!
   ```

---

## KROK 3: Weryfikacja (po 1-24h)

1. **Sprawd? w CKAN client:**
   - Otwórz CKAN
   - Kliknij "Refresh"
   - Wyszukaj "KerbNoteLite"
   - Powinna pojawi? si? wersja 1.3.0

2. **Sprawd? na stronie:**
   - https://spacedock.info/mod/TWOJ_MOD_ID
   - Zaktualizuj tam równie? opis (opcjonalnie)

---

## FAQ

**Q: Jak d?ugo trwa aktualizacja w CKAN?**
A: 1-24 godziny po zatwierdzeniu Pull Request

**Q: Co je?li mój PR zostanie odrzucony?**
A: Bot poka?e b??dy - popraw plik .netkan i zaktualizuj PR

**Q: Czy musz? usun?? stare wersje?**
A: Nie, CKAN automatycznie zarz?dza wersjami

**Q: Jak sprawdzi?, czy release jest poprawny?**
A: URL powinien by? dost?pny:
```
https://github.com/garyblu71mods/KerbNoteLite/releases/download/v1.3.0/KerbNoteLite_v1.3.0.zip
```

---

## Podsumowanie kroków:

1. ? Przygotowane pliki (DONE)
2. ? Publikacja release na GitHubie (DO NOW)
3. ? Fork NetKAN repo (DO AFTER RELEASE)
4. ? Dodaj KerbNoteLite.netkan (DO AFTER FORK)
5. ? Utwórz Pull Request (DO AFTER ADD)
6. ? Czekaj na zatwierdzenie (1-24h)

---

**ZACZNIJ OD KROKU 1 - PUBLIKACJA RELEASE!**

Po opublikowaniu release, wró? tutaj i wykonaj Krok 2 (metoda A).
