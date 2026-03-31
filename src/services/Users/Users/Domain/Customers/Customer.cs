using Contracts.Common;
using System.ComponentModel.DataAnnotations;

namespace Users.Domain;

public class Customer
{
    [Key]
    public Guid Id { get; set; }
    public Guid? GuestId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Address Address { get; set; } = default!;
    public string? Email { get; set; }
    public int Loyalty { get; set; }
}
