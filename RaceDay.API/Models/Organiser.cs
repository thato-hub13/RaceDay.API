
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Organiser
{
    public int OrganiserId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(150)]
    public string OrganisationName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? ContactNumber { get; set; }

    // Relationships
    public UserAccount UserAccount { get; set; } = null!;

    public ICollection<Event> Events { get; set; }
        = new List<Event>();
}
