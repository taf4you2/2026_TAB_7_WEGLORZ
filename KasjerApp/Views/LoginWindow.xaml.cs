using System.Windows;
using KasjerApp.Services;

namespace KasjerApp.Views;

public partial class LoginWindow : Window
{
    private readonly AuthService _auth = new(AppConfig.ApiBaseUrl);

    public LoginWindow()
    {
        InitializeComponent();
        // Enter zatwierdza formularz
        PasswordBox.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                LoginBtn_Click(this, new RoutedEventArgs());
        };
    }

    private async void LoginBtn_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        LoginBtn.IsEnabled = false;
        LoginBtn.Content = "Logowanie...";

        try
        {
            var result = await _auth.LoginAsync(EmailBox.Text.Trim(), PasswordBox.Password);

            if (result == null)
            {
                ErrorText.Text = "Nieprawidłowy login lub hasło.";
                ErrorText.Visibility = Visibility.Visible;
                PasswordBox.Focus();
                return;
            }

            Session.Token = result.Token;
            Session.UserId = result.UserId;
            Session.Role = result.Role;

            new DashboardWindow().Show();
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Błąd połączenia z API: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoginBtn.IsEnabled = true;
            LoginBtn.Content = "Zaloguj się";
        }
    }
}
