# TODO — System Sprzedaży Biletów Narciarskich

## Zaimplementowane — SystemAPI (port 5000)

### Auth
- [x] `POST /api/auth/login` — logowanie kasjera i narciarza (JWT)
- [x] `POST /api/auth/register` — rejestracja narciarza (email + hasło, BCrypt)
- [x] `POST /api/auth/logout` — bezstanowy (usuwa token po stronie klienta)

### Sprzedaż (KasjerApp)
- [x] UC1: sprzedaż biletu jednorazowego — `POST /api/bilety`
- [x] UC2: sprzedaż karnetu — `POST /api/karnety`
- [x] UC3: blokada karnetu — `POST /api/karnety/{id}/blokuj`
- [x] UC11: symulacja zwrotu — `GET /api/karnety/{id}/symulacja-zwrotu`
- [x] UC11: zwrot karnetu — `POST /api/karnety/{id}/zwrot`

### Portal narciarza (web)
- [x] UC2 online: zakup karnetu online — `POST /api/zakup/online`
- [x] Lista karnetów do zakupu — `GET /api/zakup/taryfy`
- [x] Profil narciarza + karty RFID — `GET /api/users/me`
- [x] Rezerwacje narciarza — `GET /api/users/me/rezerwacje`
- [x] Wyszukiwanie narciarza po emailu — `GET /api/uzytkownicy?email=`

### Dane i raporty
- [x] Lista taryf — `GET /api/taryfy`
- [x] Lista kart RFID — `GET /api/karty`, `GET /api/karty/{rfid}`
- [x] Lista wyciągów z rozkładem — `GET /api/wyciagi`
- [x] Statystyki dzisiejszego dnia (kasjer) — `GET /api/statystyki/dzisiaj`
- [x] Historia transakcji — `GET /api/transakcje`
- [x] Raport zmiany — `GET /api/raport-zmiany`, `POST /api/raport-zmiany/zamknij`
- [x] Historia przejazdów (bramka) — `GET /api/raporty/przejazdy`
- [x] Zwroty oczekujące — `GET /api/zwroty/oczekujace`

### Strony (wwwroot)
- [x] `login.html` — logowanie narciarza
- [x] `kasjer-login.html` — logowanie kasjera (przekierowanie do KasjerApp)
- [x] `register.html` — rejestracja narciarza
- [x] `narciarz.html` — portal narciarza (karnety, rozkład, historia, zwrot, zakup online)

## Do zrobienia

### Rezerwacje online → KasjerApp
- [ ] `GET /api/rezerwacje/oczekujace` — lista rezerwacji online do obsługi przez kasjera
- [ ] `POST /api/rezerwacje/{id}/wydaj` — kasjer przypisuje kartę RFID i aktywuje karnet
- [ ] Panel w KasjerApp do odbioru rezerwacji online

### BramkaAPI
- [ ] UC5/UC6: rejestracja przejazdu i weryfikacja SkiPass (`POST /api/bramka/skan`, weryfikacja ważności)
- [ ] Sprawdzanie godzin otwarcia przy skanowaniu bramki
- [ ] Synchronizacja bazy lokalnej bramki przed otwarciem wyciągów (aktualnie: raz na godzinę)
- [ ] Skalowanie aplikacji konsolowej (Bramka) na wiele bramek

### Inne
- [ ] UC4: generowanie danych do druku biletu — `POST /api/bilety/{id}/wydrukuj`
- [ ] UC7: edycja rozkładu wyciągów — `PUT /api/wyciagi/{id}/harmonogram`
- [ ] JWT claims: CashierId i UserId pobierane z tokena (aktualnie w niektórych miejscach hardcoded)
