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

            // Database'de email ile tenant ara
            var tenant = await db.Tenants
                .AsNoTracking()
                .Where(t => t.Email == email.ToLowerInvariant())
                .Select(t => new { 
                    t.Username, 
                    t.Ad, 
                    t.Soyad, 
                    t.Email,
                    t.Gln,
                    t.IsCompleted 
                })
                .FirstOrDefaultAsync(ct);

            Console.WriteLine($"🔍 DEBUG - Tenant from DB: Username={tenant?.Username}, Gln={tenant?.Gln}, Email={tenant?.Email}");

            if (tenant == null)
            {
                return Results.Ok(new { 
                    found = false, 
                    email = email.ToLowerInvariant(),
                    message = "Bu email adresi ile kayıtlı kullanıcı bulunamadı"
                });
            }

            if (!tenant.IsCompleted)
            {
                return Results.Ok(new { 
                    found = false, 
                    email = email.ToLowerInvariant(),
                    message = "Bu hesabın kaydı tamamlanmamış"
                });
            }

            var response = new { 
                found = true, 
                email = tenant.Email,
                username = tenant.Username,
                firstName = tenant.Ad,
                lastName = tenant.Soyad,
                gln = tenant.Gln,
                message = $"Kullanıcı bulundu: {tenant.Ad} {tenant.Soyad}"
            };
            
            Console.WriteLine($"🔍 DEBUG - Response object: {System.Text.Json.JsonSerializer.Serialize(response)}");
            
            return Results.Ok(response);
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
