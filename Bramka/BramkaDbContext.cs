using Microsoft.EntityFrameworkCore;

namespace Bramka
{
    public class BramkaDbContext : DbContext
    {
        public DbSet<KartaLokalna> KartyLokalne { get; set; }
 
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=bramka_lokalna.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KartaLokalna>().HasKey(k => k.Id);
        }
    }
}