using Microsoft.EntityFrameworkCore;
using Payment.Domain;

namespace Payment.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentTransaction> Transactions { get; set; }
}
