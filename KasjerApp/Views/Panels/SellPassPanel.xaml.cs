using System.Text.RegularExpressions;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KasjerApp.Models;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class SellPassPanel : UserControl
{
    private readonly ApiService _api;
    private List<TariffItem> _tariffs = [];
    private int? _foundUserId;
    private string? _foundUserEmail;
    private DateTime _validFrom;
    private DateTime _validTo;

    public SellPassPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) =>
        {
            var minimumSaleDate = await _api.GetMinimumSaleDateAsync();
            ValidFromPicker.DisplayDateStart = minimumSaleDate;
            ValidFromPicker.SelectedDate = minimumSaleDate;
            ValidFromTimeBox.Text = DateTime.Now.ToString("HH:mm");
            await LoadTariffsAsync();
            await LoadFreeCardsAsync();
            UpdateValidityPreview();
        };
    }

    private async Task LoadTariffsAsync()
    {
        try
        {
            var all = await _api.GetTariffsAsync();
            _tariffs = all
                .Where(t => t.PassType == null || !t.PassType.Contains("bilet", StringComparison.OrdinalIgnoreCase))
                .Select(t => new TariffItem(t))
                .ToList();

            TariffCombo.ItemsSource = _tariffs;
            if (_tariffs.Count > 0) TariffCombo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            ShowError($"Blad pobierania taryf: {ex.Message}");
        }
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
            FreeCardCombo.ItemsSource = null;
            ShowCardInfo($"Nie udalo sie pobrac listy wolnych kart. Wpisz RFID recznie. Szczegoly: {ex.Message}",
                Brushes.LemonChiffon, Brushes.SaddleBrown);
        }
    }

    private async void FreeCardCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FreeCardCombo.SelectedItem is CardItem card)
        {
            RfidBox.Text = card.Id;
            await VerifyCardAsync(true);
        }
    }

    private async void VerifyCard_Click(object sender, RoutedEventArgs e) => await VerifyCardAsync(true);

    private async Task<bool> VerifyCardAsync(bool showInfo)
    {
        CardInfoBorder.Visibility = Visibility.Collapsed;
        var rfid = RfidBox.Text.Trim();
        if (string.IsNullOrEmpty(rfid))
        {
            if (showInfo) ShowCardInfo("Podaj RFID karty.", Brushes.MistyRose, Brushes.DarkRed);
            return false;
        }

        try
        {
            var verification = await _api.VerifyCardForIssueAsync(rfid);
            var card = verification.Card;
            var details = card == null
                ? verification.Message
                : $"{verification.Message} Status: {card.Status} | Wlasciciel: {card.Owner ?? "brak"}";
            ShowCardInfo(details, verification.CanIssue ? Brushes.Honeydew : Brushes.MistyRose,
                verification.CanIssue ? Brushes.DarkGreen : Brushes.DarkRed);
            return verification.CanIssue;
        }
        catch (Exception ex)
        {
            ShowCardInfo($"Blad weryfikacji: {ex.Message}", Brushes.MistyRose, Brushes.DarkRed);
            return false;
        }
    }

    private void ShowCardInfo(string text, Brush background, Brush foreground)
    {
        CardInfoText.Text = text;
        CardInfoText.Foreground = foreground;
        CardInfoBorder.Background = background;
        CardInfoBorder.Visibility = Visibility.Visible;
    }

    private void TariffCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateValidityPreview();
    private void ValidFromPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => UpdateValidityPreview();
    private void ValidFromTimeBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateValidityPreview();

    private void UpdateValidityPreview()
    {
        var date = ValidFromPicker?.SelectedDate ?? DateTime.Today;
        var text = ValidFromTimeBox?.Text ?? "";
        var time = TimeSpan.TryParseExact(text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : DateTime.Now.TimeOfDay;
        _validFrom = date.Date.Add(time);
        var days = TariffCombo.SelectedItem is TariffItem tariff ? ParseDurationDays(tariff.Dto.Name) : 1;
        _validTo = _validFrom.AddDays(days);
        if (ValidToText != null)
            ValidToText.Text = _validTo.ToString("dd.MM.yyyy HH:mm");
    }

    private static int ParseDurationDays(string tariffName)
    {
        var m = Regex.Match(tariffName, @"(\d+)[-\s]*dniow", RegexOptions.IgnoreCase);
        return m.Success ? int.Parse(m.Groups[1].Value) : 1;
    }

    private async void UserSearch_Click(object sender, RoutedEventArgs e) => await SearchUserAsync();

    private async void UserEmailBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await SearchUserAsync();
    }

    private void UserEmailBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_foundUserEmail == null) return;
        if (string.Equals(_foundUserEmail, UserEmailBox.Text.Trim(), StringComparison.OrdinalIgnoreCase)) return;

        _foundUserId = null;
        _foundUserEmail = null;
        UserResultsList.Visibility = Visibility.Collapsed;
        UserInfoBorder.Visibility = Visibility.Collapsed;
    }

    private async Task SearchUserAsync()
    {
        ClearUserSearchState();

        var email = UserEmailBox.Text.Trim();
        if (string.IsNullOrEmpty(email)) return;

        try
        {
            var users = await _api.SearchUsersAsync(email);
            if (users.Count == 0)
            {
                if (!LooksLikeEmail(email))
                {
                    ShowUserInfo("Brak wynikow. Wpisz pelny adres email, aby utworzyc nowego klienta.", Brushes.LemonChiffon, Brushes.SaddleBrown);
                    return;
                }

                var created = await _api.CreateUserAsync(email);
                SelectUser(created, $"Utworzono klienta: {created.Email} (ID: {created.Id})");
            }
            else if (users.Count == 1)
            {
                SelectUser(users[0], $"Znaleziono: {users[0].Email} (ID: {users[0].Id})");
            }
            else
            {
                ShowUserInfo($"Znaleziono {users.Count} klientow - kliknij wlasciwy email:", Brushes.LemonChiffon, Brushes.SaddleBrown);
                UserResultsList.ItemsSource = users.Select(u => new UserResultItem(u.Id, u.Email)).ToList();
                UserResultsList.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ShowUserInfo($"Blad wyszukiwania: {ex.Message}", Brushes.MistyRose, Brushes.DarkRed);
        }
    }

    private void UserResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UserResultsList.SelectedItem is UserResultItem item)
        {
            SelectUser(new UserDto(item.Id, item.Email), $"Wybrany: {item.Email} (ID: {item.Id})");
        }
    }

    private void SelectUser(UserDto user, string message)
    {
        _foundUserId = user.Id;
        _foundUserEmail = user.Email;
        UserEmailBox.Text = user.Email;
        UserResultsList.ItemsSource = null;
        UserResultsList.Visibility = Visibility.Collapsed;
        ShowUserInfo(message, Brushes.Honeydew, Brushes.DarkGreen);
    }

    private void ClearUserSearchState()
    {
        UserInfoBorder.Visibility = Visibility.Collapsed;
        UserResultsList.ItemsSource = null;
        UserResultsList.Visibility = Visibility.Collapsed;
        _foundUserId = null;
        _foundUserEmail = null;
    }

    private void ShowUserInfo(string text, Brush background, Brush foreground)
    {
        UserInfoText.Text = text;
        UserInfoBorder.Background = background;
        UserInfoText.Foreground = foreground;
        UserInfoBorder.Visibility = Visibility.Visible;
    }

    private async void SellBtn_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        ConfirmBorder.Visibility = Visibility.Collapsed;
        UpdateValidityPreview();

        var rfid = RfidBox.Text.Trim();
        if (string.IsNullOrEmpty(rfid)) { ShowError("Podaj RFID karty."); return; }
        if (TariffCombo.SelectedItem is not TariffItem tariff) { ShowError("Wybierz taryfe."); return; }
        var minimumSaleDate = await _api.GetMinimumSaleDateAsync();
        ValidFromPicker.DisplayDateStart = minimumSaleDate;
        if (_validFrom.Date < minimumSaleDate) { ShowError("Na dzisiaj nie mozna juz wydac karnetu. Wybierz najwczesniej kolejny dostepny dzien."); return; }
        if (!await ResolveUserBeforeIssueAsync()) return;
        if (!await VerifyCardAsync(false)) { ShowError("Karta nie jest gotowa do wydania."); return; }

        try
        {
            var req = new CreatePassRequest(rfid, tariff.Id, _validFrom, _validTo, _foundUserId);
            var res = await _api.SellPassAsync(req);
            ConfirmText.Text = $"ID karnetu: {res.Id} | Status: {res.Status} | Od: {res.ValidFrom:dd.MM.yyyy HH:mm} Do: {res.ValidTo:dd.MM.yyyy HH:mm}" +
                (res.RemainingRides.HasValue ? $" | Pozostale zjazdy: {res.RemainingRides}" : "");
            ConfirmBorder.Visibility = Visibility.Visible;
            await LoadFreeCardsAsync();
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

    private async Task<bool> ResolveUserBeforeIssueAsync()
    {
        var email = UserEmailBox.Text.Trim();
        if (string.IsNullOrEmpty(email))
        {
            _foundUserId = null;
            _foundUserEmail = null;
            return true;
        }

        if (_foundUserId.HasValue && string.Equals(_foundUserEmail, email, StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            var users = await _api.SearchUsersAsync(email);
            var exact = users
                .Where(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var candidates = exact.Count > 0 ? exact : users;

            if (candidates.Count == 1)
            {
                SelectUser(candidates[0], $"Znaleziono: {candidates[0].Email} (ID: {candidates[0].Id})");
                return true;
            }

            if (candidates.Count > 1)
            {
                UserResultsList.ItemsSource = candidates.Select(u => new UserResultItem(u.Id, u.Email)).ToList();
                UserResultsList.Visibility = Visibility.Visible;
                ShowUserInfo("Znaleziono kilku klientow - kliknij wlasciwy email.", Brushes.LemonChiffon, Brushes.SaddleBrown);
                ShowError("Kliknij email wlasciciela przed wydaniem karnetu.");
                return false;
            }

            if (!LooksLikeEmail(email))
            {
                ShowUserInfo("Nie znaleziono klienta. Wpisz pelny adres email, aby utworzyc nowego klienta.", Brushes.LemonChiffon, Brushes.SaddleBrown);
                ShowError("Wpisz pelny adres email klienta albo wybierz go z wynikow wyszukiwania.");
                return false;
            }

            var created = await _api.CreateUserAsync(email);
            SelectUser(created, $"Utworzono klienta: {created.Email} (ID: {created.Id})");
            return true;
        }
        catch (Exception ex)
        {
            ShowError($"Blad wyszukiwania narciarza: {ex.Message}");
            return false;
        }
    }

    private static bool LooksLikeEmail(string value) =>
        Regex.IsMatch(value.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

    private record TariffItem(TariffDto Dto)
    {
        public int Id => Dto.Id;
        public string DisplayName => Dto.RideCount.HasValue
            ? $"{Dto.Name} - {Dto.Price:N2} zl ({Dto.RideCount} zjazdow, {Dto.Season})"
            : $"{Dto.Name} - {Dto.Price:N2} zl ({Dto.Season})";
    }

    private record CardItem(CardDto Dto)
    {
        public string Id => Dto.Id;
        public string DisplayName => Dto.Owner != null ? $"{Dto.Id} - {Dto.Owner}" : Dto.Id;
    }

    private record UserResultItem(int Id, string Email)
    {
        public string DisplayName => Email;
    }
}

