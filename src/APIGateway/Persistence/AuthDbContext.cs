using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Users.Domain;

namespace APIGateway.Auth;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : IdentityDbContext<Account, Role, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Staff> Staffs => Set<Staff>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.ExpiresAtUtc).IsRequired();
            entity.Property(x => x.RevokedAtUtc);
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(256);
            entity.HasOne(x => x.Account)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.AccountId);
        });

        builder.Entity<Role>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        });

        builder.Entity<Customer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.OwnsOne(x => x.Address);
            entity.HasIndex(x => x.Email);
        });

        builder.Entity<Staff>(entity =>
        {
            entity.HasKey(x => x.Id);
        });
    }
}
