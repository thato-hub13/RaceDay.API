
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<UserAccount> _hasher = new();

    public AuthController(
        RaceDayDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var email = dto.EmailAddress.Trim().ToLowerInvariant();

        if (await _context.UserAccounts.AnyAsync(
                u => u.EmailAddress == email))
        {
            return Conflict(new { message = "Email already exists." });
        }

        var user = new UserAccount
        {
            FullName = dto.FullName.Trim(),
            EmailAddress = email,
            Role = "Participant",
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        _context.UserAccounts.Add(user);
        await _context.SaveChangesAsync();

        _context.Participants.Add(new Participant
        {
            UserId = user.UserId
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Created("/api/participants/me", new
        {
            user.UserId,
            user.FullName,
            user.EmailAddress,
            user.Role
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.EmailAddress.Trim().ToLowerInvariant();

        var user = await _context.UserAccounts
            .SingleOrDefaultAsync(u => u.EmailAddress == email);

        if (user == null)
            return Unauthorized(new { message = "Invalid credentials." });

        var verification = _hasher.VerifyHashedPassword(
            user, user.PasswordHash, dto.Password);

        if (verification == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid credentials." });

        var jwt = _configuration.GetSection("Jwt");
        var key = jwt["Key"];

        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            return StatusCode(500, new
            {
                message = "JWT signing configuration is invalid."
            });

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.EmailAddress),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            signingKey, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(
            int.TryParse(jwt["ExpiryMinutes"], out var minutes)
                ? Math.Clamp(minutes, 5, 1440)
                : 60);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc = expires,
            user = new
            {
                user.UserId,
                user.FullName,
                user.EmailAddress,
                user.Role
            }
        });
    }
}
