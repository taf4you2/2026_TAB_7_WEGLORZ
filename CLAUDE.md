# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**System Sprzedaży Biletów Narciarskich** – a ski ticket sales system.
All documentation and naming is in Polish; code identifiers are in English.

IDE: JetBrains Rider (`.idea/`) + Visual Studio (`.vs/`).
Solution file: `TAB.slnx` (.NET 10.0).

**Current stage:** SystemAPI is the main API with 11 controllers and all core UC endpoints implemented (stubs). BramkaAPI is a legacy/separate gate-validation API. Authentication returns a stub token (no JWT/BCrypt yet). EF schema is managed via SQL scripts only (no migrations).

## Optional Quality Rulebooks

Focused rulebooks live in `Dokumentacja/agent-rulebooks/`.
They are project-local references and should be loaded on demand, not all at once.

Use them when the task matches:

- `clean-code.mini.md` — everyday changes in controllers, services, models, WPF, XAML, code-behind, and JavaScript.
- `refactoring.mini.md` — behavior-preserving cleanup, controller simplification, XAML/code-behind cleanup, large JS file cleanup, and repeated API logic.
- `working-effectively-with-legacy-code.mini.md` — risky changes in `BramkaAPI`, `Bramka`, old mockups, large `wwwroot` files, weakly tested code, or tightly coupled code.
- `a-philosophy-of-software-design.mini.md` — API/module boundaries between controllers, EF models, WPF, static web UI, database scripts, and gate services.
- `the-pragmatic-programmer.mini.md` — automation, duplicated knowledge, manual workflows, feedback loops, and preventing quality decay.
- `ui-redesign-existing-projects.md` — UI audit and targeted redesign of existing WPF/XAML or static HTML/JS screens without breaking functionality.
- `ui-design-taste-frontend.md` — broader visual direction for new UI work and anti-generic interface decisions.

Default quality workflow:

1. For any code change, keep `clean-code.mini.md` in mind.
2. For explicit cleanup or refactor requests, also read `refactoring.mini.md`.
3. For old, tangled, or poorly tested code, read `working-effectively-with-legacy-code.mini.md` before editing.
4. For new modules, APIs, cross-file boundaries, or awkward designs, read `a-philosophy-of-software-design.mini.md`.
5. For repeated manual work, duplicated knowledge, uncertain assumptions, or quality decay, read `the-pragmatic-programmer.mini.md`.
6. For UI polish, redesign, layout, typography, interaction states, or visual hierarchy, read `ui-redesign-existing-projects.md`; read `ui-design-taste-frontend.md` for broader visual direction.

Project-specific rules in this `CLAUDE.md` override generic rulebook advice.
Do not use rulebooks as permission for broad rewrites; keep changes scoped, remove dead code touched by the task, and preserve user changes.
Do not remove legacy code automatically; remove it only when it is tied to the current task or when the user explicitly asks.
UI rulebooks are visual guidance, not stack instructions. Keep the existing WPF desktop UI and static HTML/JS web UI unless the user explicitly asks for a different UI technology.

## Working Rules

- Preserve user changes and avoid reverting unrelated work.
- Keep changes scoped to the requested behavior or cleanup target.
- Prefer existing project patterns before introducing new abstractions.
- Keep database schema changes aligned with SQL scripts under `BazaDanych`; do not assume EF migrations are the source of truth unless the project changes that policy.
- For auth, roles, IDs, and security-sensitive behavior, verify current code before editing because documentation may lag behind implementation.
- For API changes, keep WPF `KasjerApp`, static `SystemAPI/wwwroot` clients, and documentation in sync when they are affected.

## Projects

| Project | Type | Description |
|---------|------|-------------|
| `SystemStacjiNarciarskiejDLL` | Class Library | 27 EF Core entity models + `SkiResortDbContext` |
| `SystemAPI` | ASP.NET Core Web API | **Main API** — ticket sales, passes, reports, auth; port 5000 (Docker) |
| `BramkaAPI` | ASP.NET Core Web API | Gate validation API; port 49226; separate service |
| `Bramka` | Console App | Test client for BramkaAPI |

## Build / Run

### Docker (primary)

```bash
docker-compose up --build
```

This starts three services: `postgres:17` (port 5432), `system-api` (port 5000→8080), `bramka-api` (port 49226→8080).
The DB is initialized in order: schema → sequences → test data.

### Local (without Docker)

Prerequisites: .NET 10.0 SDK, PostgreSQL running.

