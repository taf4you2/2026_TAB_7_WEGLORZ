using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace StacjaNarciarskaDB.Models;

public partial class SkiDbContext : DbContext
{
    public SkiDbContext()
    {
    }

    public SkiDbContext(DbContextOptions<SkiDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Administrator> Administrators { get; set; }

    public virtual DbSet<Bramka> Bramkas { get; set; }

    public virtual DbSet<HarmonogramTrasy> HarmonogramTrasies { get; set; }

    public virtual DbSet<HarmonogramWyciagu> HarmonogramWyciagus { get; set; }

    public virtual DbSet<Karnet> Karnets { get; set; }

    public virtual DbSet<Kartum> Karta { get; set; }

    public virtual DbSet<Kasjer> Kasjers { get; set; }

    public virtual DbSet<OdbicieBramka> OdbicieBramkas { get; set; }

    public virtual DbSet<PlanistaTra> PlanistaTras { get; set; }

    public virtual DbSet<RaportAdministratora> RaportAdministratoras { get; set; }

    public virtual DbSet<RaportZmianowy> RaportZmianowies { get; set; }

    public virtual DbSet<Rezerwacja> Rezerwacjas { get; set; }

    public virtual DbSet<RodzajRaportu> RodzajRaportus { get; set; }

    public virtual DbSet<SlownikRodzajKarnetu> SlownikRodzajKarnetus { get; set; }

    public virtual DbSet<SlownikSezon> SlownikSezons { get; set; }

    public virtual DbSet<SlownikStatusKarnetu> SlownikStatusKarnetus { get; set; }

    public virtual DbSet<SlownikStatusKarty> SlownikStatusKarties { get; set; }

    public virtual DbSet<SlownikStatusRezerwacji> SlownikStatusRezerwacjis { get; set; }

    public virtual DbSet<SlownikTrudnoscTrasy> SlownikTrudnoscTrasies { get; set; }

    public virtual DbSet<SlownikTypOperacji> SlownikTypOperacjis { get; set; }

    public virtual DbSet<SlownikWynikWeryfikacji> SlownikWynikWeryfikacjis { get; set; }

    public virtual DbSet<Taryfa> Taryfas { get; set; }

    public virtual DbSet<Transakcja> Transakcjas { get; set; }

    public virtual DbSet<Trasa> Trasas { get; set; }

    public virtual DbSet<Uzytkownik> Uzytkowniks { get; set; }

    public virtual DbSet<Wyciag> Wyciags { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Database=StacjaNarciarska;Username=postgres;Password=admin");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administrator>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("administrator_pkey");

            entity.ToTable("administrator");

            entity.HasIndex(e => e.Login, "administrator_login_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CzyAktywny).HasColumnName("czy_aktywny");
            entity.Property(e => e.HasloHash)
                .HasColumnType("character varying")
                .HasColumnName("haslo_hash");
            entity.Property(e => e.Login)
                .HasColumnType("character varying")
                .HasColumnName("login");
        });

        modelBuilder.Entity<Bramka>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("bramka_pkey");

            entity.ToTable("bramka");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CzyAktywna).HasColumnName("czy_aktywna");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
            entity.Property(e => e.WyciagId).HasColumnName("wyciag_id");

