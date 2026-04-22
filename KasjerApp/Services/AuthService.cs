using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using KasjerApp.Models;

namespace KasjerApp.Services;

public class AuthService
{
    private readonly HttpClient _http;

    public AuthService(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    // Zwraca LoginResponse przy sukcesie, null przy błędnych danych (401).
    // Rzuca HttpRequestException przy problemie z połączeniem.
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var request = new LoginRequest(email, password, "kasjer");
        var response = await _http.PostAsJsonAsync("/api/auth/login", request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResponse>();
    }
}
