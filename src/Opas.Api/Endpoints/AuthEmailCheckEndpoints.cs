using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;

namespace Opas.Api.Endpoints;

public static class AuthEmailCheckEndpoints
{
    public static IEndpointRouteBuilder MapAuthEmailCheckEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/auth/check-email?email=user@example.com
        app.MapGet("/api/auth/check-email", async (
            [FromQuery] string? email,
            [FromServices] ControlPlaneDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Results.BadRequest(new { found = false, error = "Email is required" });
            }

            // Email format kontrolü
            if (!IsValidEmail(email))
            {
                return Results.BadRequest(new { found = false, error = "Invalid email format" });
            }

            // Database'de email ile kullanıcı ara
            var pharmacist = await db.PharmacistAdmins
                .AsNoTracking()
                .Where(p => p.Email == email.ToLowerInvariant())
                .Select(p => new { 
                    p.Username, 
                    p.FirstName, 
                    p.LastName, 
                    p.Email,
                    p.IsActive 
                })
                .FirstOrDefaultAsync(ct);

            if (pharmacist == null)
            {
                return Results.Ok(new { 
                    found = false, 
                    email = email.ToLowerInvariant(),
                    message = "Bu email adresi ile kayıtlı kullanıcı bulunamadı"
                });
            }

            if (!pharmacist.IsActive)
            {
                return Results.Ok(new { 
                    found = false, 
                    email = email.ToLowerInvariant(),
                    message = "Bu hesap aktif değil"
                });
            }

            return Results.Ok(new { 
                found = true, 
                email = pharmacist.Email,
                username = pharmacist.Username,
                firstName = pharmacist.FirstName,
                lastName = pharmacist.LastName,
                message = $"Kullanıcı bulundu: {pharmacist.FirstName} {pharmacist.LastName}"
            });
        })
        .WithName("CheckEmailExists")
        .WithOpenApi();

        return app;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
