
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Result
{
    public int ResultId { get; set; }

    [Required]
    public int EnrolmentId { get; set; }

    [Range(1, 1000000)]
    public int? FinishPosition { get; set; }

    [Range(0, 10000000)]
    public int? FinishTimeSeconds { get; set; }

    [Required]
    [StringLength(30)]
    public string CompletionStatus { get; set; } = "Finished";

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Relationship
    public Enrolment Enrolment { get; set; } = null!;
}
