using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;

namespace Opas.Api.Endpoints;

public static class AuthPhoneVerifyEndpoints
{
    public static IEndpointRouteBuilder MapAuthPhoneVerifyEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/auth/verify-phone-last4?username=erdem&lastFour=3555
        app.MapGet("/api/auth/verify-phone-last4", async (
            [FromQuery] string? username,
            [FromQuery] string? lastFour,
            [FromServices] ControlPlaneDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Results.BadRequest(new { valid = false, error = "Username is required" });
            }

            if (string.IsNullOrWhiteSpace(lastFour) || lastFour.Length != 4)
            {
                return Results.BadRequest(new { valid = false, error = "Last 4 digits required" });
            }

            // Sadece rakam kontrolü
            if (!lastFour.All(char.IsDigit))
            {
                return Results.BadRequest(new { valid = false, error = "Only digits allowed" });
            }

            // Database'de username ile tenant ara
            var tenant = await db.Tenants
                .AsNoTracking()
                .Where(t => t.Username == username.ToLowerInvariant())
                .Select(t => new { 
                    t.Username, 
                    t.CepTel,
                    t.IsCompleted 
                })
                .FirstOrDefaultAsync(ct);

            if (tenant == null)
            {
                return Results.Ok(new { 
                    valid = false, 
                    message = "Kullanıcı bulunamadı"
                });
            }

            if (!tenant.IsCompleted)
            {
                return Results.Ok(new { 
                    valid = false, 
                    message = "Hesap kaydı tamamlanmamış"
                });
            }

            if (string.IsNullOrWhiteSpace(tenant.CepTel))
            {
                return Results.Ok(new { 
                    valid = false, 
                    message = "Telefon numarası kayıtlı değil"
                });
            }

            // Telefon numarasının son 4 hanesi kontrolü
            var phoneLastFour = tenant.CepTel.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            phoneLastFour = phoneLastFour.Length >= 4 ? phoneLastFour.Substring(phoneLastFour.Length - 4) : phoneLastFour;

            Console.WriteLine($"🔍 DEBUG - Phone verify: Username={username}, StoredPhone={tenant.CepTel}, LastFour={phoneLastFour}, Entered={lastFour}");

            var isValid = phoneLastFour == lastFour;

            return Results.Ok(new { 
                valid = isValid,
                message = isValid ? "Telefon doğrulandı" : "Telefon numarasının son 4 hanesi hatalı"
            });
        })
        .WithName("VerifyPhoneLast4")
        .WithOpenApi();

        return app;
    }
}
