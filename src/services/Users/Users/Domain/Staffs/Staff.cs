using System.ComponentModel.DataAnnotations;

namespace Users.Domain;

public class Staff
{
    [Key]
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    //public string FirstName { get; set; } = string.Empty;
    //public string LastName { get; set; } = string.Empty;
    //public string Gender { get; set; } = string.Empty;
    //public string Nationality { get; set; } = string.Empty;
    //public string Ethnicity { get; set; } = string.Empty;
    //public string Religion { get; set; } = string.Empty;
    //public string CitizenId { get; set; } = string.Empty;
    //public DateOnly DateOfBirth { get; set; }

    //public string PhoneNumber { get; set; } = string.Empty;
    //public string Email { get; set; } = string.Empty;
    //public string Address { get; set; } = string.Empty;
    //public string ProfilePicture { get; set; } = string.Empty;
}
