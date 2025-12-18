using FalconTouch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace FalconTouch.Infrastructure.Data;

public class FalconTouchDbContext : DbContext
{
    public FalconTouchDbContext(DbContextOptions<FalconTouchDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameClick> GameClicks => Set<GameClick>();
    public DbSet<Payment> Payments => Set<Payment>();

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

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.User)
            .WithMany(u => u.Payments)
            .HasForeignKey(p => p.UserId);
    }
}
