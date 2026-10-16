
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/participants")]
[Authorize(Roles = "Participant")]
public class ParticipantsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ParticipantsController(RaceDayDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var participant = await _context.Participants
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.ParticipantId,
                p.UserId,
                p.DateOfBirth,
                p.EmergencyContact,
                FullName = p.UserAccount.FullName,
                EmailAddress = p.UserAccount.EmailAddress
            })
            .SingleOrDefaultAsync();

        return participant == null ? NotFound() : Ok(participant);
    }
}
