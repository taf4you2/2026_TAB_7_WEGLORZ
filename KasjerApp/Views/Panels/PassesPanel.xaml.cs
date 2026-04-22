using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KasjerApp.Models;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class PassesPanel : UserControl
{
    private readonly ApiService _api;
    private PassDto? _selected;

    public PassesPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private void RfidBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SearchBtn_Click(this, new RoutedEventArgs());
    }

    private async void SearchBtn_Click(object sender, RoutedEventArgs e)
    {
        SearchErrorText.Visibility = Visibility.Collapsed;
        ActionMsg.Text = "";
        var rfid = RfidBox.Text.Trim();
        if (string.IsNullOrEmpty(rfid)) { ShowSearchError("Podaj RFID karty."); return; }

        try
        {
            var passes = await _api.GetPassesByCardAsync(rfid);
            PassesGrid.ItemsSource = passes;
            if (passes.Count == 0) ShowSearchError("Brak karnetów dla tej karty.");
        }
        catch (Exception ex)
        {
            ShowSearchError(ex.Message);
        }
    }

    private void PassesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = PassesGrid.SelectedItem as PassDto;
        bool hasSelection = _selected != null;
        BlockBtn.IsEnabled = hasSelection;
        ReturnBtn.IsEnabled = hasSelection;
        ActionMsg.Text = _selected != null
            ? $"Wybrany karnet ID: {_selected.Id}"
            : "";
    }

    private async void BlockBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;
        var reason = AskReason("Powód blokady karnetu (UC3):", "Podaj powód blokady:");
        if (reason == null) return;

        try
        {
            await _api.BlockPassAsync(_selected.Id, reason);
            ActionMsg.Text = $"Karnet {_selected.Id} zablokowany.";
            await RefreshPassesAsync();
        }
        catch (Exception ex)
        {
            ActionMsg.Text = $"Błąd: {ex.Message}";
        }
    }

    private async void ReturnBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;

        // Dialog zwrotu
        var dlg = new ReturnDialog(_api, _selected.Id);
        dlg.Owner = Window.GetWindow(this);
        if (dlg.ShowDialog() != true) return;

        try
        {
            await _api.ReturnPassAsync(_selected.Id, new ReturnPassRequest(dlg.Reason, dlg.ReturnCard));
            ActionMsg.Text = $"Zwrot karnetu {_selected.Id} zatwierdzony.";
            await RefreshPassesAsync();
        }
        catch (Exception ex)
        {
            ActionMsg.Text = $"Błąd: {ex.Message}";
        }
    }

    private async Task RefreshPassesAsync()
    {
        var rfid = RfidBox.Text.Trim();
        if (!string.IsNullOrEmpty(rfid))
        {
            try { PassesGrid.ItemsSource = await _api.GetPassesByCardAsync(rfid); }
            catch { /* ignore */ }
        }
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
        var btn = new Button { Content = "OK", Width = 80, Height = 34,
            Background = System.Windows.Media.Brushes.DodgerBlue,
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0) };
        sp.Children.Add(btn);
        win.Content = sp;
        string? result = null;
        btn.Click += (_, _) => { result = tb.Text.Trim(); win.DialogResult = true; };
        return win.ShowDialog() == true && !string.IsNullOrWhiteSpace(result) ? result : null;
    }

    private void ShowSearchError(string msg)
    {
        SearchErrorText.Text = msg;
        SearchErrorText.Visibility = Visibility.Visible;
    }
}
