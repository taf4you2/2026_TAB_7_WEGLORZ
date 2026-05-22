# Scenariusze testów — SystemAPI

Testy manualne. Baza uruchomiona przez `docker compose up postgres` z danymi testowymi.
SystemAPI dostępne pod `http://localhost:5000`.

---

## ST-01 · Pobierz listę taryf

**Endpoint:** `GET /api/taryfy`

**Kroki:**
1. Uruchom `docker compose up`.
2. Wyślij `GET http://localhost:5000/api/taryfy`.

**Oczekiwany wynik:**
- Status `200 OK`
- Tablica ≥ 11 taryf, każda z polami `id`, `name`, `price`, `passType`, `season`
- Taryfy z `passType = bilet_jednorazowy` i `passType = karnet` obecne

---

## ST-02 · Sprzedaj bilet jednorazowy (UC1)

**Endpoint:** `POST /api/bilety`

**Kroki:**
1. Wyślij:
```json
{
  "cardId": "A3:F2:11:CC",
  "tariffId": 1,
  "validOn": "2026-04-17T00:00:00Z",
  "quantity": 1
}
```

**Oczekiwany wynik:**
- Status `200 OK`
- Body zawiera `reservationId`, `totalAmount = 89.00`, `quantity = 1`
- W tabeli `reservation` pojawia się nowy rekord
- W tabeli `ski_pass` pojawia się nowy rekord ze statusem `aktywny`

**Przypadek błędu — nieistniejąca karta:**
```json
{ "cardId": "ZZ:ZZ:ZZ:ZZ", "tariffId": 1, "validOn": "2026-04-17T00:00:00Z", "quantity": 1 }
```
Oczekiwany: `400 Bad Request` z komunikatem o braku karty.

---

## ST-03 · Sprzedaj karnet (UC2)

**Endpoint:** `POST /api/karnety`

**Kroki:**
1. Wyślij:
```json
{
  "cardId": "F0:11:22:33",
  "tariffId": 6,
  "validFrom": "2026-04-17T00:00:00Z",
  "validTo":   "2026-04-20T00:00:00Z"
}
```

**Oczekiwany wynik:**
- Status `201 Created`
- Lokalizacja (`Location`) wskazuje na `/api/karnety/{id}`
- Body zawiera `status = aktywny`, `tariff = Karnet 3-dniowy Dorosły`

---

## ST-04 · Pobierz karnet po ID

**Endpoint:** `GET /api/karnety/{id}`

**Kroki:**
1. Wyślij `GET http://localhost:5000/api/karnety/1`

**Oczekiwany wynik:**
- Status `200 OK`
- Body z polami `id`, `cardId`, `status`, `tariff`, `validFrom`, `validTo`

**Przypadek błędu:**
- `GET /api/karnety/99999` → `404 Not Found`

---

## ST-05 · Blokada karnetu (UC3)

**Endpoint:** `POST /api/karnety/{id}/blokuj`

**Kroki:**
1. Wyślij `POST http://localhost:5000/api/karnety/1/blokuj` z body:
```json
{ "reason": "Zgubiona karta" }
```

**Oczekiwany wynik:**
- Status `204 No Content`
- `GET /api/karnety/1` zwraca `status = zablokowany`, `blockReason = Zgubiona karta`

---

## ST-06 · Symulacja zwrotu (podgląd)

**Endpoint:** `GET /api/karnety/{id}/symulacja-zwrotu`

**Kroki:**
1. Wyślij `GET http://localhost:5000/api/karnety/2/symulacja-zwrotu?returnCard=false`

**Oczekiwany wynik:**
- Status `200 OK`
- Body zawiera `grossAmount`, `refundForUnusedDays`, `manipulationFee = 10`, `totalRefund`
- Wartości numeryczne spójne (totalRefund = refundForUnusedDays − 10)

---

## ST-07 · Zwrot karnetu (UC11)

**Endpoint:** `POST /api/karnety/{id}/zwrot`

