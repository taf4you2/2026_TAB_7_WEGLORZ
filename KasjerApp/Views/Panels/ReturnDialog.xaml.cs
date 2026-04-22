using System.Windows;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class ReturnDialog : Window
{
    private readonly ApiService _api;
    private readonly int _passId;

    public string Reason   => ReasonBox.Text.Trim();
    public bool ReturnCard => ReturnCardCheck.IsChecked == true;

    public ReturnDialog(ApiService api, int passId)
    {
        InitializeComponent();
        _api = api;
        _passId = passId;
        Loaded += async (_, _) => await RefreshPreviewAsync();
    }

    private async void ReturnCardCheck_Changed(object sender, RoutedEventArgs e)
        => await RefreshPreviewAsync();

    private async Task RefreshPreviewAsync()
    {
        try
        {
            var p = await _api.GetReturnPreviewAsync(_passId, ReturnCard);
            if (p == null) { PreviewText.Text = "Brak danych."; return; }
            PreviewText.Text =
                $"Kwota brutto:           {p.GrossAmount:N2} PLN\n" +
                $"Dni łącznie / używane:  {p.TotalDays} / {p.UsedDays}\n" +
                $"Zwrot za niewykorzystane: {p.RefundForUnusedDays:N2} PLN\n" +
                $"Opłata manipulacyjna:   -{p.ManipulationFee:N2} PLN\n" +
                $"Zwrot kaucji karty:     +{p.DepositReturn:N2} PLN\n" +
                $"─────────────────────────────────\n" +
                $"RAZEM do zwrotu:        {p.TotalRefund:N2} PLN";
        }
        catch (Exception ex)
        {
            PreviewText.Text = $"Błąd kalkulacji: {ex.Message}";
        }
    }

    private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Reason))
        {
            MessageBox.Show("Podaj powód zwrotu.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
