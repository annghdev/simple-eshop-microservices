using Microsoft.EntityFrameworkCore;
using Shipping.Domain;

namespace Shipping.Persistence;

public class ShippingDbContext : DbContext
{
    public ShippingDbContext(DbContextOptions<ShippingDbContext> options) : base(options)
    {        
    }

    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<ShipmentTracking> Trackings { get; set; }
}
