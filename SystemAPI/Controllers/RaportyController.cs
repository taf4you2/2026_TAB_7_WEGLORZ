using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

namespace SystemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/raporty")]
public class RaportyController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/raporty/przejazdy?cardId={rfid}&date={YYYY-MM-DD}
    // Historia skanów bramki dla danej karty.
    // date — opcjonalne; bez niego zwraca wszystkie przejazdy.
    [HttpGet("przejazdy")]
    public async Task<IActionResult> GetRideHistory(
        [FromQuery] string cardId,
        [FromQuery] DateOnly? date)
    {
        if (string.IsNullOrEmpty(cardId))
            return BadRequest(new { message = "Parametr cardId jest wymagany." });

        var query = db.GateScans
            .Include(gs => gs.Gate)
                .ThenInclude(g => g!.Lift)
            .Include(gs => gs.VerificationResult)
            .Where(gs => gs.CardId == cardId)
            .AsQueryable();

        if (date.HasValue)
        {
            var from = SkiResortClock.StartOfDay(date.Value);
            var to = from.AddDays(1);
            query = query.Where(gs => gs.ScanTime >= from && gs.ScanTime < to);
        }

        var scans = await query
            .OrderByDescending(gs => gs.ScanTime)
            .ToListAsync();

        var result = scans.Select(gs => new GateScanDto(
            gs.Id,
            gs.CardId,
            gs.Gate?.Name,
            gs.Gate?.Lift?.Name,
            gs.ScanTime,
            gs.VerificationResult?.Name
        ));

        return Ok(result);
    }

    // 1. Raport zmianowy (Kasa)
    [Authorize(Roles = "admin")]
    [HttpGet("zmiany")]
    public async Task<IActionResult> GetShiftReports()
    {
        var reports = await db.ShiftReports
            .Include(r => r.Cashier)
            .OrderByDescending(r => r.StartTime)
            .Select(r => new
            {
                r.Id,
                CashierLogin = r.Cashier != null ? r.Cashier.Login : "Nieznany",
                r.StartTime,
                r.EndTime,
                r.TotalRevenue,
                r.TotalDepositReturns,
                r.CardsIssuedCount
            })
            .ToListAsync();

        return Ok(reports);
    }

    // 2. Raport sprzedaży ogólnej
    [Authorize(Roles = "admin")]
    [HttpGet("sprzedaz-ogolna")]
    public async Task<IActionResult> GetGeneralSalesReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (from > to)
            return BadRequest(new { message = "Data poczatkowa nie moze byc pozniejsza niz data koncowa." });

        var transactions = await db.Transactions
            .Include(t => t.OperationType)
            .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
            .ToListAsync();

        var onsite = transactions.Where(t => t.CashierId != null);
        var online = transactions.Where(t => t.CashierId == null);

        static object BuildChannelSummary(IEnumerable<SystemStacjiNarciarskiejDLL.Models.Transaction> source)
        {
            var list = source.ToList();
            return new
            {
                Amount = list.Sum(t => t.Amount),
                Count = list.Count,
                SalesAmount = list.Where(t => t.Amount > 0).Sum(t => t.Amount),
                SalesCount = list.Count(t => t.Amount > 0),
                ReturnsAmount = Math.Abs(list.Where(t => t.Amount < 0).Sum(t => t.Amount)),
                ReturnsCount = list.Count(t => t.Amount < 0),
                ByOperation = list.GroupBy(t => t.OperationType?.Name ?? "Inne")
                    .Select(g => new
                    {
                        Operation = g.Key,
                        Amount = g.Sum(x => x.Amount),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => Math.Abs(x.Amount))
            };
        }

        var result = new
        {
            TotalRevenue = transactions.Sum(t => t.Amount),
            TransactionCount = transactions.Count,
            GrossSalesAmount = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
            GrossSalesCount = transactions.Count(t => t.Amount > 0),
            ReturnsAmount = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount)),
            ReturnsCount = transactions.Count(t => t.Amount < 0),
            Onsite = BuildChannelSummary(onsite),
            Online = BuildChannelSummary(online),
            GeneratedAt = DateTime.UtcNow
        };

        await LogAdminReportAsync("sprzedaz_ogolna", $"from={from:yyyy-MM-dd};to={to:yyyy-MM-dd}");
        return Ok(result);
    }

    // 3. Raport przepustowości wyciągów
    [Authorize(Roles = "admin")]
    [HttpGet("przepustowosc-wyciagow")]
    public async Task<IActionResult> GetLiftThroughput([FromQuery] DateOnly date)
    {
        var from = SkiResortClock.StartOfDay(date);
        var to = from.AddDays(1);

        var lifts = await db.Lifts
            .OrderBy(l => l.Name)
            .Select(l => new { l.Id, l.Name })
            .ToListAsync();

        var scans = await db.GateScans
            .Where(gs => gs.ScanTime >= from && gs.ScanTime < to 
                     && gs.VerificationResult != null && gs.VerificationResult.Name == "ok"
                     && gs.Gate != null && gs.Gate.LiftId != null)
            .Select(gs => new
            {
                LiftId = gs.Gate!.LiftId!.Value,
                Hour = gs.ScanTime!.Value.Hour
            })
            .ToListAsync();

        var countsByLift = scans
            .GroupBy(s => s.LiftId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(s => s.Hour).ToDictionary(hg => hg.Key, hg => hg.Count()));

        var throughput = lifts.Select(l =>
        {
            countsByLift.TryGetValue(l.Id, out var hourlyCounts);
            var hourlyStats = Enumerable.Range(0, 24)
                .Select(hour => new
                {
                    Hour = hour,
                    Count = hourlyCounts != null && hourlyCounts.TryGetValue(hour, out var count) ? count : 0
                })
                .ToList();

            return new
            {
                LiftId = l.Id,
                LiftName = NormalizeReportText(l.Name),
                TotalScans = hourlyStats.Sum(h => h.Count),
                PeakHour = hourlyStats.OrderByDescending(h => h.Count).First().Hour,
                PeakCount = hourlyStats.Max(h => h.Count),
                HourlyStats = hourlyStats
            };
        }).ToList();

        await LogAdminReportAsync("przepustowosc_wyciagow", $"date={date:yyyy-MM-dd}");
        return Ok(throughput);
    }

    // 4. Historia raportow wygenerowanych przez administratorow
    [Authorize(Roles = "admin")]
    [HttpGet("zaawansowany")]
    public async Task<IActionResult> GetAdvancedReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (from > to)
            return BadRequest(new { message = "Data poczatkowa nie moze byc pozniejsza niz data koncowa." });

        var dayFrom = DateOnly.FromDateTime(from.Date);
        var dayTo = DateOnly.FromDateTime(to.Date);
        var dayCount = dayTo.DayNumber - dayFrom.DayNumber + 1;
        if (dayCount > 370)
            return BadRequest(new { message = "Raport zaawansowany moze obejmowac maksymalnie 370 dni." });

        var transactions = await db.Transactions
            .Include(t => t.OperationType)
            .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
            .ToListAsync();

        var transactionRows = transactions
            .Where(t => t.TransactionDate.HasValue)
            .ToList();

        var dailyRevenue = Enumerable.Range(0, dayCount)
            .Select(offset =>
            {
                var day = dayFrom.AddDays(offset);
                var dailyTransactions = transactionRows
                    .Where(t => DateOnly.FromDateTime(t.TransactionDate!.Value.Date) == day)
                    .ToList();

                return new
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    NetRevenue = dailyTransactions.Sum(t => t.Amount),
                    GrossSales = dailyTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
                    Returns = Math.Abs(dailyTransactions.Where(t => t.Amount < 0).Sum(t => t.Amount)),
                    TransactionCount = dailyTransactions.Count
                };
            })
            .ToList();

        var tariffRows = await db.SkiPasses
            .Include(p => p.Tariff)
                .ThenInclude(t => t!.PassType)
            .Include(p => p.Reservation)
                .ThenInclude(r => r!.Transactions)
            .Where(p => p.Reservation != null
                && p.Reservation.Transactions.Any(t => t.TransactionDate >= from && t.TransactionDate <= to && t.Amount > 0))
            .Select(p => new
            {
                TariffName = p.Tariff != null ? p.Tariff.Name : "Brak taryfy",
                PassType = p.Tariff != null && p.Tariff.PassType != null ? p.Tariff.PassType.Name : "Inny",
                Amount = p.Reservation!.Transactions
                    .Where(t => t.TransactionDate >= from && t.TransactionDate <= to && t.Amount > 0)
                    .Sum(t => t.Amount)
            })
            .ToListAsync();

        var tariffSales = tariffRows
            .GroupBy(t => new { t.TariffName, t.PassType })
            .Select(g => new
            {
                TariffName = NormalizeReportText(g.Key.TariffName),
                PassType = NormalizeReportText(g.Key.PassType),
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var scans = await db.GateScans
            .Where(gs => gs.ScanTime >= from && gs.ScanTime <= to)
            .Select(gs => new
            {
                gs.ScanTime,
                ResultName = gs.VerificationResult != null ? gs.VerificationResult.Name : null,
                LiftName = gs.Gate != null && gs.Gate.Lift != null ? gs.Gate.Lift.Name : "Nieznany wyciag"
            })
            .ToListAsync();

        var scanRows = scans
            .Where(gs => gs.ScanTime.HasValue)
            .ToList();

        var hourlyRides = Enumerable.Range(0, 24)
            .Select(hour =>
            {
                var hourScans = scanRows.Where(gs => gs.ScanTime!.Value.Hour == hour).ToList();
                return new
                {
                    Hour = hour,
                    Accepted = hourScans.Count(gs => gs.ResultName == "ok"),
                    Rejected = hourScans.Count(gs => gs.ResultName != "ok")
                };
            })
            .ToList();

        var liftUsage = scanRows
            .Where(gs => gs.ResultName == "ok")
            .GroupBy(gs => gs.LiftName)
            .Select(g => new
            {
                LiftName = NormalizeReportText(g.Key),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToList();

        static object BuildDayTypeSummary(IEnumerable<SystemStacjiNarciarskiejDLL.Models.Transaction> source)
        {
            var list = source.ToList();
            return new
            {
                NetRevenue = list.Sum(t => t.Amount),
                GrossSales = list.Where(t => t.Amount > 0).Sum(t => t.Amount),
                Returns = Math.Abs(list.Where(t => t.Amount < 0).Sum(t => t.Amount)),
                TransactionCount = list.Count
            };
        }

        static bool IsWeekend(DateTime date) =>
            date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        var result = new
        {
            DateFrom = from,
            DateTo = to,
            GeneratedAt = DateTime.UtcNow,
            Summary = new
            {
                NetRevenue = transactionRows.Sum(t => t.Amount),
                GrossSales = transactionRows.Where(t => t.Amount > 0).Sum(t => t.Amount),
                Returns = Math.Abs(transactionRows.Where(t => t.Amount < 0).Sum(t => t.Amount)),
                TransactionCount = transactionRows.Count,
                AcceptedRides = scanRows.Count(gs => gs.ResultName == "ok"),
                RejectedRides = scanRows.Count(gs => gs.ResultName != "ok"),
                TopTariff = tariffSales.FirstOrDefault()?.TariffName ?? "Brak danych",
                TopLift = liftUsage.FirstOrDefault()?.LiftName ?? "Brak danych"
            },
            DailyRevenue = dailyRevenue,
            TariffSales = tariffSales,
            HourlyRides = hourlyRides,
            LiftUsage = liftUsage,
            DayTypeComparison = new
            {
                Weekdays = BuildDayTypeSummary(transactionRows.Where(t => !IsWeekend(t.TransactionDate!.Value))),
                Weekends = BuildDayTypeSummary(transactionRows.Where(t => IsWeekend(t.TransactionDate!.Value)))
            }
        };

        await LogAdminReportAsync("raport_zaawansowany", $"from={from:yyyy-MM-dd};to={to:yyyy-MM-dd}");
        return Ok(result);
    }

    // 5. Historia raportow wygenerowanych przez administratorow
    [Authorize(Roles = "admin")]
    [HttpGet("historia-admin")]
    public async Task<IActionResult> GetAdminReportHistory([FromQuery] int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 500);

        var reportRows = await db.AdminReports
            .Include(r => r.Admin)
            .Include(r => r.ReportType)
            .OrderByDescending(r => r.GeneratedAt)
            .Take(limit)
            .Select(r => new
            {
                r.Id,
                AdminLogin = r.Admin != null ? r.Admin.Login : "Nieznany",
                ReportTypeName = r.ReportType != null ? r.ReportType.Name : null,
                ReportParameters = r.ReportParameters,
                GeneratedAt = r.GeneratedAt ?? DateTime.MinValue
            })
            .ToListAsync();

        var reports = reportRows.Select(r => new
        {
            r.Id,
            r.AdminLogin,
            ReportType = ResolveReportType(r.ReportParameters, r.ReportTypeName),
            r.ReportParameters,
            r.GeneratedAt
        });

        return Ok(reports);
    }

    // 6. Administracyjne zamykanie zmiany kasjera
    [Authorize(Roles = "admin")]
    [HttpPost("zamknij-kasjera/{cashierId}")]
    public async Task<IActionResult> AdminCloseShift(int cashierId)
    {
        var shift = await db.ShiftReports
            .FirstOrDefaultAsync(r => r.CashierId == cashierId && r.EndTime == null);

        if (shift == null)
            return BadRequest(new { message = "Kasjer nie ma otwartej zmiany." });

        var shiftStart = shift.StartTime ?? DateTime.UtcNow.Date;
        var shiftEnd = DateTime.UtcNow;

        // Podliczanie transakcji
        var transactions = await db.Transactions
            .Where(t => t.CashierId == cashierId && t.TransactionDate >= shiftStart && t.TransactionDate <= shiftEnd)
            .ToListAsync();

        var revenue = transactions.Sum(t => t.Amount);
        var returns = transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));

        shift.EndTime = shiftEnd;
        shift.TotalRevenue = revenue;
        shift.TotalDepositReturns = returns;

        await db.SaveChangesAsync();
        return Ok(new { message = "Zmiana zostala pomyslnie zamknieta przez administratora.", revenue });
    }

    private async Task LogAdminReportAsync(string reportName, string parameters)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var reportType = await db.DictReportTypes.FirstOrDefaultAsync(t => t.Name == reportName)
            ?? await db.DictReportTypes.FirstOrDefaultAsync(t => t.Name == "zarzadczy");

        db.AdminReports.Add(new SystemStacjiNarciarskiejDLL.Models.AdminReport
        {
            AdminId = adminId,
            ReportTypeId = reportType?.Id,
            GeneratedAt = DateTime.UtcNow,
            ReportParameters = $"type={reportName};{parameters}"
        });

        await db.SaveChangesAsync();
    }

    private static string ResolveReportType(string? parameters, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(parameters))
        {
            var typePart = parameters
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(p => p.StartsWith("type=", StringComparison.OrdinalIgnoreCase));
            if (typePart != null)
                return typePart["type=".Length..];
        }

        return fallback ?? "nieznany";
    }

    private static string NormalizeReportText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        return value
            .Replace("\u00C4\u2026", "\u0105")
            .Replace("\u00C4\u2021", "\u0107")
            .Replace("\u00C4\u2122", "\u0119")
            .Replace("\u0139\u201A", "\u0142")
            .Replace("\u0139\u201E", "\u0144")
            .Replace("\u0102\u0142", "\u00F3")
            .Replace("\u0139\u203A", "\u015B")
            .Replace("\u0139\u015F", "\u017A")
            .Replace("\u0139\u017A", "\u017A")
            .Replace("\u0139\u013D", "\u017C")
            .Replace("\u00C4\u201E", "\u0104")
            .Replace("\u00C4\u2020", "\u0106")
            .Replace("\u00C4\u02DC", "\u0118")
            .Replace("\u0139\u0081", "\u0141")
            .Replace("\u0139\u0192", "\u0143")
            .Replace("\u0102\u201C", "\u00D3")
            .Replace("\u0139\u0161", "\u015A")
            .Replace("\u0139\u0105", "\u0179")
            .Replace("\u0139\u00BB", "\u017B");
    }

    private static string NormalizePolishText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        return value
            .Replace("Ä…", "ą")
            .Replace("Ä‡", "ć")
            .Replace("Ä™", "ę")
            .Replace("Ĺ‚", "ł")
            .Replace("Ĺ„", "ń")
            .Replace("Ăł", "ó")
            .Replace("Ĺ›", "ś")
            .Replace("Ĺş", "ź")
            .Replace("ĹĽ", "ż")
            .Replace("Ä„", "Ą")
            .Replace("Ä†", "Ć")
            .Replace("Ä", "Ę")
            .Replace("Ĺ", "Ł")
            .Replace("Ĺƒ", "Ń")
            .Replace("Ă“", "Ó")
            .Replace("Ĺš", "Ś")
            .Replace("Ĺą", "Ź")
            .Replace("Ĺ»", "Ż");
    }
}


public record GateScanDto(
    int Id,
    string? CardId,
    string? GateName,
    string? LiftName,
    DateTime? ScanTime,
    string? Result
);
