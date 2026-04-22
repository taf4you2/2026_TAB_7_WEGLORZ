using System.Windows;
using System.Windows.Controls;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class ShiftReportPanel : UserControl
{
    private readonly ApiService _api;

    public ShiftReportPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        CloseMsg.Visibility = Visibility.Collapsed;
        try
        {
            var dto = await _api.GetShiftReportAsync();
            if (dto == null)
            {
                NoReportText.Visibility = Visibility.Visible;
                ReportGrid.Visibility   = Visibility.Collapsed;
                return;
            }
            NoReportText.Visibility = Visibility.Collapsed;
            ReportGrid.Visibility   = Visibility.Visible;

            R_Cashier.Text       = dto.CashierLogin;
            R_Date.Text          = dto.Date.ToString("dd.MM.yyyy");
            R_SalesCount.Text    = dto.TotalSalesCount.ToString();
            R_SalesAmount.Text   = $"{dto.TotalSalesAmount:N2} PLN";
            R_ReturnsCount.Text  = dto.TotalReturnsCount.ToString();
            R_ReturnsAmount.Text = $"{dto.TotalReturnsAmount:N2} PLN";
            R_Net.Text           = $"{dto.NetRevenue:N2} PLN";
            R_Cash.Text          = $"{dto.CashAmount:N2} PLN";
            R_Card.Text          = $"{dto.CardAmount:N2} PLN";
        }
        catch (Exception ex)
        {
            NoReportText.Text       = $"Błąd: {ex.Message}";
            NoReportText.Visibility = Visibility.Visible;
            ReportGrid.Visibility   = Visibility.Collapsed;
        }
    }

    private async void CloseShiftBtn_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Czy na pewno chcesz zamknąć zmianę? Operacja zapisze raport w systemie.",
            "Zamknięcie zmiany",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            await _api.CloseShiftAsync();
            CloseMsg.Text       = "Zmiana zamknięta pomyślnie.";
            CloseMsg.Visibility = Visibility.Visible;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            CloseMsg.Text       = $"Błąd: {ex.Message}";
            CloseMsg.Foreground = System.Windows.Media.Brushes.Crimson;
            CloseMsg.Visibility = Visibility.Visible;
        }
    }
}
