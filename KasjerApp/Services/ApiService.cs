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

    // â”€â”€ Statystyki â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // â”€â”€ Taryfy â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public async Task<List<TariffDto>> GetTariffsAsync()
    {
        var r = await _http.GetAsync("/api/taryfy");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<TariffDto>>() ?? [];
    }

    public async Task<List<LiftDto>> GetLiftsAsync()
    {
        var r = await _http.GetAsync("/api/wyciagi");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<LiftDto>>() ?? [];
    }

    public async Task<DateTime> GetMinimumSaleDateAsync()
    {
        var today = DateTime.Today;

        try
        {
            var closingTimes = (await GetLiftsAsync())
                .Where(l => l.IsActive && l.ClosesAt.HasValue)
                .Select(l => l.ClosesAt!.Value)
                .ToList();

            if (closingTimes.Count > 0 && DateTime.Now.TimeOfDay > closingTimes.Max())
                return today.AddDays(1);
        }
        catch
        {
            return today;
        }

        return today;
    }

    // â”€â”€ Karty RFID â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public async Task<CardDto?> GetCardAsync(string rfid)
    {
        var r = await _http.GetAsync($"/api/karty/{Uri.EscapeDataString(rfid)}");
        if (r.StatusCode == HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CardDto>();
    }

    public async Task<CardIssueVerificationDto?> VerifyCardForIssueAsync(string rfid)
    {
        var r = await _http.GetAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/weryfikacja-wydania");
        if (r.StatusCode == HttpStatusCode.NotFound)
        {
            return await VerifyCardForIssueFallbackAsync(rfid);
        }

        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CardIssueVerificationDto>();
    }

    private async Task<CardIssueVerificationDto> VerifyCardForIssueFallbackAsync(string rfid)
    {
        var card = await GetCardAsync(rfid);
        if (card == null)
            return new CardIssueVerificationDto(false, $"Karta {rfid} nie istnieje w systemie.", null);

        if (card.ActivePassId.HasValue)
            return new CardIssueVerificationDto(false, $"Karta ma aktywny karnet: {card.ActivePassType ?? $"ID {card.ActivePassId.Value}"}.", card);

        var canIssue = card.Status.Equals("wolna", StringComparison.OrdinalIgnoreCase) ||
                       card.Status.Equals("free", StringComparison.OrdinalIgnoreCase) ||
                       card.Status.Equals("available", StringComparison.OrdinalIgnoreCase);

        return canIssue
            ? new CardIssueVerificationDto(true, "Karta jest wolna i gotowa do wydania.", card)
            : new CardIssueVerificationDto(false, $"Karta ma status '{card.Status}'.", card);
    }

    public async Task<List<CardDto>> GetCardsAsync(string? status = null, string? search = null)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        var url = "/api/karty" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        var r = await _http.GetAsync(url);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<CardDto>>() ?? [];
    }

    public async Task IssueCardAsync(string rfid)
    {
        var r = await _http.PostAsJsonAsync("/api/karty", new IssueCardRequest(rfid));
        r.EnsureSuccessStatusCode();
    }

    public async Task DeleteCardAsync(string rfid)
    {
        var r = await _http.DeleteAsync($"/api/karty/{Uri.EscapeDataString(rfid)}");
        r.EnsureSuccessStatusCode();
    }

    public async Task BlockCardAsync(string rfid, string reason)
    {
        var r = await _http.PostAsJsonAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/blokuj", new BlockCardRequest(reason));
        r.EnsureSuccessStatusCode();
    }

    public async Task UnblockCardAsync(string rfid)
    {
        var r = await _http.PostAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/odblokuj", null);
        r.EnsureSuccessStatusCode();
    }

    public async Task<CardReturnDto?> ReturnCardAsync(string rfid)
    {
        var r = await _http.PostAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/zwrot", null);
        await EnsureSuccessOrThrowApiMessageAsync(r);
        return await r.Content.ReadFromJsonAsync<CardReturnDto>();
    }

    private static async Task EnsureSuccessOrThrowApiMessageAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync();
        string? message = null;
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("message", out var msg) &&
                    msg.ValueKind == JsonValueKind.String)
                {
                    message = msg.GetString();
                }
            }
            catch (JsonException)
            {
                // body nie jest JSON-em — zostawiamy null
            }
        }

        throw new InvalidOperationException(message ?? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
    }

    // â”€â”€ Karnety â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public async Task<List<PassDto>> GetPassesByCardAsync(string rfid)
    {
        var r = await _http.GetAsync($"/api/karnety?cardId={Uri.EscapeDataString(rfid)}");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<PassDto>>() ?? [];
    }

    public async Task<PassDto?> GetPassAsync(int id)
    {
        var r = await _http.GetAsync($"/api/karnety/{id}");
        if (r.StatusCode == HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PassDto>();
    }

    public async Task<PassDto?> SellPassAsync(CreatePassRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/karnety", req);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PassDto>();
    }

    public async Task<ReservedPassActivationResponse?> ActivateReservedPassAsync(ActivatePassRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/karnety/zatwierdz-odbior", req);
        r.EnsureSuccessStatusCode();
        var body = await r.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(body)
            ? null
            : JsonSerializer.Deserialize<ReservedPassActivationResponse>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public async Task<List<ReservationSearchDto>> GetReservationsByEmailAsync(string email)
    {
        var r = await _http.GetAsync($"/api/karnety/rezerwacje/{Uri.EscapeDataString(email)}");
        if (r.StatusCode == HttpStatusCode.NotFound) return [];

        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<ReservationSearchDto>>() ?? [];
    }

    public async Task BlockPassAsync(int id, string reason)
    {
        var r = await _http.PostAsJsonAsync($"/api/karnety/{id}/blokuj", new BlockPassRequest(reason));
        r.EnsureSuccessStatusCode();
    }

    public async Task UnblockPassAsync(int id)
    {
        var r = await _http.PostAsync($"/api/karnety/{id}/odblokuj", null);
        r.EnsureSuccessStatusCode();
    }

    public async Task<ReturnPreviewDto?> GetReturnPreviewAsync(int id, bool returnCard)
    {
        var r = await _http.GetAsync($"/api/karnety/{id}/symulacja-zwrotu?returnCard={returnCard}");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ReturnPreviewDto>();
    }

    public async Task<ReturnPreviewDto?> ReturnPassAsync(int id, ReturnPassRequest req)
    {
        var r = await _http.PostAsJsonAsync($"/api/karnety/{id}/zwrot", req);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ReturnPreviewDto>();
    }

    // â”€â”€ UĹĽytkownicy â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public async Task<List<UserDto>> SearchUsersAsync(string email)
    {
        var r = await _http.GetAsync($"/api/uzytkownicy?email={Uri.EscapeDataString(email)}");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<UserDto>>() ?? [];
    }

    public async Task<UserDto?> CreateUserAsync(string email)
    {
        var r = await _http.PostAsJsonAsync("/api/uzytkownicy", new CreateUserRequest(email));
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<UserDto>();
    }

    // â”€â”€ Bilety (UC1) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public async Task<SellTicketResponse?> SellTicketAsync(SellTicketRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/bilety", req);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<SellTicketResponse>();
    }

    // â”€â”€ Transakcje â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public async Task<List<TransactionDto>> GetTransactionsAsync(DateOnly? date = null, int? cashierId = null)
    {
        var qs = new List<string>();
        if (date.HasValue) qs.Add($"date={date.Value:yyyy-MM-dd}");
        if (cashierId.HasValue) qs.Add($"cashierId={cashierId.Value}");
        var url = "/api/transakcje" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        var r = await _http.GetAsync(url);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? [];
    }

    // â”€â”€ Raport zmiany â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // â”€â”€ OczekujÄ…ce zwroty â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public async Task<ShiftReportDto?> GetShiftReportAsync()
    {
        var r = await _http.GetAsync("/api/raport-zmiany");
        if (r.StatusCode == HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ShiftReportDto>();
    }

    public async Task CloseShiftAsync()
    {
        var r = await _http.PostAsync("/api/raport-zmiany/zamknij", null);
        r.EnsureSuccessStatusCode();
    }
    public async Task<List<PendingReturnDto>> GetPendingReturnsAsync()
    {
        var r = await _http.GetAsync("/api/zwroty/oczekujace");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<PendingReturnDto>>() ?? [];
    }
}
