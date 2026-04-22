using System.Windows;
using System.Windows.Controls;
using KasjerApp.Services;
using KasjerApp.Views.Panels;

namespace KasjerApp.Views;

public partial class DashboardWindow : Window
{
    private readonly ApiService _api;
    private Button? _activeBtn;

    public DashboardWindow()
    {
        InitializeComponent();
        _api = new ApiService(AppConfig.ApiBaseUrl, Session.Token!);
        UserLabel.Text = $"Kasjer ID: {Session.UserId}";
        Navigate(BtnStats, new StatsPanel(_api));
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
            "stats"        => new StatsPanel(_api),
            "sell_ticket"  => new SellTicketPanel(_api),
            "sell_pass"    => new SellPassPanel(_api),
            "passes"       => new PassesPanel(_api),
            "cards"        => new CardsPanel(_api),
            "transactions" => new TransactionsPanel(_api),
            "shift"        => new ShiftReportPanel(_api),
            "pending"      => new PendingReturnsPanel(_api),
            _              => new StatsPanel(_api)
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
}
