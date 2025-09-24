using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;

namespace Opas.Api.Endpoints;

public static class AuthUsernameEndpoints
{
    public static IEndpointRouteBuilder MapAuthUsernameEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/auth/check-username?username=johndoe
        app.MapGet("/api/auth/check-username", async (
            [FromQuery] string? username,
            [FromServices] ControlPlaneDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Results.BadRequest(new { available = false, error = "Username is required" });
            }

            // Username'i normalize et (lowercase, trim)
            var normalizedUsername = username.Trim().ToLowerInvariant();

            // Format kontrolü
            if (normalizedUsername.Length < 3 || normalizedUsername.Length > 50)
            {
                return Results.BadRequest(new { available = false, error = "Username must be between 3-50 characters" });
            }

            // Regex kontrolü - sadece alphanumeric, dot, dash, underscore
            if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedUsername, @"^[a-z0-9._-]+$"))
            {
                return Results.BadRequest(new { available = false, error = "Username can only contain letters, numbers, dots, dashes and underscores" });
            }

            // Reserved username kontrolü
            var reservedUsernames = new[] { 
                "admin", "administrator", "root", "system", "api", "www", "mail", "email", 
                "support", "help", "info", "contact", "about", "privacy", "terms",
                "opas", "pharmacy", "eczane", "test", "demo", "null", "undefined"
            };

            if (reservedUsernames.Contains(normalizedUsername))
            {
                return Results.BadRequest(new { available = false, error = "This username is reserved" });
            }

            // Database'de username kontrolü - NEW: PharmacistAdmin ve SubUser tablosuna bak (User tablosuna değil)
            var existsInPharmacistAdmin = await db.PharmacistAdmins
                .AsNoTracking()
                .AnyAsync(u => u.Username == normalizedUsername, ct);
            
            var existsInSubUsers = await db.SubUsers
                .AsNoTracking()
                .AnyAsync(u => u.Username == normalizedUsername, ct);
                
            var exists = existsInPharmacistAdmin || existsInSubUsers;

            return Results.Ok(new { 
                available = !exists, 
                username = normalizedUsername,
                message = !exists ? "Username is available" : "Username is already taken"
            });
        })
        .WithName("CheckUsernameAvailability")
        .WithOpenApi();

        return app;
    }
}
