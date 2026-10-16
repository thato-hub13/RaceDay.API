
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Test;

public class AuthControllerTests : IDisposable
{
    private readonly RaceDayDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly AuthController _controller;

    private const string JwtKey =
        "RaceDay-Test-Only-Signing-Key-1234567890";

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new RaceDayDbContext(options);

        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = JwtKey,
            ["Jwt:Issuer"] = "RaceDay.API",
            ["Jwt:Audience"] = "RaceDay.Client",
            ["Jwt:ExpiryMinutes"] = "60"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        _controller = new AuthController(_context, _configuration);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static RegisterDto CreateRegisterDto(
        string email = "runner@example.com")
    {
        return new RegisterDto("Test Runner", email, "StrongPassword123!");
    }

    private static LoginDto CreateLoginDto(
        string email = "runner@example.com",
        string password = "StrongPassword123!")
    {
        return new LoginDto(email, password);
    }

    private async Task<UserAccount> CreateUserAsync(
        string email = "runner@example.com",
        string password = "StrongPassword123!",
        string role = "Participant")
    {
        var user = new UserAccount
        {
            FullName = "Test Runner",
            EmailAddress = email.ToLowerInvariant(),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        var hasher = new PasswordHasher<UserAccount>();
        user.PasswordHash = hasher.HashPassword(user, password);

        _context.UserAccounts.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    

    

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        await CreateUserAsync();
        var dto = CreateRegisterDto("RUNNER@example.com");

        // Act
        var result = await _controller.Register(dto);

        // Assert
        Assert.IsType<ConflictObjectResult>(result);

        Assert.Equal(1, await _context.UserAccounts.CountAsync());
        Assert.Equal(0, await _context.Participants.CountAsync());
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsJwtToken()
    {
        // Arrange
        var user = await CreateUserAsync(role: "Participant");

        // Act
        var result = await _controller.Login(CreateLoginDto());

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var responseType = ok.Value!.GetType();

        var token = (string)responseType
            .GetProperty("accessToken")!.GetValue(ok.Value)!;

        var expiresAt = (DateTime)responseType
            .GetProperty("expiresAtUtc")!.GetValue(ok.Value)!;

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTime.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("RaceDay.API", jwt.Issuer);
        Assert.Contains("RaceDay.Client", jwt.Audiences);

        Assert.Equal(
            user.UserId.ToString(),
            jwt.Claims.Single(c =>
                c.Type == ClaimTypes.NameIdentifier).Value);

        Assert.Equal(
            "Participant",
            jwt.Claims.Single(c =>
                c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public async Task Login_OrganiserAccount_IncludesOrganiserRole()
    {
        // Arrange
        await CreateUserAsync(
            email: "organiser@raceday.com",
            role: "Organiser");

        // Act
        var result = await _controller.Login(
            CreateLoginDto("organiser@raceday.com"));

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var token = (string)ok.Value!.GetType()
            .GetProperty("accessToken")!.GetValue(ok.Value)!;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(
            "Organiser",
            jwt.Claims.Single(c =>
                c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        // Act
        var result = await _controller.Login(
            CreateLoginDto("unknown@example.com"));

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_IncorrectPassword_ReturnsUnauthorized()
    {
        // Arrange
        await CreateUserAsync();

        // Act
        var result = await _controller.Login(
            CreateLoginDto(password: "WrongPassword123!"));

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_NormalisesEmailAddress()
    {
        // Arrange
        await CreateUserAsync();

        // Act
        var result = await _controller.Login(
            CreateLoginDto("  RUNNER@EXAMPLE.COM  "));

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_MissingJwtKey_ReturnsServerError()
    {
        // Arrange
        await CreateUserAsync();

        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "RaceDay.API",
            ["Jwt:Audience"] = "RaceDay.Client",
            ["Jwt:ExpiryMinutes"] = "60"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var controller = new AuthController(_context, configuration);

        // Act
        var result = await controller.Login(CreateLoginDto());

        // Assert
        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
    }

    [Fact]
    public async Task Login_ShortJwtKey_ReturnsServerError()
    {
        // Arrange
        await CreateUserAsync();

        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "short-key",
            ["Jwt:Issuer"] = "RaceDay.API",
            ["Jwt:Audience"] = "RaceDay.Client",
            ["Jwt:ExpiryMinutes"] = "60"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var controller = new AuthController(_context, configuration);

        // Act
        var result = await controller.Login(CreateLoginDto());

        // Assert
        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
    }
}
