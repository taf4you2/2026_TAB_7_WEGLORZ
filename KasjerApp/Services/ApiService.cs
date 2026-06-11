using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using KasjerApp.Models;

namespace KasjerApp.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService(string baseUrl, string token)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<TariffDto>> GetTariffsAsync()
    {
        var response = await _http.GetAsync("/api/taryfy");
        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<List<TariffDto>>() ?? [];
    }

    public async Task<CardDto?> GetCardAsync(string rfid)
    {
        var response = await _http.GetAsync($"/api/karty/{Uri.EscapeDataString(rfid)}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<CardDto>();
    }

    public async Task<CardIssueVerificationDto?> VerifyCardForIssueAsync(string rfid)
    {
        var response = await _http.GetAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/weryfikacja-wydania");
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("Endpoint weryfikacji karty nie jest dostępny w API.");

        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<CardIssueVerificationDto>();
    }

    public async Task<List<CardDto>> GetCardsAsync(string? status = null, string? search = null)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");

        var url = "/api/karty" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        var response = await _http.GetAsync(url);
        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<List<CardDto>>() ?? [];
    }

    public async Task IssueCardAsync(string rfid)
    {
        var response = await _http.PostAsJsonAsync("/api/karty", new IssueCardRequest(rfid));
        await EnsureSuccessOrThrowApiMessageAsync(response);
    }

    public async Task DeleteCardAsync(string rfid)
    {
        var response = await _http.DeleteAsync($"/api/karty/{Uri.EscapeDataString(rfid)}");
        await EnsureSuccessOrThrowApiMessageAsync(response);
    }

    public async Task BlockCardAsync(string rfid, string reason)
    {
        var response = await _http.PostAsJsonAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/blokuj", new BlockCardRequest(reason));
        await EnsureSuccessOrThrowApiMessageAsync(response);
    }

    public async Task UnblockCardAsync(string rfid)
    {
        var response = await _http.PostAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/odblokuj", null);
        await EnsureSuccessOrThrowApiMessageAsync(response);
    }

    public async Task<CardReturnDto?> ReturnCardAsync(string rfid)
    {
        var response = await _http.PostAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/zwrot", null);
        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<CardReturnDto>();
    }

    public async Task<List<PassDto>> GetPassesByCardAsync(string rfid)
    {
        var response = await _http.GetAsync($"/api/karnety?cardId={Uri.EscapeDataString(rfid)}");
        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<List<PassDto>>() ?? [];
    }

    public async Task<PassDto?> SellPassAsync(CreatePassRequest req)
    {
        var response = await _http.PostAsJsonAsync("/api/karnety", req);
        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<PassDto>();
    }

    public async Task<ReservedPassActivationResponse?> ActivateReservedPassAsync(ActivatePassRequest req)
    {
        var response = await _http.PostAsJsonAsync("/api/karnety/zatwierdz-odbior", req);
        await EnsureSuccessOrThrowApiMessageAsync(response);

        var body = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(body)
            ? null
            : JsonSerializer.Deserialize<ReservedPassActivationResponse>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public async Task<List<ReservationSearchDto>> GetReservationsByEmailAsync(string email)
    {
        var response = await _http.GetAsync($"/api/karnety/rezerwacje/{Uri.EscapeDataString(email)}");
        if (response.StatusCode == HttpStatusCode.NotFound) return [];

        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ReservationSearchDto>>() ?? [];
    }

    public async Task<ReturnPreviewDto?> GetReturnPreviewAsync(int id, bool returnCard)
    {
        var response = await _http.GetAsync($"/api/karnety/{id}/symulacja-zwrotu?returnCard={returnCard}");
        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<ReturnPreviewDto>();
    }

    public async Task<ReturnPreviewDto?> ReturnPassAsync(int id, ReturnPassRequest req)
    {
        var response = await _http.PostAsJsonAsync($"/api/karnety/{id}/zwrot", req);
        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<ReturnPreviewDto>();
    }

    public async Task<List<UserDto>> SearchUsersAsync(string email)
    {
        var response = await _http.GetAsync($"/api/uzytkownicy?email={Uri.EscapeDataString(email)}");
        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? [];
    }

    public async Task<UserDto?> CreateUserAsync(string email)
    {
        var response = await _http.PostAsJsonAsync("/api/uzytkownicy", new CreateUserRequest(email));
        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(DateOnly? date = null)
    {
        var qs = new List<string>();
        if (date.HasValue) qs.Add($"date={date.Value:yyyy-MM-dd}");

        var url = "/api/transakcje" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        var response = await _http.GetAsync(url);
        await EnsureSuccessOrThrowApiMessageAsync(response);
        return await response.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? [];
    }

    private static async Task EnsureSuccessOrThrowApiMessageAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var message = await TryReadApiMessageAsync(response);
        throw new InvalidOperationException(message ?? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
    }

    private static async Task<string?> TryReadApiMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
