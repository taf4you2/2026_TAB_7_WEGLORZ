using System.Windows;
using System.Windows.Controls;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class TransactionsPanel : UserControl
{
    private readonly ApiService _api;

    public TransactionsPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
        DateFilter.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async void SearchBtn_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        DateFilter.SelectedDate = null;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        MsgText.Visibility = Visibility.Collapsed;
        try
        {
            DateOnly? date = DateFilter.SelectedDate.HasValue
                ? DateOnly.FromDateTime(DateFilter.SelectedDate.Value)
                : null;
            var list = await _api.GetTransactionsAsync(date);
            TransGrid.ItemsSource = list;
            if (list.Count == 0)
            {
                MsgText.Text = "Brak transakcji dla wybranych filtrów.";
                MsgText.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            MsgText.Text = ex.Message;
            MsgText.Visibility = Visibility.Visible;
        }
    }
}
