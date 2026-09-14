using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Models;

namespace SekProduction.Web.Data;

public class ApplicationDbContext : IdentityDbContext
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
    public DbSet<NewsPost> NewsPosts => Set<NewsPost>();
    public DbSet<ClientReference> ClientReferences => Set<ClientReference>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Production>()
            .HasIndex(p => p.Slug)
            .IsUnique();

        builder.Entity<NewsPost>()
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

        builder.Entity<ProductionPhoto>()
            .HasOne(ph => ph.Production)
            .WithMany(p => p.Photos)
            .HasForeignKey(ph => ph.ProductionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
