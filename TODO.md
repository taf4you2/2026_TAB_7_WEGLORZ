# TODO — System Sprzedaży Biletów Narciarskich

## Zaimplementowane endpointy (SystemAPI · port 5000)

| Metoda | Endpoint | Opis | UC |
|--------|----------|------|----|
| POST | `/api/auth/login` | Logowanie kasjera / narciarza | — |
| POST | `/api/auth/logout` | Wylogowanie (stub) | — |
| GET | `/api/taryfy` | Lista taryf biletów i karnetów | UC1, UC2 |
| POST | `/api/bilety` | Sprzedaj bilet jednorazowy | UC1 |
| GET | `/api/karnety?cardId=X` | Lista karnetów dla karty (ST-16) | portal |
| GET | `/api/karnety/{id}` | Szczegóły karnetu | — |
| POST | `/api/karnety` | Sprzedaj karnet | UC2 |
| POST | `/api/karnety/{id}/blokuj` | Zablokuj karnet | UC3 |
| GET | `/api/karnety/{id}/symulacja-zwrotu` | Podgląd kwoty zwrotu | UC11 |
| POST | `/api/karnety/{id}/zwrot` | Wykonaj zwrot karnetu | UC11 |
| GET | `/api/karty` | Lista kart RFID (filtr: status, search) | — |
| GET | `/api/karty/{rfid}` | Szczegóły karty | — |
| POST | `/api/karty` | Wydaj nową kartę RFID | — |
| GET | `/api/wyciagi` | Rozkład i status wyciągów | UC13 |
| GET | `/api/statystyki/dzisiaj` | Dashboard kasjera | — |
| GET | `/api/transakcje` | Historia transakcji (filtr: date, cashierId) | — |
| GET | `/api/raport-zmiany` | Raport zmiany kasjera | — |
| POST | `/api/raport-zmiany/zamknij` | Zamknij zmianę | — |
| GET | `/api/zwroty/oczekujace` | Lista wniosków o zwrot | UC11 |
| GET | `/api/raporty/przejazdy` | Historia skanów bramki dla karty | UC10 |

## Krytyczne (blokuje działanie)

- [OK] **Auth: weryfikacja hasła** — w `AuthController` login zwraca `stub-token` bez sprawdzenia hasła. Dodać BCrypt/PBKDF2.
- [OK ] **Auth: JWT** — zastąpić `stub-token` prawdziwym JWT; dodać middleware weryfikacji tokenu.
- [OK] **CashierId z sesji** — kontrolery `BiletyController`, `KarnetyController`, `RaportZmianyController` mają `// TODO: z sesji kasjera`. Po wdrożeniu JWT odczytywać CashierId z claimu.
- [OK] **UserId z sesji** — `KarnetyController.CreatePass` nie przypisuje rezerwacji do użytkownika.

## Ważne (brakujące funkcje)

- [ ] **Karnet: właściciel karty** — `KartyController` zwraca `owner = null`; model nie ma imienia/nazwiska, tylko e-mail przez `Reservation → User`. Ustalić jak wyświetlać właściciela.
- [ ] **Kaucja** — `KartyController` zawsze zwraca `depositPaid = true`; zaimplementować logikę kaucji przez transakcję `DictOperationType = kaucja`.
- [ ] **Status oczekuje\_na\_zwrot** — brak endpointu do zmiany statusu karnetu na `oczekuje_na_zwrot` (narciarz składa wniosek online). Dodać `POST /api/karnety/{id}/wniosek-zwrotu`. *(przeniesione do backlogu)*
- [ ] **Raport zmiany: gotówka vs karta** — `ShiftReportDto.cashAmount` i `cardAmount` zawsze `0`. Wymaga pola `payment_method` w tabeli `transaction`.
- [ ] **Zamknięcie zmiany** — `POST /api/raport-zmiany/zamknij` działa ale nie sprawdza czy zmiana jest już zamknięta (jest TODO w kodzie).
- [OK ] **Mockup login.html** — formularz logowania nie wywołuje `POST /api/auth/login`. Dodać `fetch` i przekierowanie do panelu.
- [ ] **Transakcje — karta RFID** — kolumna „Karta" w tabeli transakcji w kasjer.html pokazuje `—`; `TransactionDto` nie zawiera `cardId`. Dodać pole do DTO i zapytania w `TransakcjeController`.

## Jakość / tech debt

- [ ] **CORS** — aktualnie `AllowAnyOrigin` dla uproszczenia. Przed produkcją ograniczyć do konkretnych domen.
- [ ] **Migracje EF** — brak migracji EF Core; schemat zarządzany ręcznie przez SQL. Rozważyć `dotnet ef migrations`.
- [ ] **Walidacja requestów** — brakuje atrybutów `[Required]` / `FluentValidation` w rekordach request.
- [ ] **Obsługa błędów** — brak globalnego middleware do obsługi wyjątków; nieobsłużone błędy DB wyrzucają 500 bez treści.
- [ ] **Logowanie** — brak structured logging (Serilog/OpenTelemetry).
- [ ] **docker-compose: health check SystemAPI** — dodać `healthcheck` do serwisu `system-api`.

## Dokumentacja

- [ ] Zaktualizować `CLAUDE.md` o projekt `SystemAPI` i wszystkie endpointy.
- [ ] Wygenerować OpenAPI spec (Swagger UI) i dodać link do README.
- [ ] Uzupełnić scenariusze testów o testy negatywne dla bramki (BramkaAPI).
- [ ] Dodać seed GateScan do `03_dane_testowe.sql` żeby ST-17 (`/api/raporty/przejazdy`) dawał niezerowe wyniki.

## Przyszłe funkcje (backlog)

- [ ] Raport zarządczy: endpoint dla administratora agregujący dane z wielu zmian (`GET /api/raporty/zarzadczy`).
- [ ] Raport operacyjny: obłożenie wyciągów, top trasy (`GET /api/raporty/operacyjny`).
- [ ] Wniosek o zwrot online: `POST /api/karnety/{id}/wniosek-zwrotu` — zmienia status na `oczekuje_na_zwrot` (dla portalu narciarza).
- [ ] Rezerwacje online: obsługa statusu `oczekuje_na_potwierdzenie`.
- [ ] Powiadomienia e-mail przy sprzedaży/zwrocie.
- [ ] Testy integracyjne: xUnit + Testcontainers (PostgreSQL w kontenerze).
- [ ] UC4: druk biletu — `POST /api/bilety/{id}/wydrukuj` (generowanie danych do druku/PDF).
- [ ] UC5/UC6: rejestracja przejazdu i weryfikacja SkiPass przy bramce (`POST /api/bramka/skan`, `POST /api/bramka/weryfikuj`).
- [ ] UC7: edycja rozkładu wyciągów przez zarządcę (`PUT /api/wyciagi/{id}/harmonogram`).
