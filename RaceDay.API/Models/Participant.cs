
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Participant
{
    public int ParticipantId { get; set; }

    [Required]
    public int UserId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(150)]
    public string? EmergencyContact { get; set; }

    // Relationships
    public UserAccount UserAccount { get; set; } = null!;

    public ICollection<Enrolment> Enrolments { get; set; }
        = new List<Enrolment>();
}
