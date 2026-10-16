
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Event
{
    public int EventId { get; set; }

    [Required]
    public int OrganiserId { get; set; }

    [Required]
    public int RouteId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    // Relationships
    public Organiser Organiser { get; set; } = null!;

    public Route Route { get; set; } = null!;

    public ICollection<Category> Categories { get; set; }
        = new List<Category>();

    public ICollection<Enrolment> Enrolments { get; set; }
        = new List<Enrolment>();
}
