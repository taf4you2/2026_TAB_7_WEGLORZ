# Generowanie instrukcji

Gotowe dokumenty PDF są przechowywane wspólnie w:

`output/`

Generowanie obu dokumentów:

```powershell
.\Dokumentacja\instrukcje\generate-all-manuals.ps1
```

## Bramka

Źródło:

`bramka/instrukcja-bramki.html`

Generowanie PDF w PowerShell:

```powershell
.\Dokumentacja\instrukcje\generate-gate-manual.ps1
```

Plik wynikowy:

`output/Instrukcja_bramki.pdf`

Skrypt korzysta z lokalnej instalacji Microsoft Edge lub Google Chrome.

## Panel administratora

Treść tooltipów i instrukcji znajduje się w:

`SystemAPI/wwwroot/help/admin-help.pl.json`

Generowanie PDF administratora:

```powershell
node .\Dokumentacja\instrukcje\generate-admin-manual.mjs
```

Plik wynikowy:

`output/Instrukcja_panelu_administratora.pdf`

Panel administratora pozwala edytować treść lokalnie i pobrać scalony plik
`admin-help.pl.edited.json` albo gotowy materiał tekstowy
`Instrukcja_panelu_administratora.md`. Po zatwierdzeniu tekstów wyeksportowany
JSON może zastąpić źródło w projekcie, a Markdown może zostać użyty do składu PDF.
