
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/organisers")]
[Authorize(Roles = "Organiser")]
public class OrganisersController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public OrganisersController(RaceDayDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var organiser = await _context.Organisers
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new
            {
                o.OrganiserId,
                o.UserId,
                o.OrganisationName,
                o.ContactNumber
            })
            .SingleOrDefaultAsync();

        return organiser == null ? NotFound() : Ok(organiser);
    }
}
