using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Payment.Persistence;

public class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=paymentdb;Username=postgres;Password=postgres";
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentDbContext>();
        optionsBuilder.UseNpgsql(DesignTimeConnectionString);

        return new PaymentDbContext(optionsBuilder.Options);
    }
}
