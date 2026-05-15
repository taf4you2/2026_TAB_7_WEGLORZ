using System.IO;
using System.Text.Json;

namespace KasjerApp.Services;

public static class LoginCredentialsStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KasjerApp",
        "last-login.json");

    public static SavedLoginCredentials? Load()
    {
        try
        {
            if (!File.Exists(StorePath))
                return null;

            var json = File.ReadAllText(StorePath);
            return JsonSerializer.Deserialize<SavedLoginCredentials>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(string email, string password)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
            var json = JsonSerializer.Serialize(
                new SavedLoginCredentials(email, password),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StorePath, json);
        }
        catch
        {
            // Convenience-only feature for the lab project; failed persistence should not block login.
        }
    }
}

public record SavedLoginCredentials(string Email, string Password);
