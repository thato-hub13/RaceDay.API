
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/enrolments")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Create(CreateEnrolmentDto dto)
    {
        var participant = await _context.Participants
            .SingleOrDefaultAsync(p => p.UserId == CurrentUserId);

        if (participant == null)
            return NotFound(new { message = "Participant profile not found." });

        var category = await _context.Categories
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c =>
                c.CategoryId == dto.CategoryId &&
                c.EventId == dto.EventId);

        if (category == null)
            return BadRequest(new
            {
                message = "The selected category does not belong to this event."
            });

        if (category.Event.EventDate <= DateTime.UtcNow)
            return Conflict(new { message = "This event is no longer open for entry." });

        if (await _context.Enrolments.AnyAsync(e =>
            e.ParticipantId == participant.ParticipantId &&
            e.EventId == dto.EventId))
        {
            return Conflict(new
            {
                message = "You are already registered for this event."
            });
        }

        var activeEntries = await _context.Enrolments.CountAsync(e =>
            e.CategoryId == dto.CategoryId &&
            e.Status == "Confirmed");

        if (activeEntries >= category.MaximumParticipants)
            return Conflict(new { message = "This category is full." });

        var enrolment = new Enrolment
        {
            ParticipantId = participant.ParticipantId,
            EventId = dto.EventId,
            CategoryId = dto.CategoryId,
            EnrolmentDate = DateTime.UtcNow,
            Status = "Confirmed"
        };

        _context.Enrolments.Add(enrolment);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Also catches a concurrent duplicate registration.
            return Conflict(new
            {
                message = "The enrolment could not be created. Check whether you already entered this event."
            });
        }

        return CreatedAtAction(nameof(GetById),
            new { id = enrolment.EnrolmentId }, enrolment);
    }

    [HttpGet("me")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> GetMine()
    {
        var entries = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.Participant.UserId == CurrentUserId)
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Result)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.Status,
                e.EnrolmentDate,
                Event = e.Event.Title,
                e.Event.EventDate,
                Category = e.Category.CategoryName,
                e.Category.DistanceKm,
                Result = e.Result == null ? null : new
                {
                    e.Result.FinishPosition,
                    e.Result.FinishTimeSeconds,
                    e.Result.CompletionStatus
                }
            })
            .ToListAsync();

        return Ok(entries);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Participant,Organiser")]
    public async Task<IActionResult> GetById(int id)
    {
        var entry = await _context.Enrolments
            .AsNoTracking()
            .Include(e => e.Participant)
            .Include(e => e.Event)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (entry == null)
            return NotFound();

        if (User.IsInRole("Participant"))
        {
            if (entry.Participant.UserId != CurrentUserId)
                return Forbid();
        }
        else if (!await _context.Events.AnyAsync(e =>
                     e.EventId == entry.EventId &&
                     e.Organiser.UserId == CurrentUserId))
        {
            return Forbid();
        }

        return Ok(entry);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Cancel(int id)
    {
        var entry = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (entry == null)
            return NotFound();

        if (entry.Participant.UserId != CurrentUserId)
            return Forbid();

        if (entry.Event.EventDate <= DateTime.UtcNow)
            return Conflict(new
            {
                message = "Entries for events that have started cannot be cancelled."
            });

        if (entry.Status != "Confirmed")
            return Conflict(new
            {
                message = "Only confirmed enrolments can be cancelled."
            });

        // Keep the enrolment record for audit/history purposes.
        entry.Status = "Cancelled";
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
