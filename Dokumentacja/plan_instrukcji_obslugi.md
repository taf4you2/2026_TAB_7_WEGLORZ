# Plan wykonania instrukcji obsługi

> Status: pierwsza wersja wdrożona. Panel administratora korzysta z edytowalnego
> JSON, ma tooltipy, panel pomocy i eksport treści. Instrukcja bramki jest
> generowana do PDF ze źródła HTML.

## 1. Cel

Przygotować dwie instrukcje:

1. **Panel administratora**
   - podpowiedzi kontekstowe (tooltipy) w aplikacji,
   - panel pomocy z dłuższym opisem aktualnego ekranu,
   - treść przechowywana poza kodem, łatwa do edycji,
   - możliwość wygenerowania PDF z tej samej treści.
2. **Bramka**
   - samodzielna instrukcja w formacie PDF,
   - bez podpowiedzi i zmian interfejsu programu konsolowego.

## 2. Główne założenie dla panelu administratora

Treści pomocy nie powinny być wpisane na stałe w HTML lub JavaScript.
Jednym źródłem treści będzie edytowalny plik:

`SystemAPI/wwwroot/help/admin-help.pl.json`

Każdy wpis będzie zawierał:

- identyfikator,
- sekcję panelu,
- selektor lub `data-help-id` elementu,
- tytuł,
- krótki tekst tooltipa,
- długi opis do panelu pomocy i PDF,
- kroki wykonania operacji,
- ostrzeżenia i typowe błędy,
- kolejność w dokumencie PDF.

Przykładowa struktura:

```json
{
  "id": "infra.add-lift",
  "section": "infra",
  "target": "infra-add-lift",
  "title": "Dodawanie wyciągu",
  "tooltip": "Otwiera formularz utworzenia nowego wyciągu.",
  "description": "Formularz pozwala określić nazwę, status i godziny pracy wyciągu.",
  "steps": [
    "Kliknij przycisk + Wyciąg.",
    "Uzupełnij nazwę i godziny pracy.",
    "Wybierz status i kliknij Zapisz."
  ],
  "warnings": [
    "Nazwa wyciągu musi być unikalna."
  ],
  "pdfOrder": 210
}
```

Zmiana tego pliku po odświeżeniu aplikacji zmieni treść tooltipów, panelu pomocy
i następnego wygenerowanego PDF. Nie będzie trzeba zmieniać logiki aplikacji.

## 3. Zakres pomocy w panelu administratora

### 3.1. Elementy interfejsu

Do `admin.html` należy dodać:

- przycisk **Pomoc / Instrukcja** w nagłówku,
- przełącznik **Pokaż podpowiedzi**,
- boczny panel pomocy z opisem aktualnej sekcji,
- ikonę informacji przy najważniejszych polach i akcjach,
- stabilne atrybuty `data-help-id`, niezależne od wyglądu HTML.

Tooltip powinien pojawiać się:

- po najechaniu kursorem,
- po ustawieniu fokusu klawiaturą,
- po kliknięciu ikony informacji,
- natychmiast przy błędzie wymagającym dodatkowego wyjaśnienia.

### 3.2. Sekcje do opisania

| Sekcja | Minimalny zakres instrukcji |
|---|---|
| Logowanie | dane logowania, wybór roli, błędy logowania |
| Dashboard | znaczenie statystyk, wykres ruchu, statusy kart, Live Activity Feed |
| Infrastruktura | dodawanie i dezaktywacja wyciągu, dodawanie i dezaktywacja bramki |
| Cennik i taryfy | tworzenie, edycja, limity, status aktywności, dezaktywacja |
| Karty RFID | wyszukiwanie, dodawanie, blokowanie, odblokowanie, zwrot i usunięcie |
| Sprzedaż i kasy | kanały sprzedaży, struktura taryf, aktywne zmiany, zamknięcie zmiany |
| Raporty | zakres dat, raport sprzedaży, przepustowość, raport zaawansowany, eksport |
| Pracownicy | tworzenie konta, role, aktywacja i dezaktywacja |
| Klienci | lista klientów, status, historia klienta i jego transakcji |

