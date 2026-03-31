# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**System Sprzedaży Biletów Narciarskich** – a ski ticket sales system. The project is currently in the design/pre-implementation phase. All documentation is in Polish.

IDE: JetBrains Rider (`.idea/` present) and Visual Studio (`.vs/` present) — likely a C#/.NET project. No source code or build system exists yet.

## Build / Run / Test

No build system is configured yet. Update this section once a .sln/.csproj or other build files are added.

## Architecture

The design is fully documented via PlantUML in `Dokumentacja/`:
- `diagram_klas.puml` — class diagram
- `diagram_przypadkow_uzycia.puml` — use case diagram
- PNG exports of both diagrams

### Domain Model

**Inheritance:**
- `Uprawnienie` (abstract) — base for all entitlements
  - `Bilet` — single-use paper ticket; valid until first ride; supports `drukuj()`
  - `Karnet` — RFID physical pass with a deposit (`kaucja`), time-limited; supports `wydaj()` / `zwroc()`

**Enums:**
- `TypKarnetu`: `DZIENNY`, `WIELODNIOWY`, `SEZONOWY`
- `StatusUprawnienia`: `AKTYWNY`, `ZABLOKOWANY`, `WYGASLY`, `WYKORZYSTANY`, `ZWROCONY`
- `StatusWyciagu`: `CZYNNY`, `NIECZYNNY`, `SERWIS`
- `TypRaportu`: `ZARZADCZY`, `OPERACYJNY`, `PRZEJAZDOW_NARCIARZA`

**Key entities and relationships:**
- `Narciarz` (skier) aggregates `0..*` `Uprawnienie`
- `Uprawnienie` composes `0..*` `Przejazd` (ride records)
- `Przejazd` references `1` `Wyciag` (ski lift)
- `Wyciag` composes `1` `RokladWyciagu` (schedule with opening/closing hours and days)
- `Raport` references `Narciarz` and `Przejazd`

### Actors & Use Cases

| Actor | Use Cases |
|-------|-----------|
| **Kasjer** | UC1 Sprzedaj bilet (→ UC4 Wydrukuj bilet), UC2 Sprzedaj karnet (→ UC4b Wydaj karnet), UC3 Zablokuj bilet, UC11 Zwróć karnet |
| **System Wyciągów** | UC5 Zarejestruj przejazd (→ UC6 Weryfikuj ważność); Narciarz inicjuje UC5 |
| **Zarządca** | UC7 Zdefiniuj rozkład wyciągów, UC8 Generuj raport zarządczy, UC9 Generuj raport operacyjny |
| **Narciarz** | UC10 Wydrukuj raport przejazdów, UC11 Zwróć karnet (→ UC12 Zarejestruj zwrot kaucji), UC13 Sprawdź rozkład wyciągów |
