using Microsoft.AspNetCore.Identity;

namespace APIGateway.Auth;

public class Role : IdentityRole<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
