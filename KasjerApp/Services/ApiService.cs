using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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

    // ── Statystyki ────────────────────────────────────────────────────────────
    public async Task<DashboardDto?> GetStatsAsync()
    {
        var r = await _http.GetAsync("/api/statystyki/dzisiaj");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<DashboardDto>();
    }

    // ── Taryfy ────────────────────────────────────────────────────────────────
    public async Task<List<TariffDto>> GetTariffsAsync()
    {
        var r = await _http.GetAsync("/api/taryfy");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<TariffDto>>() ?? [];
    }

    // ── Karty RFID ────────────────────────────────────────────────────────────
    public async Task<CardDto?> GetCardAsync(string rfid)
    {
        var r = await _http.GetAsync($"/api/karty/{Uri.EscapeDataString(rfid)}");
        if (r.StatusCode == HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CardDto>();
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

    // ── Karnety ───────────────────────────────────────────────────────────────
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

    public async Task BlockPassAsync(int id, string reason)
    {
        var r = await _http.PostAsJsonAsync($"/api/karnety/{id}/blokuj", new BlockPassRequest(reason));
        r.EnsureSuccessStatusCode();
    }

    public async Task<ReturnPreviewDto?> GetReturnPreviewAsync(int id, bool returnCard)
    {
        var r = await _http.GetAsync($"/api/karnety/{id}/symulacja-zwrotu?returnCard={returnCard}");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ReturnPreviewDto>();
    }

    public async Task ReturnPassAsync(int id, ReturnPassRequest req)
    {
        var r = await _http.PostAsJsonAsync($"/api/karnety/{id}/zwrot", req);
        r.EnsureSuccessStatusCode();
    }

    // ── Użytkownicy ───────────────────────────────────────────────────────────
    public async Task<List<UserDto>> SearchUsersAsync(string email)
    {
        var r = await _http.GetAsync($"/api/uzytkownicy?email={Uri.EscapeDataString(email)}");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<UserDto>>() ?? [];
    }

    // ── Bilety (UC1) ──────────────────────────────────────────────────────────
    public async Task<SellTicketResponse?> SellTicketAsync(SellTicketRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/bilety", req);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<SellTicketResponse>();
    }

    // ── Transakcje ────────────────────────────────────────────────────────────
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

    // ── Raport zmiany ─────────────────────────────────────────────────────────
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

    // ── Oczekujące zwroty ─────────────────────────────────────────────────────
    public async Task<List<PendingReturnDto>> GetPendingReturnsAsync()
    {
        var r = await _http.GetAsync("/api/zwroty/oczekujace");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<PendingReturnDto>>() ?? [];
    }
}