### 3.3. Priorytet tooltipów

Najpierw należy opisać:

1. operacje nieodwracalne lub dezaktywujące,
2. pola formularzy,
3. raporty i filtry dat,
4. wskaźniki, których znaczenie nie jest oczywiste,
5. przyciski eksportu i drukowania,
6. elementy informacyjne dashboardu.

Nie każdy napis wymaga tooltipa. Podpowiedzi powinny wyjaśniać działanie, a nie
powtarzać etykietę przycisku.

## 4. Proponowana architektura panelu pomocy

### Pliki

```text
SystemAPI/wwwroot/
├── help/
│   └── admin-help.pl.json
├── js/
│   ├── admin-help.js
│   └── admin.js
├── css/
│   ├── admin-help.css
│   └── admin.css
└── admin.html
```

### Odpowiedzialność

- `admin-help.pl.json` — cała edytowalna treść.
- `admin-help.js` — ładowanie treści, tooltipy, panel pomocy, obsługa klawiatury.
- `admin-help.css` — wygląd tooltipów, panelu i podświetlenia elementów.
- `admin.html` — tylko identyfikatory `data-help-id` oraz kontener panelu pomocy.
- `admin.js` — informowanie modułu pomocy o zmianie aktywnej sekcji.

### Stan użytkownika

W `localStorage`:

- `admin_help_enabled` — włączenie tooltipów,
- `admin_help_seen_version` — wersja obejrzanej instrukcji,
- `admin_help_last_section` — ostatnio otwarta sekcja pomocy.

## 5. Edycja treści

### Wariant podstawowy — rekomendowany

Treść jest edytowana bezpośrednio w `admin-help.pl.json`.

Zalety:

- prosty zakres implementacji,
- treść jest wersjonowana w Git,
- aplikacja i PDF zawsze korzystają z tego samego źródła,
- brak potrzeby dodawania tabeli w bazie i nowych endpointów.

### Wariant rozszerzony — opcjonalny

Dodać administratorowi tryb **Edytuj instrukcję**, który:

- otwiera formularz edycji tytułu, tooltipa, kroków i ostrzeżeń,
- zapisuje zmiany przez chroniony endpoint API,
- przechowuje wersje treści i autora zmiany,
- pozwala wyeksportować aktualną wersję.

Ten wariant wymaga osobnego modelu danych, autoryzacji endpointów i historii zmian.
Nie jest potrzebny do pierwszego działającego wydania.

## 6. Generowanie PDF administratora

Należy przygotować generator:

```text
Dokumentacja/instrukcje/
├── assets/admin/
├── output/
└── generate-admin-manual.mjs
```

Generator:

1. odczytuje `admin-help.pl.json`,
2. sortuje wpisy według sekcji i `pdfOrder`,
3. buduje dokument HTML z okładką i spisem treści,
4. dodaje zrzuty ekranów,
5. generuje `Instrukcja_panel_administratora.pdf`.

Rekomendowany mechanizm: HTML + CSS do druku oraz Chromium/Playwright do
powtarzalnego zapisu PDF.

Struktura PDF:

1. Okładka, wersja systemu i data.
2. Logowanie.
3. Opis nawigacji.
4. Dashboard.
5. Infrastruktura.
6. Cennik i taryfy.
7. Karty RFID.
8. Sprzedaż i kasy.
9. Raporty.
10. Pracownicy.
11. Klienci.
12. Najczęstsze błędy.
13. Skrócona lista najważniejszych operacji.

## 7. Instrukcja PDF bramki

### Pliki źródłowe

```text
Dokumentacja/instrukcje/
├── bramka/
│   ├── instrukcja-bramki.md
│   └── assets/
├── output/
│   └── Instrukcja_bramki.pdf
└── generate-gate-manual.mjs
```

### Zakres instrukcji

1. Przeznaczenie programu `Bramka`.
2. Wymagania:
   - działające `BramkaAPI`,
   - poprawny adres API,
   - poprawny `GateId`,
   - dostęp do lokalnej bazy SQLite.
