
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }

    [Required]
    public int ParticipantId { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Confirmed";

    // Relationships
    public Participant Participant { get; set; } = null!;

    public Event Event { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public Result? Result { get; set; }
}
