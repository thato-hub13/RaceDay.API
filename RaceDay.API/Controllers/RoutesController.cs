
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/routes")]
public class RoutesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public RoutesController(RaceDayDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _context.Routes
            .AsNoTracking()
            .OrderBy(r => r.RouteName)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var route = await _context.Routes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RouteId == id);

        return route == null ? NotFound() : Ok(route);
    }

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create([FromBody] CreateRouteDto dto)
    {
        var route = new Models.Route
        {
            RouteName = dto.RouteName.Trim(),
            DistanceKm = dto.DistanceKm,
            StartLocation = dto.StartLocation.Trim(),
            FinishLocation = dto.FinishLocation.Trim(),
            ElevationMetres = dto.ElevationMetres
        };

        _context.Routes.Add(route);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = route.RouteId }, route);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, CreateRouteDto dto)
    {
        var route = await _context.Routes.FindAsync(id);

        if (route == null)
            return NotFound();

        route.RouteName = dto.RouteName.Trim();
        route.DistanceKm = dto.DistanceKm;
        route.StartLocation = dto.StartLocation.Trim();
        route.FinishLocation = dto.FinishLocation.Trim();
        route.ElevationMetres = dto.ElevationMetres;

        await _context.SaveChangesAsync();

        return Ok(route);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var route = await _context.Routes.FindAsync(id);

        if (route == null)
            return NotFound();

        _context.Routes.Remove(route);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "This route cannot be deleted because it is in use."
            });
        }

        return NoContent();
    }
}
