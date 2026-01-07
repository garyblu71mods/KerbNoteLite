# Instrukcja aktualizacji CKAN do v1.3.0

## Metoda 1: Automatyczna (NetKAN)

1. Upewnij si?, ?e release v1.3.0 jest opublikowany na GitHubie
2. Plik `KerbNoteLite.netkan` zosta? ju? utworzony
3. Zrób fork repozytorium CKAN NetKAN:
   ```
   https://github.com/KSP-CKAN/NetKAN
   ```

4. Dodaj plik `KerbNoteLite.netkan` do folderu `NetKAN/`

5. Utwórz Pull Request z tytu?em:
   ```
   Add KerbNoteLite v1.3.0
   ```

6. Bot automatycznie zwaliduje i zindeksuje mod

## Metoda 2: Manualna aktualizacja

1. Stwórz release v1.3.0 na GitHubie z plikiem `KerbNoteLite_v1.3.0.zip`

2. Zrób fork repozytorium CKAN:
   ```
   https://github.com/KSP-CKAN/CKAN-meta
   ```

3. Dodaj plik `KerbNoteLite-1.3.0.ckan` do folderu `KerbNoteLite/`

4. Utwórz Pull Request

## Kroki do wykonania TERAZ:

### 1. Publikacja release na GitHubie

Przejd? do: https://github.com/garyblu71mods/KerbNoteLite/releases/new

**Formularz:**
- Tag: `v1.3.0`
- Title: `KerbNoteLite v1.3.0 - Major Performance Update`
- Description: Skopiuj z `GITHUB_RELEASE_v1.3.0.md`
- Za??cz: `KerbNoteLite_v1.3.0.zip` (ju? utworzony w g?ównym folderze projektu)

### 2. Zg?oszenie do CKAN

Po opublikowaniu release:

**Opcja A - Automatyczna:**
1. Fork: https://github.com/KSP-CKAN/NetKAN
2. Dodaj `KerbNoteLite.netkan` do folderu `NetKAN/`
3. Commit: "Add KerbNoteLite NetKAN metadata"
4. Pull Request do g?ównego repo

**Opcja B - Manualna:**
1. Fork: https://github.com/KSP-CKAN/CKAN-meta
2. Skopiuj `KerbNoteLite.ckan` jako `KerbNoteLite-1.3.0.ckan`
3. Dodaj do folderu `KerbNoteLite/`
4. Commit: "Update KerbNoteLite to v1.3.0"
5. Pull Request do g?ównego repo

### 3. Alternatywa - Discord CKAN

Je?li nie chcesz robi? PR, mo?esz zg?osi? update na Discord CKAN:
- https://discord.gg/Y9vFGvy
- Kana?: #ckan-development
- Podaj link do release v1.3.0

## Pliki gotowe do u?ycia:

- ? `KerbNoteLite_v1.3.0.zip` - plik do za??czenia w release
- ? `KerbNoteLite.ckan` - metadane dla CKAN (wersja 1.3.0)
- ? `KerbNoteLite.netkan` - plik dla automatycznej integracji NetKAN
- ? `GITHUB_RELEASE_v1.3.0.md` - opis do skopiowania w release

## Oczekiwany czas:

- Release na GitHubie: 5 minut
- Pull Request do CKAN: 10 minut
- Zatwierdzenie przez CKAN: 1-24 godziny (automatycznie przez bota)

Po zatwierdzeniu PR, mod pojawi si? w CKAN dla wszystkich u?ytkowników!
