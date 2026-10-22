# Sportlétesítmény-foglalások

Első WPF-prototípus a sportlétesítmények foglalásainak beolvasására és listázására.

## Indítás

Visual Studio-ban nyisd meg a `SportFoglalasok.csproj` fájlt, majd indítsd el az alkalmazást. A program ablakában válaszd ki a `foglalasok.pelda.txt` fájlt a mintaadatok megjelenítéséhez.

Parancssorból Windows alatt:

```powershell
dotnet run --project SportFoglalasok.csproj
```

## Adatformátum

A kiválasztott fájl sima szövegfájl. Minden foglalás egy külön sor, a mezők sorrendje: név, dátum (`yyyy-MM-dd`), kezdés, befejezés, helyszín, eszközök. A mezőket `|` karakter választja el. Például:

```text
# nev|datum|kezdes|befejezes|helyszin|eszkozok
Minta foglalás|2026-11-20|14:00|15:00|Tornaterem - 1. fél|Pingpongütő és labda
```

Nem létező vagy üres fájl kiválasztásakor üres foglaláslista jelenik meg. Ez a prototípus jelenleg beolvassa és listázza az adatokat; a foglalásfelvétel és a fájlba mentés későbbi lépés.
