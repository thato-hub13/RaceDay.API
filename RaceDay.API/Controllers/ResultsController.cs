
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("results")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(CreateResultDto dto)
    {
        var entry = await _context.Enrolments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId);

        if (entry == null)
            return NotFound(new { message = "Enrolment not found." });

        if (entry.Event.OrganiserId == 0 ||
            !await _context.Events.AnyAsync(e =>
                e.EventId == entry.EventId &&
                e.Organiser.UserId == CurrentUserId))
        {
            return Forbid();
        }

        if (entry.Status != "Confirmed")
            return Conflict(new
            {
                message = "A result can only be recorded for a confirmed enrolment."
            });

        if (await _context.Results.AnyAsync(r =>
            r.EnrolmentId == dto.EnrolmentId))
        {
            return Conflict(new { message = "A result already exists." });
        }

        if (dto.CompletionStatus == "Finished" &&
            (dto.FinishPosition == null || dto.FinishTimeSeconds == null))
        {
            return BadRequest(new
            {
                message = "A finished participant must have a finishing position and time."
            });
        }

        var result = new Result
        {
            EnrolmentId = dto.EnrolmentId,
            FinishPosition = dto.FinishPosition,
            FinishTimeSeconds = dto.FinishTimeSeconds,
            CompletionStatus = dto.CompletionStatus.Trim(),
            RecordedAt = DateTime.UtcNow
        };

        _context.Results.Add(result);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "A result already exists or the data violates a database constraint."
            });
        }

        return CreatedAtAction(nameof(GetByEnrolment),
            new { id = result.EnrolmentId }, result);
    }

    [HttpPut("results/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, UpdateResultDto dto)
    {
        var result = await _context.Results
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result == null)
            return NotFound();

        if (!await _context.Events.AnyAsync(e =>
            e.EventId == result.Enrolment.EventId &&
            e.Organiser.UserId == CurrentUserId))
        {
            return Forbid();
        }

        if (dto.CompletionStatus == "Finished" &&
            (dto.FinishPosition == null || dto.FinishTimeSeconds == null))
        {
            return BadRequest(new
            {
                message = "A finished participant must have a finishing position and time."
            });
        }

        result.FinishPosition = dto.FinishPosition;
        result.FinishTimeSeconds = dto.FinishTimeSeconds;
        result.CompletionStatus = dto.CompletionStatus.Trim();
        result.RecordedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(result);
    }

    [HttpGet("results/me")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> GetMyResults()
    {
        var results = await _context.Results
            .AsNoTracking()
            .Where(r => r.Enrolment.Participant.UserId == CurrentUserId)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Event)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Category)
            .OrderByDescending(r => r.RecordedAt)
            .Select(r => new
            {
                r.ResultId,
                r.FinishPosition,
                r.FinishTimeSeconds,
                r.CompletionStatus,
                Event = r.Enrolment.Event.Title,
                EventDate = r.Enrolment.Event.EventDate,
                Category = r.Enrolment.Category.CategoryName
            })
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("enrolments/{id:int}/result")]
    [Authorize(Roles = "Participant,Organiser")]
    public async Task<IActionResult> GetByEnrolment(int id)
    {
        var entry = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Event)
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

        var result = await _context.Results
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EnrolmentId == id);

        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("events/{eventId:int}/results")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        if (!await _context.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var results = await _context.Results
            .AsNoTracking()
            .Where(r => r.Enrolment.EventId == eventId)
            .OrderBy(r => r.FinishPosition ?? int.MaxValue)
            .Select(r => new
            {
                r.ResultId,
                r.FinishPosition,
                r.FinishTimeSeconds,
                r.CompletionStatus,
                Participant = r.Enrolment.Participant.UserAccount.FullName,
                Category = r.Enrolment.Category.CategoryName
            })
            .ToListAsync();

        return Ok(results);
    }
}
