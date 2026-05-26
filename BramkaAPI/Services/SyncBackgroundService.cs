using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models.DTOs;

namespace BramkaAPI.Services;

public class SyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncBackgroundService> _logger;
    private readonly HttpClient _httpClient;
    private const string SystemApiUrl = "http://localhost:5000/api/gatescansync"; // Docelowo z konfiguracji

    public SyncBackgroundService(IServiceProvider serviceProvider, ILogger<SyncBackgroundService> logger, IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncScansAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił błąd podczas synchronizacji odbić.");
            }

            // Czekaj 30 sekund przed kolejną próbą
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task SyncScansAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiResortDbContext>();

        // Pobierz np. 100 najstarszych rekordów
        var scansToSync = await db.GateScans
            .OrderBy(gs => gs.ScanTime)
            .Take(100)
            .ToListAsync(stoppingToken);

        if (!scansToSync.Any())
        {
            return;
        }

        var dtos = scansToSync.Select(gs => new GateScanSyncDto
        {
            CardId = gs.CardId,
            GateId = gs.GateId,
            ScanTime = gs.ScanTime,
            TimeBlockedUntil = gs.TimeBlockedUntil,
            VerificationResultId = gs.VerificationResultId,
            PassTypeId = gs.PassTypeId
        }).ToList();

        // Wyślij do SystemAPI
        var response = await _httpClient.PostAsJsonAsync(SystemApiUrl, dtos, stoppingToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation($"Pomyślnie zsynchronizowano {dtos.Count} odbić.");
            
            // Usuń z lokalnej bazy
            db.GateScans.RemoveRange(scansToSync);
            await db.SaveChangesAsync(stoppingToken);
        }
        else
        {
            _logger.LogWarning($"Nie udało się zsynchronizować odbić. Status code: {response.StatusCode}");
        }
    }
}
