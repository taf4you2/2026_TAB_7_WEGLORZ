using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KasjerApp.Models;
using KasjerApp.Services;

namespace KasjerApp.Views.Panels;

public partial class CardsPanel : UserControl
{
    private readonly ApiService _api;
    private CardDto? _selectedCard;

    public CardsPanel(ApiService api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) => await LoadAsync();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SearchBtn_Click(this, new RoutedEventArgs());
    }

    private async void SearchBtn_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        MsgText.Visibility = Visibility.Collapsed;
        ResetPassesSection();
        try
        {
            var search = SearchBox.Text.Trim();
            var status = (StatusCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (status?.StartsWith("(") == true) status = null;
            var cards = await _api.GetCardsAsync(status, string.IsNullOrEmpty(search) ? null : search);
            CardsGrid.ItemsSource = cards;
            if (cards.Count == 0) ShowMsg("Brak kart spełniających kryteria.");
        }
        catch (Exception ex)
        {
            ShowMsg(ex.Message);
        }
    }

    private async void CardsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var card = CardsGrid.SelectedItem as CardDto;
        ResetPassesSection();
        if (card == null) return;
        _selectedCard = card;

        PassesLabel.Text = $"Historia karnetów dla karty: {_selectedCard.Id}";
        try
        {
            var passes = await _api.GetPassesByCardAsync(_selectedCard.Id);
            PassesGrid.ItemsSource = passes;
            if (passes.Count == 0)
            {
                PassesErrorText.Text = "Ta karta nie ma żadnych karnetów.";
                PassesErrorText.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            PassesErrorText.Text = ex.Message;
            PassesErrorText.Visibility = Visibility.Visible;
        }
    }

    private void ResetPassesSection()
    {
        _selectedCard = null;
        PassesGrid.ItemsSource = null;
        PassesErrorText.Visibility = Visibility.Collapsed;
        PassesLabel.Text = "Historia karnetów – wybierz kartę powyżej";
    }

    private async void AddCardBtn_Click(object sender, RoutedEventArgs e)
    {
        var rfid = AskRfid();
        if (rfid == null) return;
        try
        {
            await _api.IssueCardAsync(rfid);
            ShowMsg($"Karta {rfid} dodana.", success: true);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowMsg(ex.Message);
        }
    }

    private static string? AskRfid()
    {
        var win = new Window
        {
            Title = "Dodaj kartę RFID", Width = 380, Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Background = System.Windows.Media.Brushes.White
        };
        var sp = new StackPanel { Margin = new Thickness(20) };
        sp.Children.Add(new TextBlock { Text = "RFID karty (np. AA:BB:CC:DD):", Margin = new Thickness(0, 0, 0, 6) });
        var tb = new TextBox { Height = 36, Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 14) };
        sp.Children.Add(tb);
        var btn = new Button { Content = "Dodaj", Width = 80, Height = 34,
            Background = System.Windows.Media.Brushes.ForestGreen,
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0) };
        sp.Children.Add(btn);
        win.Content = sp;
        string? result = null;
        btn.Click += (_, _) => { result = tb.Text.Trim(); win.DialogResult = true; };
        return win.ShowDialog() == true && !string.IsNullOrEmpty(result) ? result : null;
    }

    private void ShowMsg(string msg, bool success = false)
    {
        MsgText.Text = msg;
        MsgText.Foreground = success
            ? System.Windows.Media.Brushes.DarkGreen
            : System.Windows.Media.Brushes.Crimson;
        MsgText.Visibility = Visibility.Visible;
    }
}
