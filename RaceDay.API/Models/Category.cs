
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Category
{
    public int CategoryId { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "1000")]
    public decimal DistanceKm { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000)]
    public int MaximumParticipants { get; set; }

    // Relationships
    public Event Event { get; set; } = null!;

    public ICollection<Enrolment> Enrolments { get; set; }
        = new List<Enrolment>();
}
