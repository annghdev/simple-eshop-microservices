using Microsoft.AspNetCore.Identity;

namespace APIGateway.Auth;

public class Account : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Guid? StaffId { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
