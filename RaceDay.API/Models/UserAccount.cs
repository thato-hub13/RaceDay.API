
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class UserAccount
{
    public int UserId { get; set; }

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string EmailAddress { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Organiser|Participant)$")]
    public string Role { get; set; } = "Participant";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Organiser? Organiser { get; set; }

    public Participant? Participant { get; set; }
}
