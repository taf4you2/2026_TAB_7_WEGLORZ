using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KasjerApp.Models;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class ReservationsPanel : UserControl
{
    private readonly ApiService _api;
    private List<ReservationSearchDto> _reservations = [];
    private ReservationSearchDto? _selectedReservation;
    private ReservationSearchDto? _activeReservation;
    private ReservationPassItem? _selectedPass;

    public ReservationsPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) => await LoadFreeCardsAsync();
    }

    private async void SearchBtn_Click(object sender, RoutedEventArgs e) => await SearchReservationsAsync();

    private void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        EmailBox.Clear();
        _reservations = [];
        _selectedReservation = null;
        _activeReservation = null;
        _selectedPass = null;
        ReservationsGrid.ItemsSource = null;
        PassesGrid.ItemsSource = null;
        SearchMsg.Text = "";
        IssueMsg.Text = "";
        SelectedReservationText.Text = "Wybierz rezerwację z listy.";
        AcceptReservationBtn.IsEnabled = false;
        IssueCardBtn.IsEnabled = false;
        StatusBorder.Visibility = Visibility.Collapsed;
    }

    private async void EmailBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        e.Handled = true;
        await SearchReservationsAsync();
    }

    private async Task SearchReservationsAsync()
    {
        var email = EmailBox.Text.Trim();
        StatusBorder.Visibility = Visibility.Collapsed;
        SearchMsg.Text = "";

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowStatus("Podaj email narciarza.", Brushes.MistyRose, Brushes.DarkRed);
            return;
        }

        if (!LooksLikeEmail(email))
        {
            ShowStatus("Podaj pełny adres email.", Brushes.MistyRose, Brushes.DarkRed);
            return;
        }

        try
        {
            _reservations = await _api.GetReservationsByEmailAsync(email);
            _selectedReservation = null;
            _activeReservation = null;
            _selectedPass = null;
            BindReservations();
            PassesGrid.ItemsSource = null;
            AcceptReservationBtn.IsEnabled = false;
            IssueCardBtn.IsEnabled = false;
            IssueMsg.Text = "";
            SelectedReservationText.Text = "Wybierz rezerwację z listy.";
            SearchMsg.Text = _reservations.Count == 0
                ? "Brak rezerwacji dla podanego emaila."
                : $"Znaleziono rezerwacje: {_reservations.Count}.";
        }
        catch (Exception ex)
        {
            ShowStatus($"Błąd wyszukiwania rezerwacji: {ex.Message}", Brushes.MistyRose, Brushes.DarkRed);
        }
    }

    private void ReservationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReservationsGrid.SelectedItem is not ReservationListItem item)
        {
            _selectedReservation = null;
            AcceptReservationBtn.IsEnabled = false;
            return;
        }

        _selectedReservation = _reservations.FirstOrDefault(r => r.Id == item.Id);
        BindPasses(_selectedReservation);
        AcceptReservationBtn.IsEnabled = _selectedReservation?.Passes.Any(IsPendingPass) == true;
        SelectedReservationText.Text = _selectedReservation == null
            ? "Wybierz rezerwację z listy."
            : $"Wybrano {item.ReservationNumber}. Karnety do wydania: {item.PendingCount}.";
        UpdateIssueButton();
    }

    private void AcceptReservationBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedReservation == null) return;

        _activeReservation = _selectedReservation;
        BindReservations();
        BindPasses(_activeReservation);
        SelectFirstPendingPass();
        IssueMsg.Text = $"Rezerwacja {_activeReservation.ReservationNumber} gotowa do wydawania kart.";
        ShowStatus("Wydawaj karty po kolei dla karnetów ze statusem oczekuje_na_odbior.", Brushes.Honeydew, Brushes.DarkGreen);
        UpdateIssueButton();
    }

    private void PassesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPass = PassesGrid.SelectedItem as ReservationPassItem;
        IssueMsg.Text = _selectedPass == null
            ? ""
            : $"Wybrany karnet ID: {_selectedPass.Id}.";
        UpdateIssueButton();
    }

    private void FreeCardCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FreeCardCombo.SelectedItem is CardItem card)
            RfidBox.Text = card.Id;
    }

    private async void IssueCardBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_activeReservation == null || _selectedPass == null) return;

        var rfid = RfidBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(rfid))
        {
            ShowStatus("Podaj RFID karty do wydania.", Brushes.MistyRose, Brushes.DarkRed);
            return;
        }

        if (!IsPendingStatus(_selectedPass.Status))
        {
            ShowStatus("Wybierz karnet oczekujący na odbiór.", Brushes.MistyRose, Brushes.DarkRed);
            return;
        }

        IssueCardBtn.IsEnabled = false;
        try
        {
            var verification = await _api.VerifyCardForIssueAsync(rfid);
            if (verification is not { CanIssue: true })
            {
                ShowStatus(verification?.Message ?? "Karta nie jest gotowa do wydania.", Brushes.MistyRose, Brushes.DarkRed);
                return;
            }

            var response = await _api.ActivateReservedPassAsync(
                new ActivatePassRequest(_activeReservation.ReservationNumber, rfid, _selectedPass.Id));
            var cardId = response?.CardId ?? rfid;
            var tariff = response?.Tariff ?? _selectedPass.Tariff ?? "brak nazwy";
            ShowStatus($"Wydano kartę {cardId} dla karnetu {_selectedPass.Id}. Taryfa: {tariff}.", Brushes.Honeydew, Brushes.DarkGreen);

            RfidBox.Clear();
            FreeCardCombo.SelectedItem = null;
            await LoadFreeCardsAsync();
            await RefreshActiveReservationAsync();
        }
        catch (Exception ex)
        {
            ShowStatus($"Błąd wydania karty: {ex.Message}", Brushes.MistyRose, Brushes.DarkRed);
        }
        finally
        {
            UpdateIssueButton();
        }
    }

    private async Task RefreshActiveReservationAsync()
    {
        var email = EmailBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(email)) return;

        var activeNumber = _activeReservation?.ReservationNumber;
        _reservations = await _api.GetReservationsByEmailAsync(email);
        _activeReservation = _reservations.FirstOrDefault(r => r.ReservationNumber == activeNumber);
        _selectedReservation = _activeReservation;
        BindReservations();
        BindPasses(_activeReservation);

        if (_activeReservation?.Passes.Any(IsPendingPass) == true)
        {
            SelectFirstPendingPass();
            IssueMsg.Text = $"Następny karnet w rezerwacji {_activeReservation.ReservationNumber}.";
            return;
        }

        IssueMsg.Text = "Wszystkie karnety z tej rezerwacji zostały wydane.";
        _activeReservation = null;
        _selectedPass = null;
        BindReservations();
        UpdateIssueButton();
    }

    private void BindReservations()
    {
        ReservationsGrid.ItemsSource = _reservations
            .Select(r => new ReservationListItem(
                r.Id,
                r.ReservationNumber,
                r.ReservationDate,
                r.Status,
                r.Passes.Count(IsPendingPass),
                _activeReservation?.ReservationNumber == r.ReservationNumber ? "w realizacji" : ""))
            .ToList();
    }

    private void BindPasses(ReservationSearchDto? reservation)
    {
        _selectedPass = null;
        PassesGrid.ItemsSource = reservation?.Passes
            .OrderByDescending(IsPendingPass)
            .ThenBy(p => p.Id)
            .Select(p => new ReservationPassItem(
                p.Id,
                p.CardId,
                p.Status,
                p.Tariff,
                p.Price,
                p.ValidFrom,
                p.ValidTo))
            .ToList();
    }

    private void SelectFirstPendingPass()
    {
        if (PassesGrid.ItemsSource is not IEnumerable<ReservationPassItem> passes) return;

        var pending = passes.FirstOrDefault(p => IsPendingStatus(p.Status));
        PassesGrid.SelectedItem = pending;
        _selectedPass = pending;
    }

    private async Task LoadFreeCardsAsync()
    {
        try
        {
            var cards = await _api.GetCardsAsync();
            FreeCardCombo.ItemsSource = cards
                .Where(c => c.Status is "wolna" or "free" or "available")
                .Select(c => new CardItem(c))
                .ToList();
        }
        catch (Exception ex)
        {
            IssueMsg.Text = $"Nie udało się pobrać wolnych kart: {ex.Message}";
        }
    }

    private void UpdateIssueButton()
    {
        IssueCardBtn.IsEnabled =
            _activeReservation != null &&
            _selectedPass != null &&
            IsPendingStatus(_selectedPass.Status);
    }

    private static bool IsPendingPass(ReservationPassDto pass) => IsPendingStatus(pass.Status);

    private static bool IsPendingStatus(string? status) =>
        string.Equals(status, "oczekuje_na_odbior", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeEmail(string value) =>
        Regex.IsMatch(value.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

    private void ShowStatus(string text, Brush background, Brush foreground)
    {
        StatusText.Text = text;
        StatusText.Foreground = foreground;
        StatusBorder.Background = background;
        StatusBorder.BorderBrush = foreground;
        StatusBorder.Visibility = Visibility.Visible;
    }

    private record ReservationListItem(
        int Id,
        string ReservationNumber,
        DateTime? ReservationDate,
        string? Status,
        int PendingCount,
        string RealizationStatus);

    private record ReservationPassItem(
        int Id,
        string? CardId,
        string? Status,
        string? Tariff,
        decimal? Price,
        DateTime? ValidFrom,
        DateTime? ValidTo);

    private record CardItem(CardDto Dto)
    {
        public string Id => Dto.Id;
        public string DisplayName => Dto.Owner != null ? $"{Dto.Id} - {Dto.Owner}" : Dto.Id;
    }
}
