# TODO — System Sprzedaży Biletów Narciarskich

## Zaimplementowane endpointy (SystemAPI · port 5000)

- [x] UC1: sprzedaż biletu jednorazowego — `POST /api/bilety/sprzedaj`
- [x] UC2: sprzedaż karnetu — `POST /api/karnety/sprzedaj`
- [x] UC3: blokada biletu/karnetu — `POST /api/bilety/{id}/blokuj`
- [x] UC11: zwrot karnetu (klient) — `POST /api/zwroty/zglos`, `POST /api/zwroty/{id}/akceptuj` (pracownik)
- [x] UC8: podgląd historii transakcji (kasjer) — `GET /api/transakcje/zmiana/{id}`
- [x] UC9: raport zmianowy (kasjer) — `GET /api/raporty/zmiana/{id}`, `POST /api/raporty/zamknij-zmiane`
- [x] UC10: podgląd statystyk (zarządca) — `GET /api/statystyki/dzienne`, `GET /api/statystyki/wyciagi`
- [x] Słowniki i dane podstawowe:
    - `GET /api/taryfy`
    - `GET /api/karty/dostepne`
    - `GET /api/wyciagi`

## Do zrobienia (Backlog)

1) trzeba dodać synchronizację bazy danych przed otwarciem wyciągów, na razie można ustawić jedną godzinę dla wszystkich bramek (w tym wypadku dla jednej)
2) wprowadzić możliwość skalowania aplikacji konsolowej w jakiś sposób.
3) wprowadzić sprawdzanie czasu w bramce, żeby nie było sytuacji że ktoś wchodzi na wyciąg w godzinach , kiedy jest zamknięty.
4)
- [ ] UC4: druk biletu — `POST /api/bilety/{id}/wydrukuj` (generowanie danych do druku/PDF).
- [ ] UC5/UC6: rejestracja przejazdu i weryfikacja SkiPass przy bramce (`POST /api/bramka/skan`, `POST /api/bramka/weryfikuj`).
- [ ] UC7: edycja rozkładu wyciągów przez zarządcę (`PUT /api/wyciagi/{id}/harmonogram`).
