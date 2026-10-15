using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Models;

namespace SekProduction.Web.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Production> Productions => Set<Production>();
    public DbSet<EventSchedule> EventSchedules => Set<EventSchedule>();
    public DbSet<CastMember> CastMembers => Set<CastMember>();
    public DbSet<ProductionPhoto> ProductionPhotos => Set<ProductionPhoto>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<EventNote> EventNotes => Set<EventNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Production>()
            .HasIndex(p => p.Slug)
            .IsUnique();

        builder.Entity<EventSchedule>()
            .HasOne(e => e.Production)
            .WithMany(p => p.EventSchedules)
            .HasForeignKey(e => e.ProductionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CastMember>()
            .HasOne(c => c.Production)
            .WithMany(p => p.CastMembers)
            .HasForeignKey(c => c.ProductionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EventNote>(entity =>
        {
            entity.HasOne(n => n.EventSchedule)
                .WithMany(e => e.Notes)
                .HasForeignKey(n => n.EventScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Author)
                .WithMany()
                .HasForeignKey(n => n.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(n => new { n.EventScheduleId, n.Scope });
        });

        builder.Entity<Hotel>()
            .HasIndex(h => new { h.City, h.Name });

        builder.Entity<ProductionPhoto>()
            .HasOne(ph => ph.Production)
            .WithMany(p => p.Photos)
            .HasForeignKey(ph => ph.ProductionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