`SystemAPI/appsettings.json` — **not committed**, create manually:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ski_resort;Username=postgres;"
  },
  "DbPassword": "postgres"
}
```

Same pattern applies to `BramkaAPI/appsettings.json`.

```bash
dotnet run --project SystemAPI    # port 5062 (HTTP profile)
dotnet run --project BramkaAPI    # port 49226
dotnet run --project Bramka       # console test client for BramkaAPI
```

### Database Init Scripts (applied in order by Docker)

| File | Purpose |
|------|---------|
| `BazaDanych/skrypty_inicjacyjne/baza_danych_kod.sql` | Full schema (tables, FKs) |
| `BazaDanych/skrypty_inicjacyjne/02_sekwencje.sql` | GENERATED BY DEFAULT AS IDENTITY on 24 tables |
| `BazaDanych/skrypty_inicjacyjne/03_dane_testowe.sql` | Dict values, 3 skiers, 2 cashiers, 1 admin, 6 lifts, 6 trails |

## Architecture

### Solution Structure

```
TAB.slnx
├── SystemStacjiNarciarskiejDLL/   ← domain layer (shared)
│   ├── SkiResortDbContext.cs
│   └── Models/                    ← 27 entity classes
├── SystemAPI/                     ← main API (new)
│   ├── Program.cs                 ← CORS AllowAnyOrigin, OpenAPI, DbContext DI
│   ├── Dockerfile
│   └── Controllers/               ← 11 controllers
├── BramkaAPI/                     ← gate validation API (legacy)
│   ├── Program.cs
│   ├── Dockerfile
│   └── Controllers/BramkaController.cs
├── Bramka/                        ← console test client
├── BazaDanych/skrypty_inicjacyjne/  ← SQL init scripts
├── mockups/                       ← HTML UI prototypes (kasjer, narciarz)
└── Dokumentacja/                  ← diagrams, use cases, schema SQL
```

### SystemAPI Endpoints

| Controller | Route | Methods / Description |
|-----------|-------|-----------------------|
| `AuthController` | `/api/auth` | `POST /login`, `POST /logout` — stub token, no JWT yet |
| `TaryfyController` | `/api/taryfy` | `GET` — all tariffs |
| `BiletyController` | `/api/bilety` | `POST` — sell single ticket (UC1) |
| `KarnetyController` | `/api/karnety` | `GET ?cardId=`, `GET /{id}`, `POST` (UC2), `POST /{id}/blokuj` (UC3), `GET /{id}/symulacja-zwrotu`, `POST /{id}/zwrot` (UC11) |
| `KartyController` | `/api/karty` | `GET`, `GET /{rfid}`, `POST` — RFID card pool |
| `WyciagController` | `/api/wyciagi` | `GET` — lift schedules (UC13) |
| `StatystykiController` | `/api/statystyki` | `GET /dzisiaj` — cashier dashboard |
| `TransakcjeController` | `/api/transakcje` | `GET` — transaction history with filters |
| `RaportZmianyController` | `/api/raport-zmiany` | `GET`, `POST /zamknij` — cashier shift report (UC9) |
| `RaportyController` | `/api/raporty` | `GET /przejazdy` — gate scan history (UC10) |
| `ZwrotyController` | `/api/zwroty` | `GET /oczekujace` — pending refund requests |

### BramkaAPI Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/bramka/weryfikuj?a={0\|1}&b={0\|1}` | Placeholder: bitwise AND |
| GET | `/api/bramka/karty` | All cards from DB |

### Domain Model

**User roles:** `User` (skier), `Cashier`, `Administrator`, `TrailPlanner`

**Entitlements:** `Card` (RFID pool) → `SkiPass` (links Card + Tariff + Reservation) → `Tariff` (pricing per season/type) → `Reservation` → `Transaction`

**Infrastructure:** `Lift`, `Trail`, `LiftTrail` (junction), `Gate`, `GateScan`, `LiftSchedule`, `TrailSchedule`

**Reports:** `ShiftReport` (cashier), `AdminReport` (admin)

**Dict tables** (id + name): `DictCardStatus`, `DictPassStatus`, `DictPassType`, `DictOperationType`, `DictSeason`, `DictTrailDifficulty`, `DictReservationStatus`, `DictVerificationResult`, `DictReportType`

### Key Known Limitations (see TODO.md for full list)

- Auth returns a stub token — no BCrypt password check, no JWT middleware
- `CashierId` and `UserId` are hard-coded in controllers (need to come from JWT claims)
- No EF migrations — schema is managed entirely via SQL scripts
- CORS is `AllowAnyOrigin` (dev only)
- `depositPaid` always returns `true`; `cashAmount`/`cardAmount` always 0 in shift reports

### NuGet Packages

| Package | Version | Used in |
|---------|---------|---------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.1 | SystemAPI, BramkaAPI, DLL |
| `Microsoft.AspNetCore.OpenApi` | 10.0.6 | SystemAPI |
| `Microsoft.AspNetCore.OpenApi` | 10.0.5 | BramkaAPI |
| `Microsoft.EntityFrameworkCore.Design/Tools` | 10.0.6 | DLL |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.6 | DLL |
| `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` | 1.23.0 | BramkaAPI |

### Actors & Use Cases

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
- `baza_danych/baza_danych_kod.sql` — original DB schema SQL
- `scenariusze/SN_Scenariusze.pdf` — scenario descriptions
- `Opis_Przypadkow_Uzycia.docx` — use case descriptions

HTML UI mockups: `mockups/kasjer.html`, `mockups/narciarz.html`

Course materials: `weglorz-kurs/` (HTML modules, build.sh)