**Kroki:**
1. Wyślij `POST http://localhost:5000/api/karnety/2/zwrot` z body:
```json
{ "reason": "Rezygnacja z wyjazdu", "returnCard": false }
```

**Oczekiwany wynik:**
- Status `200 OK`
- Body zawiera `totalRefund` (kwota ujemna transakcji)
- `GET /api/karnety/2` zwraca `status = zwrocony`
- W tabeli `transaction` nowa transakcja z `amount < 0`

---

## ST-08 · Lista oczekujących zwrotów

**Endpoint:** `GET /api/zwroty/oczekujace`

**Kroki:**
1. Upewnij się, że karnet 4 ma status `oczekuje_na_zwrot` (seed data)
2. Wyślij `GET http://localhost:5000/api/zwroty/oczekujace`

**Oczekiwany wynik:**
- Status `200 OK`
- Tablica z przynajmniej jednym rekordem (karnet 4)
- Każdy rekord zawiera `passId`, `cardRfid`, `estimatedRefund`

---

## ST-09 · Dashboard statystyki

**Endpoint:** `GET /api/statystyki/dzisiaj`

**Kroki:**
1. Wyślij `GET http://localhost:5000/api/statystyki/dzisiaj`

**Oczekiwany wynik:**
- Status `200 OK`
- `ticketsSoldToday ≥ 2` (seed data dodaje 2 bilety na dzisiaj)
- `activePasses ≥ 2`
- `pendingReturns = 1`

---

## ST-10 · Rozkład wyciągów

**Endpoint:** `GET /api/wyciagi`

**Kroki:**
1. Wyślij `GET http://localhost:5000/api/wyciagi`

**Oczekiwany wynik:**
- Status `200 OK`
- Tablica 6 wyciągów
- Każdy zawiera `id`, `name`, `status`, `opensAt`, `closesAt`, `trails`
- Status = `czynny` / `przed_otwarciem` / `po_zamknieciu` / `nieczynny` — zależnie od aktualnej godziny

---

## ST-11 · Historia transakcji z filtrem

**Endpoint:** `GET /api/transakcje`

**Kroki:**
1. `GET http://localhost:5000/api/transakcje?date=2026-04-16`

**Oczekiwany wynik:**
- Tylko transakcje z dnia 2026-04-16
- Status `200 OK`, tablica posortowana malejąco po dacie

---

## ST-12 · Raport zmiany

**Endpoint:** `GET /api/raport-zmiany?cashierId=1`

**Kroki:**
1. Wyślij `GET http://localhost:5000/api/raport-zmiany?cashierId=1`

**Oczekiwany wynik:**
- Status `200 OK`
- `totalSalesCount ≥ 2`, `netRevenue > 0`
- `cashierLogin = kasjer@stacja.pl`

---

## ST-13 · Login kasjera

**Endpoint:** `POST /api/auth/login`

**Kroki:**
1. Wyślij:
```json
{ "email": "kasjer@stacja.pl", "password": "anything", "role": "kasjer" }
```

**Oczekiwany wynik:**
- Status `200 OK`
- Body: `{ "userId": 1, "role": "kasjer", "token": "stub-token" }`

**Przypadek błędu — zły login:**
```json
{ "email": "nieznany@stacja.pl", "password": "x", "role": "kasjer" }
```
Oczekiwany: `401 Unauthorized`

---

## ST-14 · Wydanie karty RFID

**Endpoint:** `POST /api/karty`

**Kroki:**
1. Wyślij:
```json
{ "id": "99:88:77:66" }
```

**Oczekiwany wynik:**
- Status `201 Created`
- Body: `{ "id": "99:88:77:66" }`

**Duplikat:**
- Powtórz żądanie → `409 Conflict`

---

## ST-15 · Mockupy — integracja z API

