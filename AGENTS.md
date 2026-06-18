# AGENTS.md

This file is the working guide for Codex and other coding agents in this repository.
Project documentation lives in `Dokumentacja/`.

## Project Shape

This repository contains **System Sprzedaży Biletów Narciarskich**, a .NET 10 ski-ticket sales system.
Documentation and user-facing domain language are mostly Polish; code identifiers are mostly English.

Main modules:

- `SystemAPI` - main ASP.NET Core Web API for ticket sales, passes, auth, reports, skier portal endpoints, and static HTML/JS pages in `wwwroot`.
- `KasjerApp` - WPF cashier desktop application.
- `SystemStacjiNarciarskiejDLL` - shared EF Core model and `SkiResortDbContext`.
- `BramkaAPI` - separate/legacy gate API.
- `Bramka` - console client for gate testing.
- `BazaDanych` - SQL schema, sequences, and test data scripts.
- `Dokumentacja` - diagrams, use cases, scenarios, and project notes.

Solution file: `TAB.slnx`.
Primary IDEs: JetBrains Rider (`.idea/`) and Visual Studio (`.vs/`).

## Build and Run

Docker is the primary full-system workflow:

```bash
docker-compose up --build
```

This starts PostgreSQL, `SystemAPI`, and `BramkaAPI`. Database schema, sequences,
and test data are initialized from `BazaDanych/skrypty_inicjacyjne/`.

Local development requires the .NET 10 SDK and PostgreSQL. Local
`appsettings.json` files are not committed; configure `DefaultConnection` and
`DbPassword` for `SystemAPI` and `BramkaAPI`.

```bash
dotnet build TAB.slnx
dotnet run --project SystemAPI
dotnet run --project BramkaAPI
dotnet run --project Bramka
```

The database schema is managed through SQL scripts, not EF migrations:

- `BazaDanych/skrypty_inicjacyjne/baza_danych_kod.sql` - schema and foreign keys.
- `BazaDanych/skrypty_inicjacyjne/02_sekwencje.sql` - identity sequences.
- `BazaDanych/skrypty_inicjacyjne/03_dane_testowe.sql` - test data.

## Optional Quality Rulebooks

Focused rulebooks live in `Dokumentacja/agent-rulebooks/`.
They are project-local references and should be loaded on demand, not all at once.

Use them when the task matches:

- `clean-code.mini.md` - everyday changes in controllers, services, models, WPF, XAML, code-behind, and JavaScript.
- `refactoring.mini.md` - behavior-preserving cleanup, controller simplification, XAML/code-behind cleanup, large JS file cleanup, and repeated API logic.
- `working-effectively-with-legacy-code.mini.md` - risky changes in `BramkaAPI`, `Bramka`, old mockups, large `wwwroot` files, weakly tested code, or tightly coupled code.
- `a-philosophy-of-software-design.mini.md` - API/module boundaries between controllers, EF models, WPF, static web UI, database scripts, and gate services.
- `the-pragmatic-programmer.mini.md` - automation, duplicated knowledge, manual workflows, feedback loops, and preventing quality decay.
- `ui-redesign-existing-projects.md` - UI audit and targeted redesign of existing WPF/XAML or static HTML/JS screens without breaking functionality.
- `ui-design-taste-frontend.md` - broader visual direction for new UI work and anti-generic interface decisions.

Default quality workflow:

1. For any code change, keep `clean-code.mini.md` in mind.
2. For explicit cleanup or refactor requests, also read `refactoring.mini.md`.
3. For old, tangled, or poorly tested code, read `working-effectively-with-legacy-code.mini.md` before editing.
4. For new modules, APIs, cross-file boundaries, or awkward designs, read `a-philosophy-of-software-design.mini.md`.
5. For repeated manual work, duplicated knowledge, uncertain assumptions, or quality decay, read `the-pragmatic-programmer.mini.md`.
6. For UI polish, redesign, layout, typography, interaction states, or visual hierarchy, read `ui-redesign-existing-projects.md`; read `ui-design-taste-frontend.md` for broader visual direction.

Project-specific rules in this `AGENTS.md` override generic rulebook advice.
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
