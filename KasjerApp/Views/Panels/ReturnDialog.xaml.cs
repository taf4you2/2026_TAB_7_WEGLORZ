using System.Windows;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class ReturnDialog : Window
{
    private readonly ApiService _api;
    private readonly int _passId;

    public string Reason => ReasonBox.Text.Trim();
    public bool ReturnCard => ReturnCardCheck.IsChecked == true;

    public ReturnDialog(ApiService api, int passId)
    {
        InitializeComponent();
        _api = api;
        _passId = passId;
        Loaded += async (_, _) => await RefreshPreviewAsync();
    }

    private async void ReturnCardCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        await RefreshPreviewAsync();
    }

    private async Task RefreshPreviewAsync()
    {
        try
        {
            var p = await _api.GetReturnPreviewAsync(_passId, ReturnCard);
            var cardLine = ReturnCard
                ? p.CardReturnEligible
                    ? $"Zwrot karty: TAK, kaucja: {p.DepositReturn:N2} PLN"
                    : $"Zwrot karty: NIE — {p.CardReturnBlockReason ?? "karta nie kwalifikuje sie do zwrotu"}"
                : "Zwrot karty: pomijany (kaucja rozliczana osobno w panelu Karty RFID)";

            PreviewText.Text =
                $"Kwota brutto: {p.GrossAmount:N2} PLN\n" +
                $"Dni lacznie / uzywane: {p.TotalDays} / {p.UsedDays}\n" +
                $"Zwrot za niewykorzystane: {p.RefundForUnusedDays:N2} PLN\n" +
                $"Oplata manipulacyjna: -{p.ManipulationFee:N2} PLN\n" +
                $"{cardLine}\n" +
                $"RAZEM do wyplaty: {p.TotalRefund:N2} PLN";
        }
        catch (Exception ex)
        {
            PreviewText.Text = $"Blad kalkulacji: {ex.Message}";
        }
    }

    private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Reason))
        {
            MessageBox.Show("Podaj powod zwrotu.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
