using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

namespace SystemAPI.Services;

public static class SalesDatePolicy
{
    public static async Task<SalesDateWindow> GetMinimumSaleDateAsync(SkiResortDbContext db)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var dayOfWeek = (int)now.DayOfWeek;

        var lastClosingTime = await db.LiftSchedules
            .Where(s =>
                s.DayOfWeek == dayOfWeek &&
                s.ClosingTime != null &&
                s.Lift != null &&
                (s.Lift.IsActive == true || s.Lift.IsActive == null))
            .OrderByDescending(s => s.ClosingTime)
            .Select(s => s.ClosingTime)
            .FirstOrDefaultAsync();

        var isAfterLastLiftClosing = lastClosingTime.HasValue && now.TimeOfDay > lastClosingTime.Value;
        return new SalesDateWindow(
            isAfterLastLiftClosing ? today.AddDays(1) : today,
            lastClosingTime,
            isAfterLastLiftClosing);
    }

    public static string CreateTooEarlyMessage(string productName, SalesDateWindow window)
    {
        if (window.IsAfterLastLiftClosing && window.LastLiftClosingTime.HasValue)
        {
            return $"Ostatni wyciag jest juz zamkniety od {window.LastLiftClosingTime.Value:hh\\:mm}. {productName} mozna kupic najwczesniej na {window.MinimumDate:yyyy-MM-dd}.";
        }

        return $"Nie mozna kupic {productName} z data w przeszlosci.";
    }
}

public record SalesDateWindow(
    DateTime MinimumDate,
    TimeSpan? LastLiftClosingTime,
    bool IsAfterLastLiftClosing);
