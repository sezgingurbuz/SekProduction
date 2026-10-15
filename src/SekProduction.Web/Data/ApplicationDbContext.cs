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
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementRecipient> AnnouncementRecipients => Set<AnnouncementRecipient>();

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

        builder.Entity<Announcement>(entity =>
        {
            entity.HasOne(a => a.Author)
                .WithMany()
                .HasForeignKey(a => a.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Etkinlik silinse de duyuru metni kaybolmaz.
            entity.HasOne(a => a.EventSchedule)
                .WithMany()
                .HasForeignKey(a => a.EventScheduleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(a => a.CreatedAt);
        });

        builder.Entity<AnnouncementRecipient>(entity =>
        {
            entity.HasOne(r => r.Announcement)
                .WithMany(a => a.Recipients)
                .HasForeignKey(r => r.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => new { r.AnnouncementId, r.UserId }).IsUnique();
            entity.HasIndex(r => new { r.UserId, r.ReadAt });
        });

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
