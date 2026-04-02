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
    public DbSet<UserSettings> UserSettings { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ProjectPermission> ProjectPermissions { get; set; }
    public DbSet<DevOpsTasksCandidate> DevOpsTasksCandidates { get; set; }
    public DbSet<CodebookEntity> CodebookEntities { get; set; }
    public DbSet<Codebook> Codebooks { get; set; }
    public DbSet<ImplementationModel> ImplementationModels { get; set; }
    public DbSet<ImplementationItem> ImplementationItems { get; set; }
    public DbSet<ProjectImplementationItem> ProjectImplementationItems { get; set; }
    public DbSet<CheckListItem> CheckListItems { get; set; }
    public DbSet<ImplementationItemCheckListItem> ImplementationItemCheckListItems { get; set; }
    public DbSet<ProjectImplementationItemCheckList> ProjectImplementationItemCheckLists { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AiReminder> AiReminders { get; set; }
    public DbSet<DailyTask> DailyTasks { get; set; }
    public DbSet<Mail> Mailovi { get; set; }

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
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.ImplementationModelId);

            entity.HasOne(p => p.Klijent)
                .WithMany(k => k.Projekti)
                .HasForeignKey(p => p.KlijentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.ImplementationModel)
                .WithMany()
                .HasForeignKey(p => p.ImplementationModelId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Aktivnost configuration
        modelBuilder.Entity<Aktivnost>(entity =>
        {
            entity.ToTable("Aktivnosti");
            entity.HasIndex(e => e.ProjekatId);
            entity.HasIndex(e => e.Status);

            // Configure UTC DateTime properties
            entity.Property(e => e.StartUtc)
                .HasConversion(new UtcDateTimeConverter())
                .HasColumnType("datetime2");

            entity.Property(e => e.EndUtc)
                .HasConversion(new UtcDateTimeConverter())
                .HasColumnType("datetime2");

            entity.HasOne(a => a.Projekat)
                .WithMany(p => p.Aktivnosti)
                .HasForeignKey(a => a.ProjekatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CodebookEntity configuration
        modelBuilder.Entity<CodebookEntity>(entity =>
        {
            entity.ToTable("CodebookEntities");
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Codebook configuration
        modelBuilder.Entity<Codebook>(entity =>
        {
            entity.ToTable("Codebooks");
            entity.HasIndex(e => e.EntityTypeId);
            entity.HasIndex(e => new { e.EntityTypeId, e.IsActive });
            entity.HasIndex(e => new { e.EntityTypeId, e.Code }).IsUnique();

            entity.HasOne(c => c.EntityType)
                .WithMany(et => et.Codebooks)
                .HasForeignKey(c => c.EntityTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ImplementationModel configuration
        modelBuilder.Entity<ImplementationModel>(entity =>
        {
            entity.ToTable("ImplementationModels");
            entity.HasIndex(e => e.Aktivan);
        });

        // ImplementationItem configuration
        modelBuilder.Entity<ImplementationItem>(entity =>
        {
            entity.ToTable("ImplementationItems");
            entity.HasIndex(e => e.ImplementationModelId);

            entity.HasOne(i => i.ImplementationModel)
                .WithMany(m => m.Items)
                .HasForeignKey(i => i.ImplementationModelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProjectImplementationItem configuration
        modelBuilder.Entity<ProjectImplementationItem>(entity =>
        {
            entity.ToTable("ProjectImplementationItems");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.ImplementationModelId);
            entity.HasIndex(e => e.ImplementationItemId);

            entity.HasOne(pi => pi.Project)
                .WithMany(p => p.ImplementationItems)
                .HasForeignKey(pi => pi.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pi => pi.ImplementationModel)
                .WithMany()
                .HasForeignKey(pi => pi.ImplementationModelId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pi => pi.ImplementationItem)
                .WithMany()
                .HasForeignKey(pi => pi.ImplementationItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CheckListItem configuration
        modelBuilder.Entity<CheckListItem>(entity =>
        {
            entity.ToTable("CheckListItems");
        });

        // ImplementationItemCheckListItem configuration
        modelBuilder.Entity<ImplementationItemCheckListItem>(entity =>
        {
            entity.ToTable("ImplementationItemCheckListItems");
            entity.HasIndex(e => e.ImplementationItemId);
            entity.HasIndex(e => e.CheckListItemId);

            entity.HasOne(ic => ic.ImplementationItem)
                .WithMany(i => i.CheckListItems)
                .HasForeignKey(ic => ic.ImplementationItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ic => ic.CheckListItem)
                .WithMany()
                .HasForeignKey(ic => ic.CheckListItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProjectImplementationItemCheckList configuration
        modelBuilder.Entity<ProjectImplementationItemCheckList>(entity =>
        {
            entity.ToTable("ProjectImplementationItemCheckLists");
            entity.HasIndex(e => e.ProjectImplementationItemId);
            entity.HasIndex(e => e.CheckListItemId);

            entity.HasOne(pic => pic.ProjectImplementationItem)
                .WithMany(pi => pi.CheckLists)
                .HasForeignKey(pic => pic.ProjectImplementationItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pic => pic.CheckListItem)
                .WithMany()
                .HasForeignKey(pic => pic.CheckListItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => e.ReferenceKey)
                  .HasFilter("[ReferenceKey] IS NOT NULL");

            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Projekat)
                .WithMany()
                .HasForeignKey(n => n.ProjekatId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.Aktivnost)
                .WithMany()
                .HasForeignKey(n => n.AktivnostId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // AiReminder configuration
        modelBuilder.Entity<AiReminder>(entity =>
        {
            entity.ToTable("AiReminders");
            entity.HasIndex(e => new { e.RemindAt, e.Sent });
            entity.HasIndex(e => e.AktivnostId);

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(r => r.Projekat)
                .WithMany()
                .HasForeignKey(r => r.ProjekatId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.Aktivnost)
                .WithMany()
                .HasForeignKey(r => r.AktivnostId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // DailyTask configuration
        modelBuilder.Entity<DailyTask>(entity =>
        {
            entity.ToTable("DailyTasks");
            entity.HasIndex(e => e.KorisnikKreirao);
            entity.HasIndex(e => new { e.KorisnikKreirao, e.Datum });

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.KorisnikKreirao)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Mail configuration
        modelBuilder.Entity<Mail>(entity =>
        {
            entity.ToTable("Mailovi");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.MessageId }).IsUnique();
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
            else if (entry.Entity is UserSettings userSettings)
                userSettings.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
