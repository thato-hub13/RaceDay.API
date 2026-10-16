
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task<bool> OwnsEvent(int eventId)
    {
        return _context.Events.AnyAsync(e =>
            e.EventId == eventId &&
            e.Organiser.UserId == CurrentUserId);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = _context.Events
            .AsNoTracking()
            .Include(e => e.Route)
            .Include(e => e.Categories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.Title.Contains(search) ||
                e.Location.Contains(search));
        }

        if (from.HasValue)
            query = query.Where(e => e.EventDate >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.EventDate <= to.Value);

        var events = await query
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.Events
            .AsNoTracking()
            .Include(e => e.Route)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(CreateEventDto dto)
    {
        if (!await _context.Routes.AnyAsync(r => r.RouteId == dto.RouteId))
            return BadRequest(new { message = "Invalid route." });

        var organiser = await _context.Organisers
            .SingleOrDefaultAsync(o => o.UserId == CurrentUserId);

        if (organiser == null)
            return Forbid();

        var item = new Event
        {
            Title = dto.Title.Trim(),
            EventType = dto.EventType.Trim(),
            EventDate = dto.EventDate,
            Location = dto.Location.Trim(),
            Description = dto.Description,
            RouteId = dto.RouteId,
            OrganiserId = organiser.OrganiserId
        };

        _context.Events.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = item.EventId }, item);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, UpdateEventDto dto)
    {
        var item = await _context.Events.FindAsync(id);

        if (item == null)
            return NotFound();

        if (!await OwnsEvent(id))
            return Forbid();

        if (!await _context.Routes.AnyAsync(r => r.RouteId == dto.RouteId))
            return BadRequest(new { message = "Invalid route." });

        item.Title = dto.Title.Trim();
        item.EventType = dto.EventType.Trim();
        item.EventDate = dto.EventDate;
        item.Location = dto.Location.Trim();
        item.Description = dto.Description;
        item.RouteId = dto.RouteId;

        await _context.SaveChangesAsync();

        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Events.FindAsync(id);

        if (item == null)
            return NotFound();

        if (!await OwnsEvent(id))
            return Forbid();

        _context.Events.Remove(item);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The event cannot be deleted because related records exist."
            });
        }

        return NoContent();
    }

    [HttpGet("{id:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetEnrolments(int id)
    {
        if (!await _context.Events.AnyAsync(e => e.EventId == id))
            return NotFound();

        if (!await OwnsEvent(id))
            return Forbid();

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.EventId == id)
            .Include(e => e.Participant)
                .ThenInclude(p => p.UserAccount)
            .Include(e => e.Category)
            .Select(e => new
            {
                e.EnrolmentId,
                e.Status,
                e.EnrolmentDate,
                ParticipantName = e.Participant.UserAccount.FullName,
                ParticipantEmail = e.Participant.UserAccount.EmailAddress,
                Category = e.Category.CategoryName
            })
            .ToListAsync();

        return Ok(enrolments);
    }
}
