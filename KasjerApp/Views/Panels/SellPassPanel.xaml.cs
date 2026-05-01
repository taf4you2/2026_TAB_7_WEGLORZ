using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KasjerApp.Models;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class SellPassPanel : UserControl
{
    private readonly ApiService _api;
    private List<TariffItem> _tariffs = [];
    private int? _foundUserId;

    public SellPassPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
        ValidFromPicker.SelectedDate = DateTime.Today;
        Loaded += async (_, _) =>
        {
            await LoadTariffsAsync();
            await LoadFreeCardsAsync();
        };
    }

    private async Task LoadTariffsAsync()
    {
        try
        {
            var all = await _api.GetTariffsAsync();
            _tariffs = all
                .Where(t => t.PassType == null ||
                            !t.PassType.Contains("jednorazow", StringComparison.OrdinalIgnoreCase))
                .Select(t => new TariffItem(t))
                .ToList();

            if (_tariffs.Count == 0)
                _tariffs = all.Select(t => new TariffItem(t)).ToList();

            TariffCombo.ItemsSource = _tariffs;
            if (_tariffs.Count > 0) TariffCombo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Błąd pobierania taryf: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private async Task LoadFreeCardsAsync()
    {
        try
        {
            var cards = await _api.GetCardsAsync();
            var free = cards
                .Where(c => c.Status is "wolna" or "free" or "available")
                .Select(c => new CardItem(c))
                .ToList();
            FreeCardCombo.ItemsSource = free;
        }
        catch { /* ignoruj — pole ręczne nadal działa */ }
    }

    private void FreeCardCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FreeCardCombo.SelectedItem is CardItem card)
            RfidBox.Text = card.Id;
    }

    private async void VerifyCard_Click(object sender, RoutedEventArgs e)
    {
        CardInfoBorder.Visibility = Visibility.Collapsed;
        var rfid = RfidBox.Text.Trim();
        if (string.IsNullOrEmpty(rfid)) return;
        try
        {
            var card = await _api.GetCardAsync(rfid);
            if (card == null)
            {
                CardInfoText.Text = "Karta nie znaleziona.";
                CardInfoBorder.Background = Brushes.MistyRose;
            }
            else
            {
                CardInfoText.Text = $"Status: {card.Status}  |  Właściciel: {card.Owner ?? "brak"}  |  Kaucja: {(card.DepositPaid ? "wpłacona" : "brak")}";
                CardInfoBorder.Background = Brushes.LightBlue;
            }
            CardInfoBorder.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            CardInfoText.Text = $"Błąd weryfikacji: {ex.Message}";
            CardInfoBorder.Visibility = Visibility.Visible;
        }
    }

    // ── Taryfa → auto ValidTo ─────────────────────────────────────────────────

    private void TariffCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateValidTo();
    }

    private void ValidFromPicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateValidTo();
    }

    private void UpdateValidTo()
    {
        if (TariffCombo.SelectedItem is not TariffItem tariff) return;
        if (ValidFromPicker.SelectedDate is not DateTime from) return;

        var days = ParseDurationDays(tariff.Dto.Name);
        if (days > 0)
            ValidToPicker.SelectedDate = from.AddDays(days);
    }

    private static int ParseDurationDays(string tariffName)
    {
        var m = Regex.Match(tariffName, @"(\d+)[-–]dniow", RegexOptions.IgnoreCase);
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    // ── Wyszukiwanie narciarza po emailu ──────────────────────────────────────

    private async void UserSearch_Click(object sender, RoutedEventArgs e)
    {
        UserInfoBorder.Visibility = Visibility.Collapsed;
        UserResultCombo.Visibility = Visibility.Collapsed;
        _foundUserId = null;

        var email = UserEmailBox.Text.Trim();
        if (string.IsNullOrEmpty(email)) return;

        try
        {
            var users = await _api.SearchUsersAsync(email);

            if (users.Count == 0)
            {
                UserInfoText.Text = "Nie znaleziono narciarza o podanym adresie email.";
                UserInfoBorder.Background = Brushes.MistyRose;
                UserInfoText.Foreground = Brushes.DarkRed;
                UserInfoBorder.Visibility = Visibility.Visible;
            }
            else if (users.Count == 1)
            {
                _foundUserId = users[0].Id;
                UserInfoText.Text = $"✓  Znaleziono: {users[0].Email}  (ID: {users[0].Id})";
                UserInfoBorder.Background = new SolidColorBrush(Color.FromRgb(243, 229, 245));
                UserInfoText.Foreground = new SolidColorBrush(Color.FromRgb(74, 20, 140));
                UserInfoBorder.Visibility = Visibility.Visible;
            }
            else
            {
                UserInfoText.Text = $"Znaleziono {users.Count} narciarzy – wybierz z listy:";
                UserInfoBorder.Background = new SolidColorBrush(Color.FromRgb(255, 249, 196));
                UserInfoText.Foreground = new SolidColorBrush(Color.FromRgb(130, 90, 0));
                UserInfoBorder.Visibility = Visibility.Visible;

                UserResultCombo.ItemsSource = users.Select(u => new UserResultItem(u.Id, u.Email)).ToList();
                UserResultCombo.SelectedIndex = -1;
                UserResultCombo.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            UserInfoText.Text = $"Błąd wyszukiwania: {ex.Message}";
            UserInfoBorder.Background = Brushes.MistyRose;
            UserInfoText.Foreground = Brushes.DarkRed;
            UserInfoBorder.Visibility = Visibility.Visible;
        }
    }

    private void UserResultCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UserResultCombo.SelectedItem is UserResultItem item)
        {
            _foundUserId = item.Id;
            UserInfoText.Text = $"✓  Wybrany: {item.Email}  (ID: {item.Id})";
            UserInfoBorder.Background = new SolidColorBrush(Color.FromRgb(243, 229, 245));
            UserInfoText.Foreground = new SolidColorBrush(Color.FromRgb(74, 20, 140));
        }
    }

    // ── Sprzedaż ──────────────────────────────────────────────────────────────

    private async void SellBtn_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        ConfirmBorder.Visibility = Visibility.Collapsed;

        var rfid = RfidBox.Text.Trim();
        if (string.IsNullOrEmpty(rfid)) { ShowError("Podaj RFID karty."); return; }
        if (TariffCombo.SelectedItem is not TariffItem tariff) { ShowError("Wybierz taryfę."); return; }
        if (ValidFromPicker.SelectedDate is not DateTime from) { ShowError("Wybierz datę 'Ważny od'."); return; }
        if (ValidToPicker.SelectedDate is not DateTime to) { ShowError("Wybierz datę 'Ważny do'."); return; }
        if (to <= from) { ShowError("Data końcowa musi być późniejsza niż początkowa."); return; }

        try
        {
            var req = new CreatePassRequest(rfid, tariff.Id, from, to, _foundUserId);
            var res = await _api.SellPassAsync(req);
            if (res == null) { ShowError("Brak odpowiedzi z API."); return; }
            ConfirmText.Text =
                $"ID karnetu: {res.Id}  |  Status: {res.Status}  |  " +
                $"Od: {res.ValidFrom:dd.MM.yyyy}  Do: {res.ValidTo:dd.MM.yyyy}";
            ConfirmBorder.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.Visibility = Visibility.Visible;
    }

    // ── Modele lokalne ────────────────────────────────────────────────────────

    private record TariffItem(TariffDto Dto)
    {
        public int Id => Dto.Id;
        public string DisplayName => $"{Dto.Name}  ·  {Dto.Price:N2} zł  ({Dto.Season})";
    }

    private record CardItem(CardDto Dto)
    {
        public string Id => Dto.Id;
        public string DisplayName => Dto.Owner != null
            ? $"{Dto.Id}  ·  {Dto.Owner}"
            : Dto.Id;
    }

    private record UserResultItem(int Id, string Email)
    {
        public string DisplayName => $"#{Id}  {Email}";
    }
}