3. Uruchomienie programu.
4. Wprowadzenie lub odczyt ID karty.
5. Interpretacja komunikatów:
   - dostęp przyznany,
   - odmowa dostępu,
   - błąd serwera,
   - przekroczony czas oczekiwania.
6. Tryb online:
   - zapis odbicia przez API,
   - wyświetlenie decyzji.
7. Tryb offline:
   - użycie lokalnego cache kart,
   - zapis odbicia lokalnego,
   - późniejsza synchronizacja.
8. Synchronizacja aktywnych kart i zaległych odbić.
9. Lokalizacja i znaczenie `bramka_lokalna.db`.
10. Bezpieczne zatrzymanie i ponowne uruchomienie.
11. Rozwiązywanie problemów:
    - API niedostępne,
    - nieznana karta,
    - błędny `GateId`,
    - brak synchronizacji,
    - uszkodzony lub zablokowany plik SQLite.
12. Jednostronicowa skrócona instrukcja operatora.

W PDF należy umieścić zrzuty konsoli dla co najmniej czterech przypadków:

- dostęp przyznany online,
- odmowa online,
- poprawna weryfikacja offline z cache,
- odmowa offline dla nieznanej karty.

## 8. Kolejność realizacji

### Etap 1 — inwentaryzacja

- przypisać `data-help-id` do elementów administratora,
- utworzyć listę komunikatów bramki,
- ustalić nazwy ekranów i terminologię.

### Etap 2 — źródło treści

- utworzyć schemat `admin-help.pl.json`,
- napisać pierwszą wersję tekstów,
- dodać walidację unikalności identyfikatorów i wymaganych pól.

### Etap 3 — pomoc w aplikacji administratora

- dodać silnik tooltipów,
- dodać panel dłuższej instrukcji,
- podłączyć zmianę sekcji,
- dodać obsługę klawiatury i `aria-describedby`,
- zapisywać ustawienia pomocy w `localStorage`.

### Etap 4 — treść instrukcji bramki

- przygotować instrukcję operatora,
- przygotować część techniczną,
- zebrać komunikaty i procedury awaryjne.

### Etap 5 — zrzuty ekranów

- uruchomić system na danych testowych,
- wykonać zrzuty wszystkich sekcji administratora,
- wykonać zrzuty czterech głównych stanów bramki,
- używać stałej rozdzielczości i anonimizowanych danych.

### Etap 6 — generatory PDF

- utworzyć wspólny szablon HTML/CSS,
- wygenerować PDF administratora z pliku JSON,
- wygenerować PDF bramki z Markdown,
- dodać numery stron, wersję i datę.

### Etap 7 — przegląd

- sprawdzić tekst pod względem językowym,
- sprawdzić zgodność instrukcji z rzeczywistym działaniem,
- sprawdzić linki, spis treści, podział stron i czytelność zrzutów,
- wykonać test instrukcji z osobą, która nie zna systemu.

## 9. Kryteria odbioru

### Administrator

- treść tooltipów nie jest wpisana na stałe w HTML,
- zmiana pliku treści jest widoczna po odświeżeniu,
- każda główna sekcja ma opis w panelu pomocy,
- najważniejsze formularze i niebezpieczne akcje mają tooltipy,
- pomoc działa myszą i klawiaturą,
- z tego samego źródła można wygenerować aktualny PDF.

### Bramka

- PDF opisuje pracę online i offline,
- wszystkie komunikaty operatora są wyjaśnione,
- opisano konfigurację `GateId` i adresu API,
- instrukcja zawiera procedury awaryjne i skróconą stronę operatora,
- treść odpowiada aktualnej implementacji `Bramka/Program.cs`.

## 10. Szacowany podział prac

| Zadanie | Szacunek |
|---|---:|
| Inwentaryzacja i schemat treści | 0,5 dnia |
| Teksty administratora | 1 dzień |
| Tooltipy i panel pomocy | 1–2 dni |
| Tekst instrukcji bramki | 0,5–1 dnia |
| Zrzuty ekranów | 0,5 dnia |
| Generatory i styl PDF | 1 dzień |
| Test i korekta | 0,5 dnia |

Łącznie: około **5–6 dni roboczych** dla kompletnej, sprawdzonej wersji.
