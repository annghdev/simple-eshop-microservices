using Contracts.Common;
using Microsoft.AspNetCore.Identity;
using Users.Domain;

namespace APIGateway.Auth;

public class AuthDataSeeder(
    RoleManager<Role> roleManager,
    UserManager<Account> userManager,
    AuthDbContext dbContext,
    ILogger<AuthDataSeeder> logger)
{
    public async Task SeedAsync()
    {
        var roleDisplayNames = new Dictionary<string, string>
        {
            [AuthRoles.Customer] = "Customer",
            [AuthRoles.WarehouseManager] = "Warehouse Manager",
            [AuthRoles.OrderManager] = "Order Manager",
            [AuthRoles.PaymentManager] = "Payment Manager",
            [AuthRoles.ShippingManager] = "Shipping Manager",
            [AuthRoles.Admin] = "Administrator"
        };

        foreach (var roleName in RolePermissionMap.Map.Keys)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                await roleManager.CreateAsync(new Role
                {
                    Name = roleName,
                    DisplayName = roleDisplayNames.GetValueOrDefault(roleName, roleName)
                });
            }
            else if (string.IsNullOrWhiteSpace(role.DisplayName))
            {
                role.DisplayName = roleDisplayNames.GetValueOrDefault(roleName, roleName);
                await roleManager.UpdateAsync(role);
            }
        }

        foreach (var (roleName, permissions) in RolePermissionMap.Map)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var existingClaims = await roleManager.GetClaimsAsync(role);
            foreach (var permission in permissions)
            {
                if (existingClaims.Any(x => x.Type == AuthClaimTypes.Permission && x.Value == permission))
                {
                    continue;
                }

                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim(AuthClaimTypes.Permission, permission));
            }
        }

        if (await userManager.FindByEmailAsync("admin@eshop.local") is not null)
        {
            return;
        }

        var admin = new Account
        {
            Id = Guid.CreateVersion7(),
            UserName = "admin@eshop.local",
            Email = "admin@eshop.local",
            FullName = "System Admin",
            EmailConfirmed = true
        };

        var adminCreate = await userManager.CreateAsync(admin, "Admin@123456");
        if (adminCreate.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, AuthRoles.Admin);
            logger.LogInformation("Seeded default admin account: admin@eshop.local");
        }

        var customerId = Guid.CreateVersion7();
        var customerAccount = new Account
        {
            Id = Guid.CreateVersion7(),
            UserName = "customer@eshop.local",
            Email = "customer@eshop.local",
            FullName = "Default Customer",
            CustomerId = customerId,
            EmailConfirmed = true
        };

        var customerCreate = await userManager.CreateAsync(customerAccount, "Customer@123");
        if (customerCreate.Succeeded)
        {
            await userManager.AddToRoleAsync(customerAccount, AuthRoles.Customer);
            dbContext.Customers.Add(new Customer
            {
                Id = customerId,
                FullName = customerAccount.FullName,
                Email = customerAccount.Email,
                PhoneNumber = "0000000000",
                Address = new Address
                {
                    Street = "N/A",
                    Ward = "N/A",
                    District = "N/A",
                    Province = "N/A"
                },
                Loyalty = 0
            });
            await dbContext.SaveChangesAsync();
        }
    }
}
