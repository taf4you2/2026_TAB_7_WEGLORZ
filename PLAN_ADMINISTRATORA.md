# Rozszerzony Plan Dokończenia Panelu Administratora - SkiAdmin

Ten dokument zawiera szczegółową specyfikację techniczną i funkcjonalną dla modułu administratora. Plan skupia się na analityce, monitorowaniu w czasie rzeczywistym oraz audycie operacyjnym.

---

## 1. Architektura Danych i API (Backend)

### 1.1. Nowe Struktury Danych (DTOs)
*   **ActivityFeedDto:**
    *   `Id` (int): identyfikator skanu.
    *   `CardRfid` (string): unikalny kod karty.
    *   `Location` (string): nazwa bramki + nazwa wyciągu (np. "Bramka A - Wyciąg Orlik").
    *   `Timestamp` (DateTime): dokładny czas zdarzenia.
    *   `Status` (string): "Sukces", "Odmowa", "Błąd techniczny".
    *   `Reason` (string): opcjonalny powód odmowy (np. "Brak środków", "Karta zablokowana").
*   **SalesChartDto:**
    *   `Labels` (string[]): daty lub godziny.
    *   `OnlineValues` (decimal[]): kwoty ze sprzedaży internetowej.
    *   `OnsiteValues` (decimal[]): kwoty ze sprzedaży w kasach.
*   **AdminReportHistoryDto:**
    *   `Id`, `AdminLogin`, `ReportTypeName`, `Parameters` (zserializowany JSON), `GeneratedAt`.

### 1.2. Logika Biznesowa Endpointów
*   **Monitorowanie Ruchu (`StatystykiController`):**
    *   Endpoint powinien agregować dane z ostatnich 15 minut, 1 godziny i 24 godzin.
    *   Wymagana optymalizacja: zapytania SQL powinny korzystać z indeksów na kolumnie `ScanTime`.
*   **Analiza Sprzedaży:**
    *   Implementacja porównywania okresów (np. "ten tydzień vs poprzedni tydzień").
    *   Rozbicie przychodów na typy karnetów (czasowe vs punktowe).

---

## 2. Projekt Interfejsu i Wizualizacji (Frontend)

### 2.1. System Wykresów (Chart.js Integration)
*   **Wykres 1: Natężenie Ruchu (Dashboard):**
    *   Typ: `line` (liniowy).
    *   Oś X: Godziny otwarcia stacji (08:00 - 22:00).
    *   Oś Y: Liczba osób przechodzących przez bramki.
    *   Funkcja: Możliwość przełączania widoku między "Wszystkie wyciągi" a konkretnym wyciągiem.
*   **Wykres 2: Struktura Sprzedaży (Sprzedaż):**
    *   Typ: `doughnut` (pierścieniowy).
    *   Dane: Udział procentowy różnych taryf w całkowitym zysku.
*   **Wykres 3: Porównanie Kanałów (Sprzedaż):**
    *   Typ: `bar` (słupkowy).
    *   Dane: Przychód dzienny z podziałem na "Kasy" i "Online".

### 2.2. Panel "Live Activity Feed"
*   **Układ:** Pionowa lista w prawej kolumnie dashboardu lub dedykowana sekcja.
*   **Interaktywność:** 
    *   Kliknięcie w zdarzenie otwiera mini-okno z detalami karty i jej historią.
    *   Przycisk "Zablokuj kartę" dostępny bezpośrednio z poziomu logu aktywności w razie wykrycia oszustwa.
*   **Wizualizacja statusów:**
    *   Ikona zielonego ptaszka dla poprawnych przejść.
    *   Czerwony wykrzyknik dla prób użycia nieważnej karty.
    *   Ikona "podejrzane" (pomarańczowa) dla zbyt szybkich odbić na różnych bramkach.

### 2.3. Moduł Raportów Zarządczych
*   **Generator:** Formularz pozwalający wybrać typ raportu, zakres dat, konkretne wyciągi lub konkretnych kasjerów.
*   **Podgląd Live:** Zanim admin pobierze PDF/CSV, widzi tabelaryczne podsumowanie danych na ekranie.
*   **Historia:** Tabela z logami generowania, pozwalająca sprawdzić, który administrator sprawdzał jakie dane (wymóg bezpieczeństwa).

---

## 3. Bezpieczeństwo i Audyt

*   **Logowanie akcji administratora:** Każda zmiana statusu wyciągu, bramki lub karty musi być zapisana w logach systemowych.
*   **Walidacja Uprawnień:** Każdy endpoint w `StatystykiController` i `RaportyController` musi rygorystycznie sprawdzać rolę `admin` w tokenie JWT.
*   **Ochrona danych osobowych:** W raportach sprzedażowych dane klientów powinny być zanonimizowane (np. ukryte maile), chyba że raport dotyczy konkretnego użytkownika.

---

## 4. Plan Rozwoju Bazy Danych (SQL)

### 4.1. Indeksy i Wydajność
*   Utworzenie indeksu kompozytowego na `gate_scans(scan_time, gate_id)` dla przyspieszenia dashboardu.
*   Utworzenie indeksu na `transactions(transaction_date)` dla raportów finansowych.

### 4.2. Logika "Triggera" (Poziom Aplikacji lub SQL)
*   **Alert Fraudowy:** Mechanizm sprawdzający czy karta użyta na Wyciągu A o 12:00 nie została użyta na Wyciągu B (oddalonym o 2km) o 12:01. 
*   W przypadku wykrycia, system automatycznie wysyła flagę do "Live Feed" administratora.

---

## 5. Kamienie Milowe (Milestones)

1.  **M1: Fundamenty API** - Gotowe DTOs i podstawowe zapytania agregujące.
2.  **M2: Wizualizacja Dashboardu** - Chart.js zintegrowany z danymi o ruchu i statusach kart.
3.  **M3: Live Feed** - System odświeżania i prezentacji ostatnich odbić.
4.  **M4: Pełne Raportowanie** - Filtrowanie, historia i eksport danych.
5.  **M5: System Alertów** - Wykrywanie podejrzanych aktywności i logowanie audytowe.
