
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Route
{
    public int RouteId { get; set; }

    [Required]
    [StringLength(150)]
    public string RouteName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "1000")]
    public decimal DistanceKm { get; set; }

    [Required]
    [StringLength(200)]
    public string StartLocation { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FinishLocation { get; set; } = string.Empty;

    [Range(0, 100000)]
    public int ElevationMetres { get; set; }

    // Relationships
    public ICollection<Event> Events { get; set; }
        = new List<Event>();
}
