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
    private PassDto? _selectedPass;

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
        ShowMsg("");
        _selectedCard = null;
        ResetPassesSection();
        try
        {
            var search = SearchBox.Text.Trim();
            var status = (StatusCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (status?.StartsWith("(") == true) status = null;
            var cards = await _api.GetCardsAsync(status, string.IsNullOrEmpty(search) ? null : search);
            CardsGrid.ItemsSource = cards;
            if (cards.Count == 0) ShowMsg("Brak kart spelniajacych kryteria.");
        }
        catch (Exception ex)
        {
            ShowMsg(ex.Message);
        }
    }

    private async void CardsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedCard = CardsGrid.SelectedItem as CardDto;
        ResetPassesSection();
        if (_selectedCard == null) return;

        PassesLabel.Text = $"Historia karnetow dla karty: {_selectedCard.Id}";
        try
        {
            var passes = await _api.GetPassesByCardAsync(_selectedCard.Id);
            PassesGrid.ItemsSource = passes;
            if (passes.Count == 0)
            {
                PassesErrorText.Text = "Ta karta nie ma zadnych karnetow.";
                PassesErrorText.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            PassesErrorText.Text = ex.Message;
            PassesErrorText.Visibility = Visibility.Visible;
        }
    }

    private void PassesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPass = PassesGrid.SelectedItem as PassDto;
        UpdateCardButtons();
    }

    private async void BlockBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCard == null) return;
        var reason = AskText("Blokada karty", "Podaj powod blokady:");
        if (string.IsNullOrWhiteSpace(reason)) return;

        try
        {
            await _api.BlockCardAsync(_selectedCard.Id, reason);
            ShowMsg($"Karta {_selectedCard.Id} zablokowana.", true);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowMsg(ex.Message);
        }
    }

    private async void UnblockBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCard == null) return;
        try
        {
            await _api.UnblockCardAsync(_selectedCard.Id);
            ShowMsg($"Karta {_selectedCard.Id} odblokowana.", true);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowMsg(ex.Message);
        }
    }

    private async void ReturnCardBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCard == null) return;
        var expectedDeposit = _selectedCard.DepositPaid ? "20 zl" : "0 zl";
        if (MessageBox.Show(Window.GetWindow(this), $"Zwroc fizyczna karte {_selectedCard.Id}? Kaucja do wyplaty: {expectedDeposit}.", "Zwrot karty", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            var result = await _api.ReturnCardAsync(_selectedCard.Id);
            ShowMsg($"Karta {_selectedCard.Id} zwrocona. Kaucja: {result?.DepositReturn ?? 0:N2} zl.", true);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowMsg(ex.Message);
        }
    }

    private async Task RefreshPassesAsync()
    {
        if (_selectedCard == null) return;
        var passes = await _api.GetPassesByCardAsync(_selectedCard.Id);
        PassesGrid.ItemsSource = passes;
    }

    private void ResetPassesSection()
    {
        _selectedPass = null;
        PassesGrid.ItemsSource = null;
        PassesErrorText.Visibility = Visibility.Collapsed;
        PassesLabel.Text = "Historia karnetow - wybierz karte powyzej";
        UpdateCardButtons();
    }

    private void UpdateCardButtons()
    {
        BlockBtn.IsEnabled = _selectedCard != null && _selectedCard.Status != "zastrzezony";
        UnblockBtn.IsEnabled = _selectedCard != null && _selectedCard.Status == "zastrzezony";
        ReturnCardBtn.IsEnabled = _selectedCard != null;
    }

    private async void AddCardBtn_Click(object sender, RoutedEventArgs e)
    {
        var rfid = AskText("Dodaj karte RFID", "RFID karty:");
        if (string.IsNullOrWhiteSpace(rfid)) return;
        try
        {
            await _api.IssueCardAsync(rfid);
            ShowMsg($"Karta {rfid} dodana.", true);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowMsg(ex.Message);
        }
    }

    private async void DeleteCardBtn_Click(object sender, RoutedEventArgs e)
    {
        var selected = CardsGrid.SelectedItem as CardDto;
        var rfid = selected?.Id ?? AskText("Usun karte RFID", "RFID karty do usuniecia:");
        if (string.IsNullOrWhiteSpace(rfid)) return;

        if (MessageBox.Show(Window.GetWindow(this), $"Usunac karte {rfid}?", "Usun karte", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            await _api.DeleteCardAsync(rfid);
            ShowMsg($"Karta {rfid} usunieta.", true);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowMsg(ex.Message);
        }
    }

    private static string? AskText(string title, string label)
    {
        var win = new Window { Title = title, Width = 390, Height = 180, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = System.Windows.Media.Brushes.White };
        var sp = new StackPanel { Margin = new Thickness(20) };
        sp.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 6) });
        var tb = new TextBox { Height = 36, Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 14) };
        sp.Children.Add(tb);
        var btn = new Button { Content = "OK", Width = 80, Height = 34, Background = System.Windows.Media.Brushes.ForestGreen, Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0) };
        sp.Children.Add(btn);
        win.Content = sp;
        string? result = null;
        btn.Click += (_, _) => { result = tb.Text.Trim(); win.DialogResult = true; };
        return win.ShowDialog() == true ? result : null;
    }

    private void ShowMsg(string msg, bool success = false)
    {
        MsgText.Text = msg;
        MsgText.Foreground = success ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Crimson;
    }
}