            entity.HasOne(d => d.Wyciag).WithMany(p => p.Bramkas)
                .HasForeignKey(d => d.WyciagId)
                .HasConstraintName("bramka_wyciag_id_fkey");
        });

        modelBuilder.Entity<HarmonogramTrasy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("harmonogram_trasy_pkey");

            entity.ToTable("harmonogram_trasy");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CzyOtwarta).HasColumnName("czy_otwarta");
            entity.Property(e => e.DataZmiany)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("data_zmiany");
            entity.Property(e => e.PowodZamkniecia)
                .HasColumnType("character varying")
                .HasColumnName("powod_zamkniecia");
            entity.Property(e => e.TrasaId).HasColumnName("trasa_id");

            entity.HasOne(d => d.Trasa).WithMany(p => p.HarmonogramTrasies)
                .HasForeignKey(d => d.TrasaId)
                .HasConstraintName("harmonogram_trasy_trasa_id_fkey");
        });

        modelBuilder.Entity<HarmonogramWyciagu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("harmonogram_wyciagu_pkey");

            entity.ToTable("harmonogram_wyciagu");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.DzienTygodnia).HasColumnName("dzien_tygodnia");
            entity.Property(e => e.GodzinaOtwarcia).HasColumnName("godzina_otwarcia");
            entity.Property(e => e.GodzinaZamkniecia).HasColumnName("godzina_zamkniecia");
            entity.Property(e => e.WyciagId).HasColumnName("wyciag_id");

            entity.HasOne(d => d.Wyciag).WithMany(p => p.HarmonogramWyciagus)
                .HasForeignKey(d => d.WyciagId)
                .HasConstraintName("harmonogram_wyciagu_wyciag_id_fkey");
        });

        modelBuilder.Entity<Karnet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("karnet_pkey");

            entity.ToTable("karnet");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.DataWaznosciDo)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("data_waznosci_do");
            entity.Property(e => e.DataWaznosciOd)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("data_waznosci_od");
            entity.Property(e => e.KartaId)
                .HasColumnType("character varying")
                .HasColumnName("karta_id");
            entity.Property(e => e.PowodBlokady).HasColumnName("powod_blokady");
            entity.Property(e => e.RezerwacjaId).HasColumnName("rezerwacja_id");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.TaryfaId).HasColumnName("taryfa_id");

            entity.HasOne(d => d.Karta).WithMany(p => p.Karnets)
                .HasForeignKey(d => d.KartaId)
                .HasConstraintName("karnet_karta_id_fkey");

            entity.HasOne(d => d.Rezerwacja).WithMany(p => p.Karnets)
                .HasForeignKey(d => d.RezerwacjaId)
                .HasConstraintName("karnet_rezerwacja_id_fkey");

            entity.HasOne(d => d.Status).WithMany(p => p.Karnets)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("karnet_status_id_fkey");

            entity.HasOne(d => d.Taryfa).WithMany(p => p.Karnets)
                .HasForeignKey(d => d.TaryfaId)
                .HasConstraintName("karnet_taryfa_id_fkey");
        });

        modelBuilder.Entity<Kartum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("karta_pkey");

            entity.ToTable("karta");

            entity.Property(e => e.Id)
                .HasColumnType("character varying")
                .HasColumnName("id");
            entity.Property(e => e.DataDodaniaDoPuli)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("data_dodania_do_puli");
            entity.Property(e => e.StanFizyczny)
                .HasColumnType("character varying")
                .HasColumnName("stan_fizyczny");
            entity.Property(e => e.StatusId).HasColumnName("status_id");

            entity.HasOne(d => d.Status).WithMany(p => p.Karta)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("karta_status_id_fkey");
        });

        modelBuilder.Entity<Kasjer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("kasjer_pkey");

            entity.ToTable("kasjer");

            entity.HasIndex(e => e.Login, "kasjer_login_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CzyAktywny).HasColumnName("czy_aktywny");
            entity.Property(e => e.HasloHash)
                .HasColumnType("character varying")
                .HasColumnName("haslo_hash");
            entity.Property(e => e.Login)
                .HasColumnType("character varying")
                .HasColumnName("login");
        });

        modelBuilder.Entity<OdbicieBramka>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("odbicie_bramka_pkey");

            entity.ToTable("odbicie_bramka");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.BlokadaCzasowaDo)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("blokada_czasowa_do");
            entity.Property(e => e.BramkaId).HasColumnName("bramka_id");
            entity.Property(e => e.CzasOdbicia)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("czas_odbicia");
            entity.Property(e => e.KartaId)
                .HasColumnType("character varying")
                .HasColumnName("karta_id");
            entity.Property(e => e.WynikWeryfikacjiId).HasColumnName("wynik_weryfikacji_id");

            entity.HasOne(d => d.Bramka).WithMany(p => p.OdbicieBramkas)
                .HasForeignKey(d => d.BramkaId)
                .HasConstraintName("odbicie_bramka_bramka_id_fkey");

            entity.HasOne(d => d.Karta).WithMany(p => p.OdbicieBramkas)
                .HasForeignKey(d => d.KartaId)
                .HasConstraintName("odbicie_bramka_karta_id_fkey");

            entity.HasOne(d => d.WynikWeryfikacji).WithMany(p => p.OdbicieBramkas)
                .HasForeignKey(d => d.WynikWeryfikacjiId)
                .HasConstraintName("odbicie_bramka_wynik_weryfikacji_id_fkey");
        });

        modelBuilder.Entity<PlanistaTra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("planista_tras_pkey");

            entity.ToTable("planista_tras");

            entity.HasIndex(e => e.Login, "planista_tras_login_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CzyAktywny).HasColumnName("czy_aktywny");
            entity.Property(e => e.HasloHash)
                .HasColumnType("character varying")
                .HasColumnName("haslo_hash");
            entity.Property(e => e.Login)
                .HasColumnType("character varying")
                .HasColumnName("login");
        });

        modelBuilder.Entity<RaportAdministratora>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("raport_administratora_pkey");

            entity.ToTable("raport_administratora");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.AdministratorId).HasColumnName("administrator_id");
            entity.Property(e => e.DataWygenerowania)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("data_wygenerowania");
            entity.Property(e => e.ParametryRaportu).HasColumnName("parametry_raportu");
            entity.Property(e => e.RodzajRaportuId).HasColumnName("rodzaj_raportu_id");

            entity.HasOne(d => d.Administrator).WithMany(p => p.RaportAdministratoras)
                .HasForeignKey(d => d.AdministratorId)
                .HasConstraintName("raport_administratora_administrator_id_fkey");

            entity.HasOne(d => d.RodzajRaportu).WithMany(p => p.RaportAdministratoras)
                .HasForeignKey(d => d.RodzajRaportuId)
                .HasConstraintName("raport_administratora_rodzaj_raportu_id_fkey");
        });

        modelBuilder.Entity<RaportZmianowy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("raport_zmianowy_pkey");

            entity.ToTable("raport_zmianowy");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.DataRozpoczecia)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("data_rozpoczecia");
            entity.Property(e => e.DataZakonczenia)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("data_zakonczenia");
            entity.Property(e => e.KasjerId).HasColumnName("kasjer_id");
            entity.Property(e => e.LiczbaWydanychKart).HasColumnName("liczba_wydanych_kart");
            entity.Property(e => e.SumaPrzychody).HasColumnName("suma_przychody");
            entity.Property(e => e.SumaZwrotyKaucji).HasColumnName("suma_zwroty_kaucji");

            entity.HasOne(d => d.Kasjer).WithMany(p => p.RaportZmianowies)
                .HasForeignKey(d => d.KasjerId)
                .HasConstraintName("raport_zmianowy_kasjer_id_fkey");
        });

        modelBuilder.Entity<Rezerwacja>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rezerwacja_pkey");

            entity.ToTable("rezerwacja");

            entity.HasIndex(e => e.NumerRezerwacji, "rezerwacja_numer_rezerwacji_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.DataRezerwacji)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("data_rezerwacji");
            entity.Property(e => e.NumerRezerwacji)
                .HasColumnType("character varying")
                .HasColumnName("numer_rezerwacji");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.UzytkownikId).HasColumnName("uzytkownik_id");

            entity.HasOne(d => d.Status).WithMany(p => p.Rezerwacjas)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("rezerwacja_status_id_fkey");

            entity.HasOne(d => d.Uzytkownik).WithMany(p => p.Rezerwacjas)
                .HasForeignKey(d => d.UzytkownikId)
                .HasConstraintName("rezerwacja_uzytkownik_id_fkey");
        });

        modelBuilder.Entity<RodzajRaportu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rodzaj_raportu_pkey");

            entity.ToTable("rodzaj_raportu");

            entity.HasIndex(e => e.Nazwa, "rodzaj_raportu_nazwa_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
            entity.Property(e => e.Opis).HasColumnName("opis");
        });

        modelBuilder.Entity<SlownikRodzajKarnetu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("slownik_rodzaj_karnetu_pkey");

            entity.ToTable("slownik_rodzaj_karnetu");

            entity.HasIndex(e => e.Nazwa, "slownik_rodzaj_karnetu_nazwa_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
        });

        modelBuilder.Entity<SlownikSezon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("slownik_sezon_pkey");

            entity.ToTable("slownik_sezon");

            entity.HasIndex(e => e.Nazwa, "slownik_sezon_nazwa_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
        });

        modelBuilder.Entity<SlownikStatusKarnetu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("slownik_status_karnetu_pkey");

            entity.ToTable("slownik_status_karnetu");

            entity.HasIndex(e => e.Nazwa, "slownik_status_karnetu_nazwa_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
        });

        modelBuilder.Entity<SlownikStatusKarty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("slownik_status_karty_pkey");

            entity.ToTable("slownik_status_karty");

            entity.HasIndex(e => e.Nazwa, "slownik_status_karty_nazwa_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
        });

        modelBuilder.Entity<SlownikStatusRezerwacji>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("slownik_status_rezerwacji_pkey");

            entity.ToTable("slownik_status_rezerwacji");

            entity.HasIndex(e => e.Nazwa, "slownik_status_rezerwacji_nazwa_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
        });

        modelBuilder.Entity<SlownikTrudnoscTrasy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("slownik_trudnosc_trasy_pkey");

            entity.ToTable("slownik_trudnosc_trasy");

            entity.HasIndex(e => e.Nazwa, "slownik_trudnosc_trasy_nazwa_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
        });

        modelBuilder.Entity<SlownikTypOperacji>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("slownik_typ_operacji_pkey");

            entity.ToTable("slownik_typ_operacji");

            entity.HasIndex(e => e.Nazwa, "slownik_typ_operacji_nazwa_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
        });

        modelBuilder.Entity<SlownikWynikWeryfikacji>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("slownik_wynik_weryfikacji_pkey");

            entity.ToTable("slownik_wynik_weryfikacji");

            entity.HasIndex(e => e.Nazwa, "slownik_wynik_weryfikacji_nazwa_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
        });

        modelBuilder.Entity<Taryfa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("taryfa_pkey");

            entity.ToTable("taryfa");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Cena).HasColumnName("cena");
            entity.Property(e => e.LimitPuli).HasColumnName("limit_puli");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
            entity.Property(e => e.RodzajKarnetuId).HasColumnName("rodzaj_karnetu_id");
            entity.Property(e => e.SezonId).HasColumnName("sezon_id");

            entity.HasOne(d => d.RodzajKarnetu).WithMany(p => p.Taryfas)
                .HasForeignKey(d => d.RodzajKarnetuId)
                .HasConstraintName("taryfa_rodzaj_karnetu_id_fkey");

            entity.HasOne(d => d.Sezon).WithMany(p => p.Taryfas)
                .HasForeignKey(d => d.SezonId)
                .HasConstraintName("taryfa_sezon_id_fkey");
        });

        modelBuilder.Entity<Transakcja>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transakcja_pkey");

            entity.ToTable("transakcja");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.DataTransakcji)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("data_transakcji");
            entity.Property(e => e.KasjerId).HasColumnName("kasjer_id");
            entity.Property(e => e.Kwota).HasColumnName("kwota");
            entity.Property(e => e.RezerwacjaId).HasColumnName("rezerwacja_id");
            entity.Property(e => e.TypOperacjiId).HasColumnName("typ_operacji_id");

            entity.HasOne(d => d.Kasjer).WithMany(p => p.Transakcjas)
                .HasForeignKey(d => d.KasjerId)
                .HasConstraintName("transakcja_kasjer_id_fkey");

            entity.HasOne(d => d.Rezerwacja).WithMany(p => p.Transakcjas)
                .HasForeignKey(d => d.RezerwacjaId)
                .HasConstraintName("transakcja_rezerwacja_id_fkey");

            entity.HasOne(d => d.TypOperacji).WithMany(p => p.Transakcjas)
                .HasForeignKey(d => d.TypOperacjiId)
                .HasConstraintName("transakcja_typ_operacji_id_fkey");
        });

        modelBuilder.Entity<Trasa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trasa_pkey");

            entity.ToTable("trasa");

            entity.HasIndex(e => e.Nazwa, "trasa_nazwa_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Dlugosc).HasColumnName("dlugosc");
            entity.Property(e => e.Lokalizacja)
                .HasColumnType("character varying")
                .HasColumnName("lokalizacja");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
            entity.Property(e => e.PlanistaId).HasColumnName("planista_id");
            entity.Property(e => e.TrudnoscId).HasColumnName("trudnosc_id");

            entity.HasOne(d => d.Planista).WithMany(p => p.Trasas)
                .HasForeignKey(d => d.PlanistaId)
                .HasConstraintName("trasa_planista_id_fkey");

            entity.HasOne(d => d.Trudnosc).WithMany(p => p.Trasas)
                .HasForeignKey(d => d.TrudnoscId)
                .HasConstraintName("trasa_trudnosc_id_fkey");
        });

        modelBuilder.Entity<Uzytkownik>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("uzytkownik_pkey");

            entity.ToTable("uzytkownik");

            entity.HasIndex(e => e.Email, "uzytkownik_email_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.DataUtworzenia)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("data_utworzenia");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.HasloHash)
                .HasColumnType("character varying")
                .HasColumnName("haslo_hash");
        });

        modelBuilder.Entity<Wyciag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("wyciag_pkey");

            entity.ToTable("wyciag");

            entity.HasIndex(e => e.Nazwa, "wyciag_nazwa_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Dlugosc).HasColumnName("dlugosc");
            entity.Property(e => e.Lokalizacja)
                .HasColumnType("character varying")
                .HasColumnName("lokalizacja");
            entity.Property(e => e.Nazwa)
                .HasColumnType("character varying")
                .HasColumnName("nazwa");
            entity.Property(e => e.PlanistaId).HasColumnName("planista_id");

            entity.HasOne(d => d.Planista).WithMany(p => p.Wyciags)
                .HasForeignKey(d => d.PlanistaId)
                .HasConstraintName("wyciag_planista_id_fkey");

            entity.HasMany(d => d.Trasas).WithMany(p => p.Wyciags)
                .UsingEntity<Dictionary<string, object>>(
                    "WyciagTrasa",
                    r => r.HasOne<Trasa>().WithMany()
                        .HasForeignKey("TrasaId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("wyciag_trasa_trasa_id_fkey"),
                    l => l.HasOne<Wyciag>().WithMany()
                        .HasForeignKey("WyciagId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("wyciag_trasa_wyciag_id_fkey"),
                    j =>
                    {
                        j.HasKey("WyciagId", "TrasaId").HasName("wyciag_trasa_pkey");
                        j.ToTable("wyciag_trasa");
                        j.IndexerProperty<int>("WyciagId").HasColumnName("wyciag_id");
                        j.IndexerProperty<int>("TrasaId").HasColumnName("trasa_id");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
