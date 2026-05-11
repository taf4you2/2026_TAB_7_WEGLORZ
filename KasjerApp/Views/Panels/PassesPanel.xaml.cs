using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KasjerApp.Models;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class PassesPanel : UserControl
{
    private readonly ApiService _api;
    private PassListItem? _selected;

    public PassesPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void PassesPanel_Loaded(object sender, RoutedEventArgs e) => await LoadAsync();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SearchBtn_Click(this, new RoutedEventArgs());
    }

    private async void SearchBtn_Click(object sender, RoutedEventArgs e) => await LoadAsync(SearchBox.Text.Trim());
    private async void RefreshBtn_Click(object sender, RoutedEventArgs e) => await LoadAsync(SearchBox.Text.Trim());

    private async Task LoadAsync(string? search = null)
    {
        ActionMsg.Text = "";
        ReturnBtn.IsEnabled = false;
        _selected = null;

        try
        {
            var cards = await _api.GetCardsAsync(search: string.IsNullOrWhiteSpace(search) ? null : search);
            var rows = new List<PassListItem>();
            foreach (var card in cards)
            {
                var passes = await _api.GetPassesByCardAsync(card.Id);
                foreach (var pass in passes)
                {
                    rows.Add(new PassListItem(
                        pass.Id,
                        card.Id,
                        pass.Status,
                        pass.Tariff,
                        pass.PassType,
                        pass.ValidFrom,
                        pass.ValidTo,
                        pass.RemainingRides));
                }
            }

            PassesGrid.ItemsSource = rows;
            if (rows.Count == 0) ActionMsg.Text = "Brak karnetow.";
        }
        catch (Exception ex)
        {
            ActionMsg.Text = ex.Message;
        }
    }

    private void PassesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = PassesGrid.SelectedItem as PassListItem;
        ReturnBtn.IsEnabled = _selected != null;
        ActionMsg.Text = _selected != null ? $"Wybrany karnet ID: {_selected.PassId}" : "";
    }

    private async void ReturnBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;
        var dlg = new ReturnDialog(_api, _selected.PassId) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;

        try
        {
            await _api.ReturnPassAsync(_selected.PassId, new ReturnPassRequest(dlg.Reason, dlg.ReturnCard));
            ActionMsg.Text = $"Zwrot karnetu {_selected.PassId} zatwierdzony.";
            await LoadAsync(SearchBox.Text.Trim());
        }
        catch (Exception ex)
        {
            ActionMsg.Text = $"Blad: {ex.Message}";
        }
    }

    private record PassListItem(
        int PassId,
        string CardId,
        string? Status,
        string? Tariff,
        string? PassType,
        DateTime? ValidFrom,
        DateTime? ValidTo,
        int? RemainingRides);
}
