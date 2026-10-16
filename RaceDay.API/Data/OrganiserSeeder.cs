
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public static class OrganiserSeeder
{
    public static async Task SeedAsync(
        RaceDayDbContext context,
        IConfiguration configuration)
    {
        var email = configuration["InitialOrganiser:Email"];
        var password = configuration["InitialOrganiser:Password"];
        var organisationName =
            configuration["InitialOrganiser:OrganisationName"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(organisationName))
        {
            throw new InvalidOperationException(
                "Initial organiser configuration is missing.");
        }

        if (password.Length < 12)
        {
            throw new InvalidOperationException(
                "The organiser password must contain at least 12 characters.");
        }

        email = email.Trim().ToLowerInvariant();

        var existingUser = await context.UserAccounts
            .SingleOrDefaultAsync(u => u.EmailAddress == email);

        if (existingUser != null)
        {
            if (existingUser.Role != "Organiser")
            {
                throw new InvalidOperationException(
                    "The configured organiser email already belongs to a non-organiser account.");
            }

            var profileExists = await context.Organisers
                .AnyAsync(o => o.UserId == existingUser.UserId);

            if (!profileExists)
            {
                context.Organisers.Add(new Organiser
                {
                    UserId = existingUser.UserId,
                    OrganisationName = organisationName
                });

                await context.SaveChangesAsync();
            }

            return;
        }

        var user = new UserAccount
        {
            FullName = "RaceDay Organiser",
            EmailAddress = email,
            Role = "Organiser",
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = new PasswordHasher<UserAccount>()
            .HashPassword(user, password);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        context.UserAccounts.Add(user);
        await context.SaveChangesAsync();

        context.Organisers.Add(new Organiser
        {
            UserId = user.UserId,
            OrganisationName = organisationName
        });

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
