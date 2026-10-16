
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
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task<bool> OwnsEvent(int eventId) =>
        _context.Events.AnyAsync(e =>
            e.EventId == eventId &&
            e.Organiser.UserId == CurrentUserId);

    [HttpGet("events/{eventId:int}/categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        if (!await _context.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound();

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("categories/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        return category == null ? NotFound() : Ok(category);
    }

    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(
        int eventId, CreateCategoryDto dto)
    {
        if (!await _context.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        if (!await OwnsEvent(eventId))
            return Forbid();

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName.Trim(),
            DistanceKm = dto.DistanceKm,
            EntryFee = dto.EntryFee,
            MaximumParticipants = dto.MaximumParticipants
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = category.CategoryId }, category);
    }

    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(
        int id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return NotFound();

        if (!await OwnsEvent(category.EventId))
            return Forbid();

        category.CategoryName = dto.CategoryName.Trim();
        category.DistanceKm = dto.DistanceKm;
        category.EntryFee = dto.EntryFee;
        category.MaximumParticipants = dto.MaximumParticipants;

        await _context.SaveChangesAsync();

        return Ok(category);
    }

    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return NotFound();

        if (!await OwnsEvent(category.EventId))
            return Forbid();

        _context.Categories.Remove(category);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The category cannot be deleted because enrolments reference it."
            });
        }

        return NoContent();
    }
}
