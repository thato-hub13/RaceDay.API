
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.API.Swagger;

public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        var methodAttributes = context.MethodInfo
            .GetCustomAttributes(true);

        var controllerAttributes = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true) ?? [];

        var attributes = controllerAttributes
            .Concat(methodAttributes)
            .ToArray();

        // AllowAnonymous overrides Authorize for this action.
        if (attributes.OfType<AllowAnonymousAttribute>().Any())
            return;

        var requiresAuth =
            attributes.OfType<AuthorizeAttribute>().Any();

        if (!requiresAuth)
            return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }
            ] = Array.Empty<string>()
        });

        // Make role requirements visible in Swagger documentation.
        var roles = attributes
            .OfType<AuthorizeAttribute>()
            .SelectMany(a => (a.Roles ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries |
                            StringSplitOptions.TrimEntries))
            .Distinct()
            .ToArray();

        if (roles.Length > 0)
        {
            operation.Description =
                $"{operation.Description}\n\nRequired role(s): {string.Join(", ", roles)}";
        }
    }
}
