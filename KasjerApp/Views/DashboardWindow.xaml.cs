using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using KasjerApp.Services;
using KasjerApp.Views.Panels;

namespace KasjerApp.Views;

public partial class DashboardWindow : Window
{
    private const string HelpDocumentRelativePath = @"Dokumentacja\Instrukcja_KasjerApp.docx";
    private readonly ApiService _api;
    private Button? _activeBtn;

    public DashboardWindow()
    {
        InitializeComponent();
        SetTooltipsEnabled(true);
        _api = new ApiService(AppConfig.ApiBaseUrl, Session.Token!);
        UserLabel.Text = $"Kasjer ID: {Session.UserId}";
        Navigate(BtnReservations, new ReservationsPanel(_api));
    }

    internal void Navigate(Button btn, UserControl panel)
    {
        if (_activeBtn != null) _activeBtn.IsEnabled = true;
        btn.IsEnabled = false;
        _activeBtn = btn;
        ContentArea.Content = panel;
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        UserControl panel = (string)btn.Tag switch
        {
            "reservations" => new ReservationsPanel(_api),
            "sell_pass" => new SellPassPanel(_api),
            "passes" => new PassesPanel(_api),
            "cards" => new CardsPanel(_api),
            "transactions" => new TransactionsPanel(_api),
            _ => new ReservationsPanel(_api)
        };
        Navigate(btn, panel);
    }

    private void LogoutBtn_Click(object sender, RoutedEventArgs e)
    {
        Session.Token = null;
        Session.UserId = null;
        Session.Role = null;
        new LoginWindow().Show();
        Close();
    }

    private void TooltipsToggle_Changed(object sender, RoutedEventArgs e)
    {
        SetTooltipsEnabled(TooltipsToggle.IsChecked == true);
    }

    private static void SetTooltipsEnabled(bool isEnabled)
    {
        Application.Current.Resources["TooltipsEnabled"] = isEnabled;
    }
}
