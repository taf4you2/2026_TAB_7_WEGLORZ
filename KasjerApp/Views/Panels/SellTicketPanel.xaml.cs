using System.Windows;
using System.Windows.Controls;
using KasjerApp.Models;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class SellTicketPanel : UserControl
{
    private readonly ApiService _api;
    private List<TariffItem> _tariffs = [];

    public SellTicketPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
        ValidOnPicker.SelectedDate = DateTime.Today.AddYears(1);
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
            // Bilety jednorazowe mają PassType zawierający "jednorazow" lub brak karnetu
            _tariffs = all
                .Where(t => t.PassType != null &&
                            (t.PassType.Contains("jednorazow", StringComparison.OrdinalIgnoreCase) ||
                             t.PassType.Contains("bilet", StringComparison.OrdinalIgnoreCase)))
                .Select(t => new TariffItem(t))
                .ToList();

            // Jeśli filtr nic nie dał — pokaż wszystkie
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

    private void FreeCardCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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
                CardInfoText.Text = "Karta nie znaleziona w systemie.";
                CardInfoBorder.Background = System.Windows.Media.Brushes.MistyRose;
            }
            else
            {
                CardInfoText.Text = $"Status: {card.Status}  |  Właściciel: {card.Owner ?? "brak"}  |  Kaucja: {(card.DepositPaid ? "wpłacona" : "brak")}";
                CardInfoBorder.Background = System.Windows.Media.Brushes.LightBlue;
            }
            CardInfoBorder.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            CardInfoText.Text = $"Błąd weryfikacji: {ex.Message}";
            CardInfoBorder.Visibility = Visibility.Visible;
        }
    }

    private async void SellBtn_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        ConfirmBorder.Visibility = Visibility.Collapsed;

        var rfid = RfidBox.Text.Trim();
        if (string.IsNullOrEmpty(rfid)) { ShowError("Podaj RFID karty."); return; }
        if (TariffCombo.SelectedItem is not TariffItem tariff) { ShowError("Wybierz taryfę."); return; }
        if (ValidOnPicker.SelectedDate is not DateTime validOn) { ShowError("Wybierz datę ważności."); return; }
        if (!int.TryParse(QuantityBox.Text.Trim(), out int qty) || qty < 1 || qty > 50)
        {
            ShowError("Liczba zjazdów musi być liczbą całkowitą 1–50."); return;
        }

        try
        {
            var req = new SellTicketRequest(rfid, tariff.Id, validOn, qty);
            var res = await _api.SellTicketAsync(req);
            if (res == null) { ShowError("Brak odpowiedzi z API."); return; }
            ConfirmText.Text =
                $"Nr rezerwacji: {res.ReservationId}  |  Ilość: {res.Quantity} szt.  |  " +
                $"Kwota: {res.TotalAmount:N2} PLN  |  Data ważności: {res.ValidOn:dd.MM.yyyy}";
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
}
