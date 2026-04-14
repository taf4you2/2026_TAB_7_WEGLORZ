using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemStacjiNarciarskiejDLL;

public class SkiResortDbContext : DbContext
{
    public SkiResortDbContext() { }
    
    public SkiResortDbContext(DbContextOptions<SkiResortDbContext> options) : base(options) { }

    public DbSet<DictCardStatus> DictCardStatuses { get; set; } = null!;
    public DbSet<DictPassStatus> DictPassStatuses { get; set; } = null!;
    public DbSet<DictPassType> DictPassTypes { get; set; } = null!;
    public DbSet<DictOperationType> DictOperationTypes { get; set; } = null!;
    public DbSet<DictSeason> DictSeasons { get; set; } = null!;
    public DbSet<DictTrailDifficulty> DictTrailDifficulties { get; set; } = null!;
    public DbSet<DictReservationStatus> DictReservationStatuses { get; set; } = null!;
    public DbSet<DictVerificationResult> DictVerificationResults { get; set; } = null!;
    public DbSet<DictReportType> DictReportTypes { get; set; } = null!;

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Cashier> Cashiers { get; set; } = null!;
    public DbSet<Administrator> Administrators { get; set; } = null!;
    public DbSet<TrailPlanner> TrailPlanners { get; set; } = null!;

    public DbSet<Lift> Lifts { get; set; } = null!;
    public DbSet<Trail> Trails { get; set; } = null!;
    public DbSet<LiftTrail> LiftTrails { get; set; } = null!;
    public DbSet<Gate> Gates { get; set; } = null!;
    public DbSet<LiftSchedule> LiftSchedules { get; set; } = null!;
    public DbSet<TrailSchedule> TrailSchedules { get; set; } = null!;

    public DbSet<Card> Cards { get; set; } = null!;
    public DbSet<Tariff> Tariffs { get; set; } = null!;
    public DbSet<SkiPass> SkiPasses { get; set; } = null!;
    public DbSet<Reservation> Reservations { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<GateScan> GateScans { get; set; } = null!;

    public DbSet<ShiftReport> ShiftReports { get; set; } = null!;
    public DbSet<AdminReport> AdminReports { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LiftTrail>()
            .HasKey(lt => new { lt.LiftId, lt.TrailId });

        modelBuilder.Entity<LiftTrail>()
            .HasOne(lt => lt.Lift)
            .WithMany(l => l.LiftTrails)
            .HasForeignKey(lt => lt.LiftId);

        modelBuilder.Entity<LiftTrail>()
            .HasOne(lt => lt.Trail)
            .WithMany(t => t.LiftTrails)
            .HasForeignKey(lt => lt.TrailId);
    }
}
