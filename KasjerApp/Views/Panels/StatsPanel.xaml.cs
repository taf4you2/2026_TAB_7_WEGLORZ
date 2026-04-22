using System.Windows;
using System.Windows.Controls;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class StatsPanel : UserControl
{
    private readonly ApiService _api;

    public StatsPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            var dto = await _api.GetStatsAsync();
            if (dto == null) return;
            TicketsSoldText.Text  = dto.TicketsSoldToday.ToString();
            ActivePassesText.Text = dto.ActivePasses.ToString();
            RevenueText.Text      = $"{dto.ShiftRevenue:N2}";
            PendingReturnsText.Text = dto.PendingReturns.ToString();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Błąd pobierania danych: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
