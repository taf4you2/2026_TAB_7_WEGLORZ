using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using KasjerApp.Models;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class ReservationsPanel : UserControl
{
    private const int SuggestionDebounceMs = 250;
    private const int SuggestionMinChars = 2;
    private const int SuggestionMaxItems = 8;

    private readonly ApiService _api;
    private readonly DispatcherTimer _suggestionTimer;
    private List<ReservationSearchDto> _reservations = [];
    private ReservationSearchDto? _selectedReservation;
    private ReservationSearchDto? _activeReservation;
    private ReservationPassItem? _selectedPass;
    private CancellationTokenSource? _suggestionCts;
    private bool _suppressSuggestions;

    public ReservationsPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
        _suggestionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SuggestionDebounceMs) };
        _suggestionTimer.Tick += SuggestionTimer_Tick;
        Loaded += async (_, _) => await LoadFreeCardsAsync();
    }

    private async void SearchBtn_Click(object sender, RoutedEventArgs e) => await SearchReservationsAsync();

    private void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        _suppressSuggestions = true;
        EmailBox.Clear();
        _suppressSuggestions = false;
        CloseSuggestions();
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
        CloseSuggestions();
        await SearchReservationsAsync();
    }

    private void EmailBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && SuggestionsPopup.IsOpen)
        {
            CloseSuggestions();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down && SuggestionsPopup.IsOpen && SuggestionsList.Items.Count > 0)
        {
            SuggestionsList.SelectedIndex = 0;
            var container = SuggestionsList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
            container?.Focus();
            e.Handled = true;
        }
    }

    private void EmailBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressSuggestions) return;

        _suggestionTimer.Stop();
        _suggestionTimer.Start();
    }

    private void EmailBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!SuggestionsList.IsKeyboardFocusWithin)
            CloseSuggestions();
    }

    private async void SuggestionTimer_Tick(object? sender, EventArgs e)
    {
        _suggestionTimer.Stop();
        var query = EmailBox.Text.Trim();
        if (query.Length < SuggestionMinChars)
        {
            CloseSuggestions();
            return;
        }

        _suggestionCts?.Cancel();
        _suggestionCts = new CancellationTokenSource();
        var token = _suggestionCts.Token;

        try
        {
            var users = await _api.SearchUsersAsync(query);
            if (token.IsCancellationRequested) return;

            if (users.Count == 0)
            {
                CloseSuggestions();
                return;
            }

            SuggestionsList.ItemsSource = users.Take(SuggestionMaxItems).ToList();
            SuggestionsList.SelectedIndex = -1;
            SuggestionsPopup.IsOpen = EmailBox.IsKeyboardFocused;
        }
        catch
        {
            CloseSuggestions();
        }
    }

    private void SuggestionsList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject src &&
            ItemsControl.ContainerFromElement(SuggestionsList, src) is ListBoxItem item &&
            item.DataContext is UserDto user)
        {
            PickSuggestion(user);
            e.Handled = true;
        }
    }

    private void SuggestionsList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SuggestionsList.SelectedItem is UserDto user)
        {
            PickSuggestion(user);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CloseSuggestions();
            EmailBox.Focus();
            e.Handled = true;
        }
    }

    private async void PickSuggestion(UserDto user)
    {
        _suppressSuggestions = true;
        EmailBox.Text = user.Email;
        EmailBox.CaretIndex = user.Email.Length;
        _suppressSuggestions = false;
        CloseSuggestions();
        EmailBox.Focus();
        await SearchReservationsAsync();
    }

    private void CloseSuggestions()
    {
        _suggestionTimer.Stop();
        _suggestionCts?.Cancel();
        SuggestionsPopup.IsOpen = false;
        SuggestionsList.ItemsSource = null;
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
