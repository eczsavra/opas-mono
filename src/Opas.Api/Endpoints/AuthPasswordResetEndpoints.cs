using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace Opas.Api.Endpoints;

public static class AuthPasswordResetEndpoints
{
    public static void MapAuthPasswordResetEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth/password")
            .WithTags("Authentication")
            .WithOpenApi();

        // POST /api/auth/password/reset - Şifre sıfırlama (test için)
        group.MapPost("/reset", async (
            [FromBody] PasswordResetRequest request,
            ControlPlaneDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Results.BadRequest(new { success = false, error = "Kullanıcı adı ve yeni şifre gereklidir" });
            }

            // Kullanıcıyı bul
            var pharmacist = await db.PharmacistAdmins
                .Where(p => p.Username == request.Username.ToLowerInvariant())
                .FirstOrDefaultAsync();

            if (pharmacist == null)
            {
                return Results.Ok(new { success = false, error = "Kullanıcı bulunamadı" });
            }

            // Yeni şifre hash'le
            var (hash, salt) = HashPassword(request.NewPassword);
            
            // Şifreyi güncelle
            pharmacist.PasswordHash = hash;
            pharmacist.PasswordSalt = salt;
            pharmacist.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                success = true,
                message = "Şifre başarıyla sıfırlandı",
                username = pharmacist.Username
            });
        })
        .WithName("ResetPassword")
        .WithSummary("Şifre sıfırlama (test için)")
        .WithDescription("Test amaçlı şifre sıfırlama endpoint'i");
    }

    private static (string hash, string salt) HashPassword(string password)
    {
        // 64 byte (512 bit) key oluştur - Base64'te ~88 karakter
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var keyBytes = new byte[64];
        rng.GetBytes(keyBytes);
        var salt = Convert.ToBase64String(keyBytes);
        
        using var hmac = new HMACSHA512(keyBytes);
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        
        return (hash, salt);
    }
}

public record PasswordResetRequest(
    string Username,
    string NewPassword
);
