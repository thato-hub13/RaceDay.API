
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public UsersController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = await _context.UserAccounts
            .AsNoTracking()
            .Where(u => u.UserId == CurrentUserId)
            .Select(u => new
            {
                u.UserId,
                u.FullName,
                u.EmailAddress,
                u.Role,
                u.CreatedAt
            })
            .SingleOrDefaultAsync();

        return user == null ? NotFound() : Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
    {
        var user = await _context.UserAccounts
            .FindAsync(CurrentUserId);

        if (user == null)
            return NotFound();

        user.FullName = dto.FullName.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.UserId,
            user.FullName,
            user.EmailAddress,
            user.Role
        });
    }
}
