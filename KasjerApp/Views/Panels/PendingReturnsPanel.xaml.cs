using System.Windows;
using System.Windows.Controls;
using KasjerApp.Models;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class PendingReturnsPanel : UserControl
{
    private readonly ApiService _api;
    private PendingReturnDto? _selected;

    public PendingReturnsPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        MsgText.Visibility = Visibility.Collapsed;
        ActionMsg.Text = "";
        try
        {
            var list = await _api.GetPendingReturnsAsync();
            ReturnsGrid.ItemsSource = list;
            if (list.Count == 0)
            {
                MsgText.Text = "Brak oczekujących zwrotów.";
                MsgText.Foreground = System.Windows.Media.Brushes.Gray;
                MsgText.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            MsgText.Text = ex.Message;
            MsgText.Visibility = Visibility.Visible;
        }
    }

    private void ReturnsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = ReturnsGrid.SelectedItem as PendingReturnDto;
        ReturnBtn.IsEnabled = _selected != null;
        ActionMsg.Text = _selected != null
            ? $"Wybrany karnet ID: {_selected.PassId}  (szac. {_selected.EstimatedRefund:N2} PLN)"
            : "";
    }

    private async void ReturnBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;

        var dlg = new ReturnDialog(_api, _selected.PassId);
        dlg.Owner = Window.GetWindow(this);
        if (dlg.ShowDialog() != true) return;

        try
        {
            await _api.ReturnPassAsync(_selected.PassId, new ReturnPassRequest(dlg.Reason, dlg.ReturnCard));
            ActionMsg.Text = $"Zwrot karnetu {_selected.PassId} zatwierdzony.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ActionMsg.Text = $"Błąd: {ex.Message}";
        }
    }
}
