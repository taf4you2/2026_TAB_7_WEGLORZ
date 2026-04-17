Etap 1: Fundament Offline (Lokalna Baza Bramki)

Cel: Sprawić, aby bramka potrafiła wpuścić narciarza bez użycia połączenia sieciowego.

    Instalacja paczek Entity Framework Core oraz dostawcy SQLite w projekcie konsolowym Bramka.

    Stworzenie lokalnego modelu bazy danych (np. tabela KartyLokalne zawierająca tylko niezbędne minimum: ID oraz Status).

    Wygenerowanie pliku bazy .db przy starcie aplikacji.

    Modyfikacja pętli skanującej kartę – zmiana logiki tak, aby sprawdzała ważność karty wyłącznie poprzez odpytanie lokalnego pliku bazy danych.

Etap 2: Weryfikacja Hybrydowa i Caching (Bramka + API)

Cel: Zapewnienie obsługi nowo zakupionych kart oraz optymalizacja bazy głównej.

    Rejestracja usługi IMemoryCache w głównym pliku konfiguracyjnym projektu BramkaAPI.

    Przebudowa kontrolera w API: dodanie logiki, która przychodzące zapytania sprawdza najpierw w pamięci RAM, a dopiero w przypadku braku (Cache Miss) odpytuje bazę PostgreSQL i zapisuje wynik na kilka minut w Cache.

    Aktualizacja aplikacji Bramka: dodanie warunku (Fallback). Jeśli skanowana karta nie istnieje w lokalnym SQLite, bramka wysyła zapytanie HTTP do API.

    Zapisanie nieznanej karty do lokalnego SQLite natychmiast po uzyskaniu pomyślnej odpowiedzi z API, aby przy kolejnym okrążeniu narciarza bramka działała już w 100% offline.

Etap 3: Zapis historii przejazdów (Kolejka w tle)

Cel: Niezawodne przesyłanie logów z odbiciami do głównej bazy, bez opóźniania wejścia narciarza.

    Dodanie drugiej tabeli do lokalnej bazy SQLite w bramce (np. KolejkaOdbic ze statusem "Wysłano: Tak/Nie").

    Zmiana logiki skanowania: po każdym udanym lub nieudanym odbiciu, aplikacja dodaje wiersz do tabeli KolejkaOdbic i od razu otwiera kołowrotek (lub odmawia).

    Stworzenie cyklicznego procesu w tle w aplikacji konsolowej (tzw. Background Worker), który np. co 10 sekund sprawdza, czy w tabeli KolejkaOdbic są niewysłane rekordy.

    Utworzenie nowego punktu końcowego (endpointa) typu POST w BramkaAPI, który przyjmuje paczkę (listę) odbić i zapisuje je hurtem w PostgreSQL.

    Zamknięcie pętli: proces w tle w bramce, po otrzymaniu statusu 200 OK z API, oznacza rekordy jako zsynchronizowane lub fizycznie kasuje je z lokalnego dysku.

Etap 4: Powiadomienia w Czasie Rzeczywistym z ACK (Blokady)

Cel: Natychmiastowe blokowanie zgubionych/skradzionych kart z gwarancją dostarczenia sygnału do bramki.

    Instalacja biblioteki SignalR w projektach BramkaAPI (serwer Hub) oraz Bramka (klient).

    Stworzenie tabeli ZadaniaDoBramek w systemie API/PostgreSQL (do realizacji wzorca Outbox).

    Konfiguracja API tak, aby przyjmowało informację od kasjera (np. przez odrębny kontroler) o zablokowaniu karty, dodawało wpis do tabeli zadań i natychmiast wypychało komunikat przez SignalR.

    Podłączenie aplikacji konsolowej pod strumień SignalR. Odbiór komunikatu wymusza modyfikację lokalnego pliku SQLite (np. zmiana statusu z Aktywna na Zablokowana).

    Odesłanie przez bramkę potwierdzenia (ACK) po udanym zapisie lokalnym.

    Obsługa ACK w API: po odebraniu potwierdzenia, usunięcie wpisu o blokadzie z tabeli ZadaniaDoBramek.

    Dodanie logiki łączenia (Re-connect): gdy bramka uruchamia się ponownie (lub odzyskuje sieć), wysyła sygnał "jestem online", a serwer API sprawdza tabelę zadań i dosyła te komunikaty, których bramka nie potwierdziła podczas awarii.