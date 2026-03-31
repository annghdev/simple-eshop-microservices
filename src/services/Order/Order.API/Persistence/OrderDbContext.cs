using Microsoft.EntityFrameworkCore;
using Order.Domain;

namespace Order;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<ItemReservation> ItemReservations { get; set; }
    public DbSet<OrderLog> OrderLogs { get; set; }
    public DbSet<FreeItem> FreeItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Order>()
            .HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Domain.Order>()
            .HasMany(o=>o.Logs)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.FreeItem)
            .WithOne()
            .HasForeignKey<FreeItem>(f => f.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasMany(i => i.Reservations)
            .WithOne()
            .HasForeignKey(i => i.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
