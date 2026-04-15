# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**System Sprzedaży Biletów Narciarskich** – a ski ticket sales system.
All documentation and naming is in Polish; code identifiers are in English.

IDE: JetBrains Rider (`.idea/`) + Visual Studio (`.vs/`).
Solution file: `TAB.slnx` (.NET 10.0).

**Current stage:** Early implementation. Domain models and DB context are complete. BramkaAPI has 2 working endpoints and a live PostgreSQL connection. The Bramka console client is a thin test harness. Core business logic (ticket sales, passes, reports) is not yet implemented.

## Projects

| Project | Type | Description |
|---------|------|-------------|
| `SystemStacjiNarciarskiejDLL` | Class Library | 27 EF Core entity models + `SkiResortDbContext` |
| `BramkaAPI` | ASP.NET Core Web API | REST API; Dockerfile included; connects to PostgreSQL |
| `Bramka` | Console App | Test client — calls BramkaAPI at `http://localhost:49226` |

## Build / Run

### Prerequisites
- .NET 10.0 SDK
- PostgreSQL server running
- `BramkaAPI/appsettings.json` — **not committed**, must be created manually:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<host>;Database=<db>;Username=<user>;"
  },
  "DbPassword": "<password>"
}
```

The password is intentionally split out so it can be stored in user secrets or environment variables instead.

### Run

```bash
# 1. Start BramkaAPI (IIS Express profile uses port 49226, HTTP profile uses 5062)
dotnet run --project BramkaAPI

# 2. Run console client (targets port 49226 — match your profile)
dotnet run --project Bramka
```

Alternatively use Docker:
```bash
docker build -f BramkaAPI/Dockerfile -t bramka-api .
docker run -p 8080:8080 bramka-api
```

Database schema SQL: `Dokumentacja/baza_danych/baza_danych_kod.sql`

### API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/bramka/weryfikuj?a={0|1}&b={0|1}` | Returns bitwise AND of a and b (placeholder logic) |
| GET | `/api/bramka/karty` | Returns all Cards from DB |

## Architecture

### Solution Structure

```
TAB.slnx
├── SystemStacjiNarciarskiejDLL/   ← domain layer
│   ├── SkiResortDbContext.cs
│   └── Models/                    ← 27 entity classes
├── BramkaAPI/                     ← API layer
│   ├── Program.cs
│   ├── Dockerfile
│   └── Controllers/BramkaController.cs
└── Bramka/                        ← console client
    └── Program.cs
```

### Domain Model (implemented in code)

**User roles:**
- `User` — end customer (skier)
- `Cashier`, `Administrator`, `TrailPlanner` — staff

**Entitlements:**
- `Card` — physical RFID card (pool)
- `SkiPass` — links Card + Tariff + Reservation; has status and validity dates
- `Tariff` — pricing rules per season and pass type
- `Reservation` — booking; links to Transactions

**Infrastructure:**
- `Lift`, `Trail`, `LiftTrail` (junction), `Gate`, `GateScan`
- `LiftSchedule`, `TrailSchedule`

**Reports:**
- `ShiftReport` (Cashier), `AdminReport` (Administrator)

**Dictionary/enum tables** (id + name):
`DictCardStatus`, `DictPassStatus`, `DictPassType`, `DictOperationType`,
`DictSeason`, `DictTrailDifficulty`, `DictReservationStatus`,
`DictVerificationResult`, `DictReportType`

### NuGet Packages

| Package | Version | Used in |
|---------|---------|---------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.1 | BramkaAPI, DLL |
| `Microsoft.AspNetCore.OpenApi` | 10.0.5 | BramkaAPI |
| `Microsoft.EntityFrameworkCore.Design/Tools` | 10.0.6 | DLL |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.6 | DLL |
| `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` | 1.23.0 | BramkaAPI |

### Actors & Use Cases (design)

| Actor | Use Cases |
|-------|-----------|
| **Kasjer** | UC1 Sprzedaj bilet (→ UC4 Wydrukuj bilet), UC2 Sprzedaj karnet (→ UC4b Wydaj karnet), UC3 Zablokuj bilet, UC11 Zwróć karnet |
| **System Wyciągów** | UC5 Zarejestruj przejazd (→ UC6 Weryfikuj ważność); Narciarz inicjuje UC5 |
| **Zarządca** | UC7 Zdefiniuj rozkład wyciągów, UC8 Generuj raport zarządczy, UC9 Generuj raport operacyjny |
| **Narciarz** | UC10 Wydrukuj raport przejazdów, UC11 Zwróć karnet (→ UC12 Zarejestruj zwrot kaucji), UC13 Sprawdź rozkład wyciągów |

## Documentation

All in `Dokumentacja/`:
- `diagram_klas/diagram_klas.puml` — class diagram (PlantUML)
- `diagram_przypadkow_uzycia/diagram_przypadkow_uzycia.puml` — use case diagram
- `baza_danych/baza_danych_kod.sql` — DB schema SQL
- `scenariusze/SN_Scenariusze.pdf` — scenario descriptions
- `Opis_Przypadkow_Uzycia.docx` — use case descriptions