**Kroki:**
1. Uruchom `docker compose up`.
2. Otwórz `mockups/kasjer.html` w przeglądarce.
3. Sprawdź konsolę devtools — brak błędów CORS/sieciowych.
4. Dashboard: `Bilety sprzedane dziś`, `Aktywne karnety`, `Przychód zmiany` pobrane z API.
5. Przejdź do „Sprzedaj bilet" — siatka taryf załadowana z `/api/taryfy`.
6. Przejdź do „Transakcje" — tabela załadowana z `/api/transakcje`.
7. Otwórz `mockups/narciarz.html`, przejdź do „Rozkład wyciągów" — tabela załadowana z `/api/wyciagi`.
8. W kasjer.html przejdź do „Sprzedaj karnet" — siatka taryf załadowana z `/api/taryfy` (data-tariff-id obecne na kartach).

---

## ST-16 · Lista karnetów dla karty

**Endpoint:** `GET /api/karnety?cardId={rfid}`

**Kroki:**
1. Wyślij `GET http://localhost:5000/api/karnety?cardId=A3:F2:11:CC`

**Oczekiwany wynik:**
- Status `200 OK`
- Tablica z przynajmniej jednym karnetem (karnet 1 z seed data)
- Każdy element zawiera `id`, `cardId`, `status`, `tariff`, `validFrom`, `validTo`
- Posortowane malejąco po `validFrom`

**Przypadek — brak cardId:**
- `GET /api/karnety` (bez parametru) → `400 Bad Request`

**Przypadek — karta bez karnetów:**
- `GET /api/karnety?cardId=11:22:33:44` → `200 OK`, pusta tablica `[]`

---

## ST-17 · Historia przejazdów dla karty

**Endpoint:** `GET /api/raporty/przejazdy?cardId={rfid}`

**Uwaga:** Seed data (`03_dane_testowe.sql`) nie zawiera rekordów GateScan — wynik będzie pustą tablicą. Aby przetestować z danymi, wstaw ręcznie rekord do tabeli `gate_scan`.

**Kroki:**
1. Wyślij `GET http://localhost:5000/api/raporty/przejazdy?cardId=A3:F2:11:CC`

**Oczekiwany wynik:**
- Status `200 OK`
- Tablica (pusta jeśli brak seed GateScan)
- Każdy element zawiera `id`, `cardId`, `gateName`, `liftName`, `scanTime`, `result`

**Z filtrem daty:**
1. `GET /api/raporty/przejazdy?cardId=A3:F2:11:CC&date=2026-04-16`
- Status `200 OK`
- Tylko skany z dnia 2026-04-16

**Przypadek — brak cardId:**
- `GET /api/raporty/przejazdy` → `400 Bad Request`

---

## ST-18 · Sprzedaż karnetu przez mockup (UC2 end-to-end)

**Kroki:**
1. Uruchom `docker compose up`.
2. Otwórz `mockups/kasjer.html` w przeglądarce.
3. Przejdź do sekcji „Sprzedaj karnet".
4. Wpisz istniejący RFID w polu karty, np. `AA:BB:CC:DD`.
5. Poczekaj aż siatka taryf załaduje się z API — taryfy mają `data-tariff-id`.
6. Kliknij dowolną taryfę karnetu (np. „Karnet 3-dniowy Dorosły").
7. Ustaw daty: „Od" = jutro, „Do" = za 4 dni.
8. Kliknij „✅ Wystaw karnet i wydaj kartę".

**Oczekiwany wynik:**
- Przycisk zmienia tekst na „⏳ Wystawianie…" podczas requestu
- Alert sukcesu: `✅ Karnet wystawiony · ID X · Karta AA:BB:CC:DD · aktywny`
- Pole RFID zostaje wyczyszczone
- `GET /api/statystyki/dzisiaj` po akcji zwraca wyższe `activePasses`

**Przypadek błędu — nieistniejąca karta:**
- Wpisz `ZZ:ZZ:ZZ:ZZ` i kliknij przycisk
- Alert błędu inline: `Błąd 400: Karta ZZ:ZZ:ZZ:ZZ nie istnieje.`

**Przypadek błędu — brak taryfy:**
- Nie klikaj żadnej taryfy, kliknij przycisk
- Alert błędu: `Wybierz taryfę.`
