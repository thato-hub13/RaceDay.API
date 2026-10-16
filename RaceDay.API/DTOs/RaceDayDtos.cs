
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public record RegisterDto(
    [Required, StringLength(150, MinimumLength = 2)]
    string FullName,

    [Required, EmailAddress, StringLength(254)]
    string EmailAddress,

    [Required, StringLength(100, MinimumLength = 8)]
    string Password
);

public record LoginDto(
    [Required, EmailAddress]
    string EmailAddress,

    [Required]
    string Password
);

public record UpdateProfileDto(
    [Required, StringLength(150)]
    string FullName
);

public record CreateRouteDto(
    [Required, StringLength(150)]
    string RouteName,

    [Range(typeof(decimal), "0", "1000")]
    decimal DistanceKm,

    [Required, StringLength(200)]
    string StartLocation,

    [Required, StringLength(200)]
    string FinishLocation,

    [Range(0, 100000)]
    int ElevationMetres
);

public record CreateEventDto(
    [Required, StringLength(150)]
    string Title,

    [Required, StringLength(50)]
    string EventType,

    DateTime EventDate,

    [Required, StringLength(200)]
    string Location,

    [StringLength(2000)]
    string? Description,

    [Range(1, int.MaxValue)]
    int RouteId
);

public record UpdateEventDto(
    [Required, StringLength(150)]
    string Title,

    [Required, StringLength(50)]
    string EventType,

    DateTime EventDate,

    [Required, StringLength(200)]
    string Location,

    [StringLength(2000)]
    string? Description,

    [Range(1, int.MaxValue)]
    int RouteId
);

public record CreateCategoryDto(
    [Required, StringLength(100)]
    string CategoryName,

    [Range(typeof(decimal), "0", "1000")]
    decimal DistanceKm,

    [Range(typeof(decimal), "0", "1000000")]
    decimal EntryFee,

    [Range(1, 1000000)]
    int MaximumParticipants
);

public record UpdateCategoryDto(
    [Required, StringLength(100)]
    string CategoryName,

    [Range(typeof(decimal), "0", "1000")]
    decimal DistanceKm,

    [Range(typeof(decimal), "0", "1000000")]
    decimal EntryFee,

    [Range(1, 1000000)]
    int MaximumParticipants
);

public record CreateEnrolmentDto(
    [Range(1, int.MaxValue)]
    int EventId,

    [Range(1, int.MaxValue)]
    int CategoryId
);

public record CreateResultDto(
    [Range(1, int.MaxValue)]
    int EnrolmentId,

    [Range(1, 1000000)]
    int? FinishPosition,

    [Range(0, 10000000)]
    int? FinishTimeSeconds,

    [Required, StringLength(30)]
    string CompletionStatus
);

public record UpdateResultDto(
    [Range(1, 1000000)]
    int? FinishPosition,

    [Range(0, 10000000)]
    int? FinishTimeSeconds,

    [Required, StringLength(30)]
    string CompletionStatus
);
