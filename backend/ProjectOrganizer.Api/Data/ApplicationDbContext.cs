using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Klijent> Klijenti { get; set; }
    public DbSet<Projekat> Projekti { get; set; }
    public DbSet<Aktivnost> Aktivnosti { get; set; }
    public DbSet<DocumentNumbering> DocumentNumbering { get; set; }
    public DbSet<Dokument> Dokumenti { get; set; }
    public DbSet<Note> Notes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Klijent configuration
        modelBuilder.Entity<Klijent>(entity =>
        {
            entity.ToTable("Klijenti");
            entity.HasIndex(e => e.Naziv);
        });

        // Projekat configuration
        modelBuilder.Entity<Projekat>(entity =>
        {
            entity.ToTable("Projekti");
            entity.HasIndex(e => e.BrojProjekta).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.KlijentId);

            entity.HasOne(p => p.Klijent)
                .WithMany(k => k.Projekti)
                .HasForeignKey(p => p.KlijentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Aktivnost configuration
        modelBuilder.Entity<Aktivnost>(entity =>
        {
            entity.ToTable("Aktivnosti");
            entity.HasIndex(e => e.ProjekatId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(a => a.Projekat)
                .WithMany(p => p.Aktivnosti)
                .HasForeignKey(a => a.ProjekatId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Klijent klijent)
                klijent.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Projekat projekat)
                projekat.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Aktivnost aktivnost)
                aktivnost.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
