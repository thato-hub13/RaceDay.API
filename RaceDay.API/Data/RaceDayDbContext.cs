
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Models.Route> Routes => Set<Models.Route>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // USER ACCOUNTS
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(u => u.UserId);

            entity.HasIndex(u => u.EmailAddress)
                .IsUnique();

            entity.Property(u => u.EmailAddress)
                .HasMaxLength(254)
                .IsRequired();

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.Role)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ORGANISERS
        modelBuilder.Entity<Organiser>(entity =>
        {
            entity.HasKey(o => o.OrganiserId);

            entity.HasIndex(o => o.UserId)
                .IsUnique();

            entity.HasOne(o => o.UserAccount)
                .WithOne(u => u.Organiser)
                .HasForeignKey<Organiser>(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PARTICIPANTS
        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasKey(p => p.ParticipantId);

            entity.HasIndex(p => p.UserId)
                .IsUnique();

            entity.HasOne(p => p.UserAccount)
                .WithOne(u => u.Participant)
                .HasForeignKey<Participant>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ROUTES
        modelBuilder.Entity<Models.Route>(entity =>
        {
            entity.HasKey(r => r.RouteId);

            entity.Property(r => r.DistanceKm)
                .HasPrecision(8, 2);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Routes_DistanceKm",
                "[DistanceKm] > 0"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Routes_ElevationMetres",
                "[ElevationMetres] >= 0"));
        });

        // EVENTS
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId);

            entity.HasOne(e => e.Organiser)
                .WithMany(o => o.Events)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Route)
                .WithMany(r => r.Events)
                .HasForeignKey(e => e.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.EventDate);
        });

        // CATEGORIES
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.CategoryId);

            entity.Property(c => c.DistanceKm)
                .HasPrecision(8, 2);

            entity.Property(c => c.EntryFee)
                .HasPrecision(10, 2);

            // Supports the composite FK from Enrolments.
            entity.HasAlternateKey(c => new
            {
                c.EventId,
                c.CategoryId
            });

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Categories_DistanceKm",
                "[DistanceKm] > 0"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Categories_EntryFee",
                "[EntryFee] >= 0"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Categories_MaximumParticipants",
                "[MaximumParticipants] > 0"));

            entity.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ENROLMENTS
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasKey(e => e.EnrolmentId);

            // A participant may enter an event only once.
            entity.HasIndex(e => new
            {
                e.ParticipantId,
                e.EventId
            }).IsUnique();

            entity.HasOne(e => e.Participant)
                .WithMany(p => p.Enrolments)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensures the selected category belongs to this event.
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => new
                {
                    e.EventId,
                    e.CategoryId
                })
                .HasPrincipalKey(c => new
                {
                    c.EventId,
                    c.CategoryId
                })
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RESULTS
        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(r => r.ResultId);

            // At most one result per enrolment.
            entity.HasIndex(r => r.EnrolmentId)
                .IsUnique();

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Results_FinishPosition",
                "[FinishPosition] IS NULL OR [FinishPosition] > 0"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Results_FinishTimeSeconds",
                "[FinishTimeSeconds] IS NULL OR [FinishTimeSeconds] >= 0"));

            entity.HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
