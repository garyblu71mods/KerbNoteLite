# Instrukcja aktualizacji Pull Request

## Problem zosta? zidentyfikowany:
- Tag GitHub ma prefix "v" (`v1.3.0`)
- Poprzednie wersje by?y niespójne
- CKAN potrzebuje jednolitego formatu

## Rozwi?zanie:
Dodano `x_netkan_version_edit` do pliku `.netkan`, który automatycznie usuwa prefix "v" z tagów.

---

## Jak zaktualizowa? PR:

### OPCJA A: GitHub Web (ZALECANE - najszybsze)

1. Id? do swojego forka:
   ```
   https://github.com/TWOJA_NAZWA/NetKAN
   ```

2. Otwórz: `NetKAN/KerbNoteLite.netkan`

3. Kliknij ikon? o?ówka (Edit)

4. Zast?p zawarto?? tym kodem:
```json
{
  "spec_version": "v1.34",
  "identifier": "KerbNoteLite",
  "$kref": "#/ckan/github/garyblu71mods/KerbNoteLite",
  "$vref": "#/ckan/ksp-avc",
  "x_netkan_version_edit": {
    "find": "^v",
    "replace": "",
    "strict": false
  },
  "license": "MIT",
  "category": "Utility",
  "install": [
    {
      "find": "KerbNoteLite",
      "install_to": "GameData"
    }
  ]
}
```

5. Commit:
   - Message: `Fix version prefix handling`
   - Description: `Added x_netkan_version_edit to strip 'v' prefix from GitHub tags for consistent versioning`

6. Kliknij "Commit changes"

7. PR automatycznie si? zaktualizuje!

---

### OPCJA B: Git lokalnie

```bash
# Je?li masz fork sklonowany lokalnie:

# 1. Upewnij si?, ?e jeste? w folderze NetKAN
cd C:\path\to\your\NetKAN

# 2. Skopiuj zaktualizowany plik
copy C:\Users\grzeg\Desktop\KerbCalcAndNote\KerbNoteLite\KerbNoteLite.netkan NetKAN\KerbNoteLite.netkan

# 3. Commit i push
git add NetKAN/KerbNoteLite.netkan
git commit -m "Fix version prefix handling"
git push origin TWOJA_NAZWA_BRANCHA
```

---

## Co zmienia x_netkan_version_edit:

**find**: `"^v"` - Szuka prefiksu "v" na pocz?tku
**replace**: `""` - Zast?puje go pustym stringiem (usuwa)
**strict**: `false` - Nie rzuca b??du, je?li prefix nie istnieje

**Przyk?ad:**
- Tag GitHub: `v1.3.0` ? CKAN wersja: `1.3.0`
- Tag GitHub: `1.2.3` ? CKAN wersja: `1.2.3` (bez zmian)

---

## Po aktualizacji:

Bot NetKAN:
1. Wykryje nowy commit w PR
2. Ponownie sprawdzi plik
3. Zastosuje `x_netkan_version_edit`
4. Utworzy poprawny plik `.ckan`
5. Maintainerzy zatwierdz? PR

---

## Sprawd? status:

Po commit, sprawd? swój PR:
- Bot powinien doda? nowy komentarz
- Status powinien zmieni? si? na zielony ?
- Maintainerzy zmerguj? PR (1-24h)

---

**NAST?PNY KROK: Zaktualizuj plik w swoim forku (Opcja A)**
