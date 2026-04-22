using System.Windows;
using System.Windows.Controls;
using KasjerApp.Models;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class SellPassPanel : UserControl
{
    private readonly ApiService _api;
    private List<TariffItem> _tariffs = [];

    public SellPassPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
        ValidFromPicker.SelectedDate = DateTime.Today;
        ValidToPicker.SelectedDate = DateTime.Today.AddDays(30);
        Loaded += async (_, _) => await LoadTariffsAsync();
    }

    private async Task LoadTariffsAsync()
    {
        try
        {
            var all = await _api.GetTariffsAsync();
            // Karnety: wszystko poza biletami jednorazowymi
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
        if (ValidFromPicker.SelectedDate is not DateTime from) { ShowError("Wybierz datę 'Ważny od'."); return; }
        if (ValidToPicker.SelectedDate is not DateTime to) { ShowError("Wybierz datę 'Ważny do'."); return; }
        if (to <= from) { ShowError("Data końcowa musi być późniejsza niż początkowa."); return; }

        int? userId = null;
        if (!string.IsNullOrWhiteSpace(UserIdBox.Text))
        {
            if (!int.TryParse(UserIdBox.Text.Trim(), out int uid)) { ShowError("ID narciarza musi być liczbą."); return; }
            userId = uid;
        }

        try
        {
            var req = new CreatePassRequest(rfid, tariff.Id, from, to, userId);
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

    private record TariffItem(TariffDto Dto)
    {
        public int Id => Dto.Id;
        public string DisplayName => $"{Dto.Name}  ({Dto.Season})  —  {Dto.Price:N2} PLN";
    }
}
