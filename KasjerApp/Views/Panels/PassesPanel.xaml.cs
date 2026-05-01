using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KasjerApp.Models;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class PassesPanel : UserControl
{
    private readonly ApiService _api;
    private CardDto? _selectedCard;
    private PassDto? _selectedPass;

    public PassesPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void PassesPanel_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadCardsAsync();
    }

    // ── Karty ──────────────────────────────────────────────────────────────────

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SearchBtn_Click(this, new RoutedEventArgs());
    }

    private async void SearchBtn_Click(object sender, RoutedEventArgs e)
    {
        await LoadCardsAsync(SearchBox.Text.Trim());
    }

    private async void LoadAllBtn_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
        await LoadCardsAsync();
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        await LoadCardsAsync(SearchBox.Text.Trim());
    }

    private async Task LoadCardsAsync(string? search = null)
    {
        CardsErrorText.Visibility = Visibility.Collapsed;
        ResetPassesSection();

        try
        {
            var cards = await _api.GetCardsAsync(search: search);
            CardsGrid.ItemsSource = cards;
            if (cards.Count == 0) ShowCardsError("Brak kart spełniających kryteria.");
        }
        catch (Exception ex)
        {
            ShowCardsError(ex.Message);
        }
    }

    private async void CardsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedCard = CardsGrid.SelectedItem as CardDto;
        ResetPassesSection();

        if (_selectedCard == null) return;

        PassesLabel.Text = $"Aktywny karnet dla karty: {_selectedCard.Id}";
        PassesErrorText.Visibility = Visibility.Collapsed;

        try
        {
            var passes = await _api.GetPassesByCardAsync(_selectedCard.Id);
            var latest = passes.Count > 0 ? passes.Take(1).ToList() : passes;
            PassesGrid.ItemsSource = latest;
            if (latest.Count == 0) ShowPassesError("Ta karta nie ma żadnych karnetów.");
        }
        catch (Exception ex)
        {
            ShowPassesError(ex.Message);
        }
    }

    // ── Karnety ────────────────────────────────────────────────────────────────

    private void PassesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPass = PassesGrid.SelectedItem as PassDto;
        bool has = _selectedPass != null;
        BlockBtn.IsEnabled = has;
        ReturnBtn.IsEnabled = has;
        ActionMsg.Text = has ? $"Wybrany karnet ID: {_selectedPass!.Id}" : "";
    }

    private async void BlockBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPass == null) return;
        var reason = AskReason("Blokada karnetu (UC3)", "Podaj powód blokady:");
        if (reason == null) return;

        try
        {
            await _api.BlockPassAsync(_selectedPass.Id, reason);
            ActionMsg.Text = $"Karnet {_selectedPass.Id} zablokowany.";
            await RefreshPassesAsync();
        }
        catch (Exception ex)
        {
            ActionMsg.Text = $"Błąd: {ex.Message}";
        }
    }

    private async void ReturnBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPass == null) return;

        var dlg = new ReturnDialog(_api, _selectedPass.Id);
        dlg.Owner = Window.GetWindow(this);
        if (dlg.ShowDialog() != true) return;

        try
        {
            await _api.ReturnPassAsync(_selectedPass.Id, new ReturnPassRequest(dlg.Reason, dlg.ReturnCard));
            ActionMsg.Text = $"Zwrot karnetu {_selectedPass.Id} zatwierdzony.";
            await RefreshPassesAsync();
        }
        catch (Exception ex)
        {
            ActionMsg.Text = $"Błąd: {ex.Message}";
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task RefreshPassesAsync()
    {
        if (_selectedCard == null) return;
        try
        {
            var passes = await _api.GetPassesByCardAsync(_selectedCard.Id);
            PassesGrid.ItemsSource = passes.Count > 0 ? passes.Take(1).ToList() : passes;
        }
        catch { /* ignore */ }
    }

    private void ResetPassesSection()
    {
        _selectedPass = null;
        PassesGrid.ItemsSource = null;
        PassesErrorText.Visibility = Visibility.Collapsed;
        PassesLabel.Text = "Aktywny karnet – wybierz kartę powyżej";
        BlockBtn.IsEnabled = false;
        ReturnBtn.IsEnabled = false;
        ActionMsg.Text = "";
    }

    private void ShowCardsError(string msg)
    {
        CardsErrorText.Text = msg;
        CardsErrorText.Visibility = Visibility.Visible;
    }

    private void ShowPassesError(string msg)
    {
        PassesErrorText.Text = msg;
        PassesErrorText.Visibility = Visibility.Visible;
    }

    private static string? AskReason(string title, string label)
    {
        var win = new Window
        {
            Title = title, Width = 400, Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Background = System.Windows.Media.Brushes.White
        };
        var sp = new StackPanel { Margin = new Thickness(20) };
        sp.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 8) });
        var tb = new TextBox { Height = 36, Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 16) };
        sp.Children.Add(tb);
        var btn = new Button
        {
            Content = "OK", Width = 80, Height = 34,
            Background = System.Windows.Media.Brushes.DodgerBlue,
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0)
        };
        sp.Children.Add(btn);
        win.Content = sp;
        string? result = null;
        btn.Click += (_, _) => { result = tb.Text.Trim(); win.DialogResult = true; };
        return win.ShowDialog() == true && !string.IsNullOrWhiteSpace(result) ? result : null;
    }
}
