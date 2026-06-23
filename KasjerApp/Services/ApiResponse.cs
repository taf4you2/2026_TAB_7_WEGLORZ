using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace KasjerApp.Services;

internal static class ApiResponse
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<T> ReadRequiredJsonAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return value ?? throw new InvalidOperationException("API zwrocilo pusta odpowiedz.");
    }

    public static async Task EnsureSuccessOrThrowApiMessageAsync(HttpResponseMessage response)
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
