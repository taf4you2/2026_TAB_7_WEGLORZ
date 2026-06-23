# Notatka po konflikcie merge 2026-06-23

Podczas `git pull` lokalny `main` byl 4 commity przed `origin/main` i 3 commity za nim.
Na prosbe zespolu konflikty zostaly rozwiazane przez przyjecie wersji zdalnej (`origin/main`).

## Rzeczy lokalne do ewentualnego przywrocenia

### `KasjerApp/Services/ApiService.cs`

Lokalna wersja miala bardziej spojna obsluge bledow API:

- `GetTariffsAsync`, `GetCardAsync`, `GetCardsAsync`, `IssueCardAsync`, `DeleteCardAsync`,
  `BlockCardAsync`, `UnblockCardAsync`, `GetPassesByCardAsync`, `SellPassAsync`,
  `ActivateReservedPassAsync`, `GetReservationsByEmailAsync`, `GetReturnPreviewAsync`,
  `ReturnPassAsync`, `SearchUsersAsync`, `CreateUserAsync` i `GetTransactionsAsync`
  uzywaly `EnsureSuccessOrThrowApiMessageAsync(response)`.
- `EnsureSuccessOrThrowApiMessageAsync` czytal JSON z polem `message` i rzucal
  `InvalidOperationException` z czytelnym komunikatem z API zamiast ogolnego
  `HttpRequestException` z `EnsureSuccessStatusCode()`.
- Lokalna wersja miala wydzielone `TryReadApiMessageAsync(response)`, zeby nie powielac
  parsowania odpowiedzi bledu.
- Przy braku endpointu `/api/karty/{rfid}/weryfikacja-wydania` lokalna wersja rzucala
  komunikat: `Endpoint weryfikacji karty nie jest dostępny w API.`

Po przyjeciu wersji zdalnej warto rozważyć przywrocenie tej obslugi bledow, ale juz
z zachowaniem zdalnych metod dodanych w merge:

- `GetLiftsAsync()`
- `GetMinimumSaleDateAsync()`
- `GetPassAsync(int id)`
- `BlockPassAsync(int id, string reason)`
- `UnblockPassAsync(int id)`
- `SellTicketAsync(SellTicketRequest req)`
- `GetShiftReportAsync()`
- `CloseShiftAsync()`
- `GetPendingReturnsAsync()`
- filtr `cashierId` w `GetTransactionsAsync(...)`

### `KasjerApp/Models/ApiModels.cs`

Lokalna wersja nie zawierala modeli:

- `ShiftReportDto`
- `LiftDto`
- `PendingReturnDto`
- `SellTicketRequest`
- `SellTicketResponse`
- `BlockPassRequest`

Zdalna wersja je przywraca/dodaje. Jesli po merge cleanup nadal ma byc celem,
trzeba sprawdzic, ktore z nich sa faktycznie uzywane przez aktualny WPF i API.

### `KasjerApp/Views/Panels/SellTicketPanel.xaml.cs`

Lokalny commit `e4191c9` usunal:

- `KasjerApp/Views/Panels/SellTicketPanel.xaml`
- `KasjerApp/Views/Panels/SellTicketPanel.xaml.cs`
- `KasjerApp/Views/Panels/PendingReturnsPanel.xaml`
- `KasjerApp/Views/Panels/PendingReturnsPanel.xaml.cs`

W merge konflikt byl typu modify/delete: lokalnie `SellTicketPanel.xaml.cs` zostal
usuniety, a zdalnie plik byl nadal obecny/zmieniany. Przyjelismy wersje zdalna, wiec
`SellTicketPanel.xaml.cs` wraca do drzewa. Dodatkowo przywrocono z `origin/main`
`SellTicketPanel.xaml`, bo sam code-behind bez XAML zostawilby niepelny panel WPF.

Do sprawdzenia pozniej:

- czy `SellTicketPanel.xaml` tez powinien wrocic, bo sam code-behind bez XAML moze
  nie miec sensu w WPF;
- czy funkcja sprzedazy biletu ma zostac jako osobny panel, czy powinna byc
  przeniesiona do obecnego przeplywu w `SellPassPanel`;
- czy usuniecie `PendingReturnsPanel` bylo celowe i nadal aktualne po przyjeciu
  zdalnych metod `GetPendingReturnsAsync()`.

### Pliki niekonfliktowe z merge

Merge automatycznie przyjal zdalne zmiany m.in. w:

- `BazaDanych/skrypty_inicjacyjne/03_dane_testowe.sql`
- `Dokumentacja/diagram_klas/diagramKlasPoprawiony.puml`
- `KasjerApp/Views/Panels/SellPassPanel.xaml.cs`
- `SystemAPI/Controllers/BiletyController.cs`
- `SystemAPI/Controllers/KarnetyController.cs`
- `SystemAPI/Controllers/UsersController.cs`
- `SystemAPI/Controllers/ZakupOnlineController.cs`
- `SystemAPI/Services/SalesDatePolicy.cs`
- `SystemAPI/wwwroot/kasjer.html`
- `SystemAPI/wwwroot/narciarz.html`

Te pliki nie wymagaly recznego wyboru podczas konfliktu, ale moga wplywac na zachowanie
systemu po merge.
