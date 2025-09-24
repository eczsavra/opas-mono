using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Shared.ControlPlane;
using System.Security.Cryptography;
using System.Text;

namespace Opas.Api.Endpoints;

public static class AuthLoginEndpoints
{
    public static void MapAuthLoginEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth/login")
            .WithTags("Authentication")
            .WithOpenApi();

        // POST /api/auth/login - PharmacistAdmin giriş
        group.MapPost("/", async (
            [FromBody] LoginRequest request,
            ControlPlaneDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { success = false, error = "Kullanıcı adı ve şifre gereklidir" });
            }

            // Kullanıcıyı bul (username veya email ile)
            var pharmacist = await db.PharmacistAdmins
                .AsNoTracking()
                .Where(p => p.Username == request.Username.ToLowerInvariant() || p.Email == request.Username.ToLowerInvariant())
                .FirstOrDefaultAsync();

            if (pharmacist == null)
            {
                return Results.Ok(new { success = false, error = "Kullanıcı adı veya şifre hatalı" });
            }

            // Şifre kontrolü
            var passwordValid = VerifyPassword(request.Password, pharmacist.PasswordHash, pharmacist.PasswordSalt);
            Console.WriteLine($"🔐 Password verification: {passwordValid}");
            Console.WriteLine($"🔐 Input password: {request.Password}");
            Console.WriteLine($"🔐 Stored hash: {pharmacist.PasswordHash}");
            Console.WriteLine($"🔐 Stored salt: {pharmacist.PasswordSalt}");
            
            if (!passwordValid)
            {
                return Results.Ok(new { success = false, error = "Kullanıcı adı veya şifre hatalı" });
            }

            // Aktif kullanıcı kontrolü
            if (!pharmacist.IsActive)
            {
                return Results.Ok(new { success = false, error = "Hesabınız aktif değil. Lütfen yöneticinizle iletişime geçin" });
            }

            // Son giriş zamanını güncelle
            pharmacist.LastLoginAt = DateTime.UtcNow;
            db.PharmacistAdmins.Update(pharmacist);
            await db.SaveChangesAsync();

            // Başarılı giriş response
            return Results.Ok(new
            {
                success = true,
                message = "Giriş başarılı",
                user = new
                {
                    pharmacistId = pharmacist.PharmacistId,
                    username = pharmacist.Username,
                    email = pharmacist.Email,
                    firstName = pharmacist.FirstName,
                    lastName = pharmacist.LastName,
                    tenantId = pharmacist.TenantId,
                    tenantStatus = pharmacist.TenantStatus,
                    role = pharmacist.Role,
                    isEmailVerified = pharmacist.IsEmailVerified,
                    isPhoneVerified = pharmacist.IsPhoneVerified,
                    isNviVerified = pharmacist.IsNviVerified,
                    lastLoginAt = pharmacist.LastLoginAt
                }
            });
        })
        .WithName("LoginPharmacist")
        .WithSummary("PharmacistAdmin giriş")
        .WithDescription("Kayıtlı eczacıların sisteme giriş yapması için kullanılır")
        .Produces<object>(200)
        .Produces<object>(400)
        .Produces<object>(401);

        // GET /api/auth/login/check-username - Kullanıcı adı kontrolü
        group.MapGet("/check-username", async (
            [FromQuery] string username,
            ControlPlaneDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Results.BadRequest(new { success = false, error = "Kullanıcı adı gereklidir" });
            }

            var exists = await db.PharmacistAdmins
                .AsNoTracking()
                .AnyAsync(p => p.Username == username.ToLowerInvariant());

            return Results.Ok(new
            {
                success = true,
                exists = exists,
                message = exists ? "Bu kullanıcı adı zaten kullanılıyor" : "Kullanıcı adı müsait"
            });
        })
        .WithName("CheckUsername")
        .WithSummary("Kullanıcı adı kontrolü")
        .WithDescription("Giriş sırasında kullanıcı adının varlığını kontrol eder");
    }

    private static bool VerifyPassword(string password, string hash, string salt)
    {
        try
        {
            var trimmedSalt = salt.Length > 128 ? salt.Substring(0, 128) : salt;
            using var hmac = new HMACSHA512(Convert.FromBase64String(trimmedSalt));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(Convert.FromBase64String(hash));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔐 Hash verification error: {ex.Message}");
            return false;
        }
    }
}

public record LoginRequest(
    string Username,
    string Password
);
