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

    public async Task<List<TariffDto>> GetTariffsAsync()
    {
        var response = await _http.GetAsync("/api/taryfy");
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<List<TariffDto>>(response);
    }

    public async Task<List<LiftDto>> GetLiftsAsync()
    {
        var response = await _http.GetAsync("/api/wyciagi");
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<List<LiftDto>>(response);
    }

    public async Task<DateTime> GetMinimumSaleDateAsync()
    {
        var today = DateTime.Today;
        var closingTimes = (await GetLiftsAsync())
            .Where(l => l.IsActive && l.ClosesAt.HasValue)
            .Select(l => l.ClosesAt!.Value)
            .ToList();

        return closingTimes.Count > 0 && DateTime.Now.TimeOfDay > closingTimes.Max()
            ? today.AddDays(1)
            : today;
    }

    public async Task<CardIssueVerificationDto> VerifyCardForIssueAsync(string rfid)
    {
        var response = await _http.GetAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/weryfikacja-wydania");
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<CardIssueVerificationDto>(response);
    }

    public async Task<List<CardDto>> GetCardsAsync(string? status = null, string? search = null)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");

        var url = "/api/karty" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        var response = await _http.GetAsync(url);
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<List<CardDto>>(response);
    }

    public async Task IssueCardAsync(string rfid)
    {
        var response = await _http.PostAsJsonAsync("/api/karty", new IssueCardRequest(rfid));
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
    }

    public async Task DeleteCardAsync(string rfid)
    {
        var response = await _http.DeleteAsync($"/api/karty/{Uri.EscapeDataString(rfid)}");
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
    }

    public async Task BlockCardAsync(string rfid, string reason)
    {
        var response = await _http.PostAsJsonAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/blokuj", new BlockCardRequest(reason));
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
    }

    public async Task UnblockCardAsync(string rfid)
    {
        var response = await _http.PostAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/odblokuj", null);
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
    }

    public async Task<CardReturnDto> ReturnCardAsync(string rfid)
    {
        var response = await _http.PostAsync($"/api/karty/{Uri.EscapeDataString(rfid)}/zwrot", null);
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<CardReturnDto>(response);
    }

    public async Task<List<PassDto>> GetPassesByCardAsync(string rfid)
    {
        var response = await _http.GetAsync($"/api/karnety?cardId={Uri.EscapeDataString(rfid)}");
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<List<PassDto>>(response);
    }

    public async Task<PassDto> SellPassAsync(CreatePassRequest req)
    {
        var response = await _http.PostAsJsonAsync("/api/karnety", req);
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<PassDto>(response);
    }

    public async Task<ReservedPassActivationResponse> ActivateReservedPassAsync(ActivatePassRequest req)
    {
        var response = await _http.PostAsJsonAsync("/api/karnety/zatwierdz-odbior", req);
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<ReservedPassActivationResponse>(response);
    }

    public async Task<List<ReservationSearchDto>> GetReservationsByEmailAsync(string email)
    {
        var response = await _http.GetAsync($"/api/karnety/rezerwacje/{Uri.EscapeDataString(email)}");
        if (response.StatusCode == HttpStatusCode.NotFound) return [];

        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<List<ReservationSearchDto>>(response);
    }

    public async Task<ReturnPreviewDto> GetReturnPreviewAsync(int id, bool returnCard)
    {
        var response = await _http.GetAsync($"/api/karnety/{id}/symulacja-zwrotu?returnCard={returnCard}");
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<ReturnPreviewDto>(response);
    }

    public async Task<ReturnPreviewDto> ReturnPassAsync(int id, ReturnPassRequest req)
    {
        var response = await _http.PostAsJsonAsync($"/api/karnety/{id}/zwrot", req);
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<ReturnPreviewDto>(response);
    }

    public async Task<List<UserDto>> SearchUsersAsync(string email)
    {
        var response = await _http.GetAsync($"/api/uzytkownicy?email={Uri.EscapeDataString(email)}");
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<List<UserDto>>(response);
    }

    public async Task<UserDto> CreateUserAsync(string email)
    {
        var response = await _http.PostAsJsonAsync("/api/uzytkownicy", new CreateUserRequest(email));
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<UserDto>(response);
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(DateOnly? date = null, int? cashierId = null)
    {
        var qs = new List<string>();
        if (date.HasValue) qs.Add($"date={date.Value:yyyy-MM-dd}");
        if (cashierId.HasValue) qs.Add($"cashierId={cashierId.Value}");

        var url = "/api/transakcje" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        var response = await _http.GetAsync(url);
        await ApiResponse.EnsureSuccessOrThrowApiMessageAsync(response);
        return await ApiResponse.ReadRequiredJsonAsync<List<TransactionDto>>(response);
    }
}
