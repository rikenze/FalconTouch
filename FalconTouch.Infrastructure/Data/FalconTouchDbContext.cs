using FalconTouch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FalconTouch.Infrastructure.Data;

public class FalconTouchDbContext : DbContext
{
    public FalconTouchDbContext(DbContextOptions<FalconTouchDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameClick> GameClicks => Set<GameClick>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Prize> Prizes => Set<Prize>();
    public DbSet<PrizeImage> PrizeImages => Set<PrizeImage>();
    public DbSet<DeliveryInfo> DeliveryInfos => Set<DeliveryInfo>();
    public DbSet<Influencer> Influencers => Set<Influencer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(120);

            e.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(180);

            e.Property(u => u.CPF)
                .IsRequired()
                .HasMaxLength(11);

            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.CPF).IsUnique();
        });

        modelBuilder.Entity<GameClick>()
            .HasOne(gc => gc.Game)
            .WithMany(g => g.Clicks)
            .HasForeignKey(gc => gc.GameId);

        modelBuilder.Entity<GameClick>()
            .HasOne(gc => gc.User)
            .WithMany()
            .HasForeignKey(gc => gc.UserId);

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId);

            e.HasOne(p => p.Game)
                .WithMany()
                .HasForeignKey(p => p.GameId);

            e.HasOne(p => p.Influencer)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InfluencerId);
        });

        modelBuilder.Entity<Game>(e =>
        {
            e.Property(g => g.Name)
                .HasMaxLength(120);

            e.Property(g => g.Price)
                .HasPrecision(10, 2);
        });

        modelBuilder.Entity<Prize>(e =>
        {
            e.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(200);

            e.HasOne(p => p.Game)
                .WithOne(g => g.Prize)
                .HasForeignKey<Prize>(p => p.GameId);
        });

        modelBuilder.Entity<PrizeImage>(e =>
        {
            e.Property(pi => pi.Image)
                .IsRequired();

            e.HasOne(pi => pi.Prize)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.PrizeId);
        });

        modelBuilder.Entity<DeliveryInfo>(e =>
        {
            e.Property(d => d.Street).HasMaxLength(180);
            e.Property(d => d.Number).HasMaxLength(20);
            e.Property(d => d.Neighborhood).HasMaxLength(120);
            e.Property(d => d.City).HasMaxLength(120);
            e.Property(d => d.State).HasMaxLength(2);
            e.Property(d => d.ZipCode).HasMaxLength(12);

            e.HasOne(d => d.User)
                .WithMany(u => u.DeliveryInfos)
                .HasForeignKey(d => d.UserId);

            e.HasOne(d => d.Game)
                .WithMany()
                .HasForeignKey(d => d.GameId);
        });

        modelBuilder.Entity<Influencer>(e =>
        {
            e.Property(i => i.Name).HasMaxLength(120);
            e.Property(i => i.Code).HasMaxLength(50);
            e.Property(i => i.CommissionType).HasMaxLength(30);
            e.Property(i => i.CommissionValue).HasPrecision(10, 2);
            e.Property(i => i.MinimumFollowerPercentage).HasPrecision(5, 2);
            e.Property(i => i.DiscountPercent).HasPrecision(5, 2);
        });
    }
}
