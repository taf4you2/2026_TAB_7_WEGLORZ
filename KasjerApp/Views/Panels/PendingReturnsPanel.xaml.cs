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

        if (_selected == null)
        {
            DetailsBorder.Visibility = Visibility.Collapsed;
            ActionMsg.Text = "";
            return;
        }

        DetailPassId.Text = _selected.PassId.ToString();
        DetailCardRfid.Text = _selected.CardRfid;
        DetailOwnerEmail.Text = _selected.OwnerEmail ?? "—";
        DetailPassType.Text = _selected.PassType ?? "—";
        DetailRemainingDays.Text = _selected.RemainingDays.ToString();
        DetailEstimatedRefund.Text = $"{_selected.EstimatedRefund:N2} PLN";
        DetailsBorder.Visibility = Visibility.Visible;
        ActionMsg.Text = "";
    }

    private async void ReturnBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;

        var dlg = new ReturnDialog(_api, _selected.PassId);
        dlg.Owner = Window.GetWindow(this);
        if (dlg.ShowDialog() != true) return;

        try
        {
            var result = await _api.ReturnPassAsync(_selected.PassId, new ReturnPassRequest(dlg.Reason, dlg.ReturnCard));
            var passId = _selected.PassId;
            var refundForDays = result != null ? Math.Max(0, result.RefundForUnusedDays - result.ManipulationFee) : 0;

            if (result != null && dlg.ReturnCard && result.CardReturnEligible)
            {
                ActionMsg.Text = $"Zwrot karnetu {passId}: {refundForDays:N2} PLN. Karta zwrócona, kaucja: {result.DepositReturn:N2} PLN.";
            }
            else if (result != null && dlg.ReturnCard)
            {
                ActionMsg.Text = $"Zwrot karnetu {passId}: {refundForDays:N2} PLN. Karta nie zwrócona ({result.CardReturnBlockReason ?? "nieznany powód"}).";
            }
            else
            {
                ActionMsg.Text = $"Zwrot karnetu {passId}: {refundForDays:N2} PLN. Kaucja do rozliczenia w panelu Karty RFID.";
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            ActionMsg.Text = $"Błąd: {ex.Message}";
        }
    }
}
