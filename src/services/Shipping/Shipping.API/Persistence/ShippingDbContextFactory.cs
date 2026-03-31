using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shipping.Persistence;

public class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=shippingdb;Username=postgres;Password=postgres";
    public ShippingDbContext CreateDbContext(string[] args)
    {
        {
            var optionsBuilder = new DbContextOptionsBuilder<ShippingDbContext>();
            optionsBuilder.UseNpgsql(DesignTimeConnectionString);

            return new ShippingDbContext(optionsBuilder.Options);
        }
    }
}
