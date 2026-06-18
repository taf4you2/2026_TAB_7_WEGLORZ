# Walkthrough / kontekstowe hinty — specyfikacja UX

> Status: specyfikacja (przed implementacją).
> Zakres: warstwa front-end portalu narciarza.
> Ekrany objęte: `SystemAPI/wwwroot/login.html`, `register.html`, `narciarz.html`.

## 1. Cel i zakres

System nieinwazyjnych podpowiedzi (popupów) dla **narciarza** korzystającego z portalu
po raz pierwszy lub wracającego po pomoc („review"). Trzy niezależne mechanizmy
wyzwalania, jeden wspólny silnik renderowania, deklaratywny rejestr treści
(treść hintów = dane, nie kod).

Ekrany:

- `login.html` — logowanie
- `register.html` — rejestracja
- `narciarz.html` — portal (5 zakładek + hero)

## 2. Taksonomia elementów (4 typy)

Każdy element należy do jednej z 4 klas — od niej zależy **pozycjonowanie**,
**czas namysłu** i **szablon treści**.

| Typ | Przykłady (selektory) | Kotwiczenie | Czas namysłu (hover) |
|-----|-----------------------|-------------|----------------------|
| **A. Pole edycyjne** | `#email`, `#password`, `#password2`, `#narciarz-rfid`, `#historia-date`, `#zwrot-reason-select`, `#zwrot-reason`, `#zwrot-return-card`, `#zakup-from`, `#zakup-to` | statyczne, na prawo od pola | **5 s** |
| **B. Przycisk** | `#loginButton`, `#registerBtn`, „Załaduj", „Załaduj karnety", `#zwrot-btn`, `#zakup-btn`, „Drukuj raport", taby nawigacji, „Wyloguj" | statyczne, na prawo od przycisku | **5 s** |
| **C. Informacja wyświetlana** | `.hint` (dane testowe), `.error`/`.success`, `.hint-pass`, badge'y statusu, `pass-card` (hero), `rfid-line`, kafelki karnetów, licznik wyciągów | podąża za kursorem | **1 s** |
| **D. Sekcja / panel** | `.weather-bar`, tabela wyciągów, `.timeline`, `price-summary` / `#zwrot-preview-body`, blok „Mapa tras", całe `.section` | podąża za kursorem | **1 s** |

## 3. Tryby wyzwalania

### 3.1. Hint na hover („tępe trzymanie kursora")

- Typ **A/B**: po **5 s** nieruchomego kursora nad elementem → popup pojawia się
  **statycznie** przy elemencie.
- Typ **C/D**: po **1 s** → popup **podąża za kursorem** (jego lewy górny róg =
  pozycja kursora + offset).
- Reset licznika przy ruchu kursora poza element. Dla A/B mikroruch (<4 px)
  nie resetuje licznika.
- Tylko **jeden** hover-popup naraz. Znika przy: opuszczeniu elementu, kliknięciu,
  `Esc`, scrollu.

### 3.2. Hint po powtarzającym się błędzie

- Wyzwalany **natychmiast** (pomija czas namysłu), kotwiczony jak typ A (przy polu).
- Wariant wizualny „error" (czerwony akcent), odróżnialny od neutralnego hinta.
- Reguła „powtarzający się": **2. nieudana** walidacja tego samego pola
  (na `blur` lub przy próbie submitu) **albo** 1. zablokowany submit z winy pola.
  Próg = 2 (konfigurowalny).
- Znika po poprawieniu wartości; licznik błędów per-pole zeruje się po sukcesie.
- Reguły walidacji → §8.

### 3.3. Walk-through przy pierwszym użyciu

- Uruchamiany raz na ekran, jeśli brak znacznika „widziano" (§6).
- Sekwencja kroków w ustalonej kolejności (§7): podświetlenie elementu
  (overlay ze spotlightem) + popup zakotwiczony wg typu + pasek `Krok X/N`,
  przyciski **Dalej / Wstecz / Pomiń**.
- `Pomiń` i dojście do końca → zapis znacznika. `Esc` = Pomiń.
- Po przełączeniu zakładki w trakcie tour kontynuuje od kroków tej zakładki
  (tour jest świadomy sekcji).

## 4. Reguły pozycjonowania (dokładne)

**Typ A — pole edycyjne** (i analogicznie **B — przycisk**):

```
popup.topLeft = element.topRight + (padding, 0)
padding domyślnie 8 px
```

Prawy-górny róg pola = lewy-górny róg popupa, z małym marginesem. Popup statyczny.

**Typ C — informacja** (i analogicznie **D — sekcja**):

```
popup.topLeft = cursor.position + (offset, offset)
offset domyślnie 12 px
popup podąża za kursorem (mousemove, throttle ~16 ms)
kształt: prostokąt
```

**Fallbacki krawędziowe (wspólne):**

- Wyjście poza prawą krawędź → A/B przerzuca się na lewą stronę elementu;
  C/D ustawia kursor jako prawy-górny róg popupa (lustro).
- Wyjście w dół → kotwica przeskakuje do góry.
- Popup nigdy nie zasłania elementu, którego dotyczy (A/B).

## 5. Reguły treści (per typ)

**A. Pole edycyjne** — w kolejności:

1. Czego dotyczy pole (1 zdanie).
2. **Obowiązkowe / opcjonalne**.
3. **Format / dozwolone wartości** (np. „e-mail musi zawierać `@` i domenę,
   np. `jan@poczta.pl`"; „data DD.MM.RRRR"; „min. 8 znaków").
4. Co jest **niedozwolone** / typowe pułapki.
5. (opcjonalnie) odsyłacz do powiązanego hinta.

**B. Przycisk** — **co się stanie po kliknięciu** (efekt, dokąd prowadzi,
czy wysyła/zapisuje, warunki wstępne, np. „wymaga wybranej karty").

**C. Informacja** — (1) czego dotyczy, (2) jak zmienić to, co widać,
(3) jak reaguje na inne komponenty.

**D. Sekcja/panel** — jak C, dla całego obszaru: do czego służy, skąd bierze dane,
czym sterują filtry.

**Odsyłacze:** w treści dopuszczalny element `→ zobacz: „<tytuł hinta>"` (klikalny).
Kliknięcie otwiera docelowy hint (zakotwiczony przy jego elemencie, z podświetleniem).
Stosować w długich instrukcjach (zwrot, zakup).

## 6. Trwałość stanu (cookies / „review")

Brief mówi „Cookies", ale aplikacja używa już `localStorage` (token, role, userId).
**Rekomendacja:** `localStorage` dla spójności (cookie tylko gdy stan ma być czytany
serwerowo — tu nie jest).

| Klucz | Wartość | Sens |
|-------|---------|------|
| `wt_seen_login` | `"1"`/wersja | walkthrough loginu odbyty |
| `wt_seen_register` | `"1"` | j.w. dla rejestracji |
| `wt_seen_narciarz` | wersja, np. `"3"` | tour portalu odbyty dla danej wersji UI |
| `wt_hints_enabled` | `"1"/"0"` | globalny włącznik hover-hintów |
| `wt_err_<pole>` | licznik (sesyjny) | liczba błędów per pole (czyszczony na sukces) |

- **Wersjonowanie:** zmiana układu → podbicie wersji w rejestrze → tour pokazuje się
  ponownie.
- **„Review":** przycisk **`?` Pomoc** w navbarze (`narciarz.html`) i link
  „Pokaż wskazówki" na ekranach auth — uruchamia tour ręcznie, niezależnie od znacznika.
- **Wygaszanie:** globalny przełącznik „Nie pokazuj podpowiedzi" w popupie hovera
  (`wt_hints_enabled=0`).

## 7. Sekwencje walk-through (kolejność kroków)

**`login.html`** (4 kroki):

1. `#email` (A) → format e-maila.
2. `#password` (A) → hasło, dane testowe.
3. `.hint` (C) → skąd wziąć dane testowe.
4. `#loginButton` (B) → co zrobi logowanie + dokąd przeniesie.

**`register.html`** (5 kroków):

1. `#email` → format.
2. `#password` + `.hint-pass` → min. 8 znaków.
3. `#password2` → musi być zgodne z hasłem (odsyłacz do kroku 2).
4. `#registerBtn` → utworzy konto i zaloguje.
5. link „Zaloguj się" → dla posiadaczy konta.

**`narciarz.html`** (tour wielosekcyjny, ~12 kroków):

1. Hero `pass-card` (C) — aktywny karnet w pigułce.
2. Navbar `.nav-tabs` (D) — przełączanie 5 widoków.
3. Zakładka **Moje karnety** → `#narciarz-rfid` (A) + „Załaduj" (B) → wybór karty
   steruje całym portalem (odsyłacz: „synchronizuje Historię i Zwrot").
4. `passes-grid` / kafelek (C) — odczyt statusu, pasek postępu.
5. Zakładka **Rozkład** → `weather-bar` (D) + tabela wyciągów (D) — dane na żywo.
6. Zakładka **Historia** → `#historia-rfid` + `#historia-date` (A) + „Załaduj" (B);
   „Drukuj raport" (B, UC10).
7. Zakładka **Zwróć karnet** → alert o kaucji (C), lista radio (A),
   `#zwrot-reason-select` (A), `#zwrot-return-card` (A), `#zwrot-btn` (B) —
   z odsyłaczami między krokami.
8. Zakładka **Kup karnet** → taryfy (D) → `#zakup-from`/`#zakup-to` (A) → `#zakup-btn` (B).
9. Navbar `?` Pomoc — jak wrócić do wskazówek.

## 8. Wykrywanie „powtarzającego się błędu" (reguły per pole)

| Pole | Warunek błędu | Treść hinta błędu |
|------|---------------|-------------------|
| `#email` (login/register) | brak `@`/domeny, nie pasuje do regex e-mail | „To pole to **adres e-mail** — musi zawierać `@` i domenę, np. `jan@poczta.pl`. Wygląda, jakbyś wpisał(a) imię i nazwisko." |
| `#password` (register) | < 8 znaków | „Hasło musi mieć **min. 8 znaków**." |
| `#password2` | ≠ `#password` | „Hasła **muszą być identyczne**. → zobacz hint pola „Hasło"." |
| `#narciarz-rfid` / `#historia-rfid` / `#zwrot-rfid` | akcja „Załaduj" przy pustym wyborze | „Najpierw **wybierz kartę RFID** z listy — bez niej nie pobiorę danych." |
| `#zwrot-reason-select` | submit wniosku bez powodu | „**Powód zwrotu jest wymagany** — wybierz z listy." |
| lista karnetów (radio) | submit bez zaznaczenia | „Zaznacz **karnet do zwrotu** powyżej." |
| `#zakup-from` / `#zakup-to` | `to < from` lub pusta data przy zakupie | „Data zakończenia nie może być **wcześniejsza** niż rozpoczęcia; format DD.MM.RRRR." |

Hooki: `blur` (walidacja miękka) + przechwycenie kliknięć przycisków akcji
(walidacja twarda, podbija licznik). Istniejące `showError()` w `login/register`
pozostają — hint to **dodatkowa**, kontekstowa warstwa przy polu.

## 9. Architektura (deklaratywna)

- **Rejestr hintów** — obiekt JS/JSON per ekran:
  `{ targetSelector, type: A|B|C|D, title, body[], links[], tourOrder, validate? }`.
- **Jeden silnik** (`walkthrough.js`, wstrzykiwany do trzech HTML-i): hover-timery,
  pozycjonowanie, error-hooki, tour, persistencja.
- Zero zmian w logice biznesowej; podpięcie przez selektory / `data-wt-id`,
  bez przepisywania istniejącego JS.
- Styl popupa w jednym bloku CSS, na zmiennych z `narciarz.html`
  (`--primary`, `--radius`, `--shadow-md`) + prostokątny wariant dla C/D.

## 10. Dostępność i przypadki brzegowe

- **Klawiatura:** odpowiednik hover-hinta na `focus` (pokaż od razu — brak kursora).
  Tour klawiszowy: `→`/`Enter` = Dalej, `←` = Wstecz, `Esc` = Pomiń.
- **Czytniki ekranu:** popup `role="tooltip"`/`aria-describedby`;
  tour `role="dialog"` z focus-trapem.
- **Dotyk/mobile:** brak hovera → hover-hinty off; zostaje tour i `?`.
  Popupy podążające (C/D) na dotyku → kotwiczone statycznie.
- **Reduced motion:** bez animacji podążania.
- **Kolizje:** maks. 1 popup; error-hint > hover-hint; tour wstrzymuje hover-hinty.
- **Elementy dynamiczne** (kafelki po `fetch`): wiązanie przez delegację zdarzeń.

## 11. Otwarte decyzje (rekomendacje)

1. **Cookies vs localStorage** → `localStorage` (spójność z auth).
2. **Re-trigger po zmianie UI** → wersjonowanie + ponowny pełny tour.
3. **Próg „powtarzającego się błędu"** → 2 (lub 1 przy twardym blokowaniu submitu).
4. **Zakres I etapu** → do ustalenia z zespołem.
