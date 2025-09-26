using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Infrastructure.Logging;
using Opas.Shared.Logging;
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
            ControlPlaneDbContext db,
            IOpasLogger opasLogger,
            DatabaseLoggingService dbLogging,
            TenantLoggingService tenantLogging,
            ManagementLoggingService managementLogging,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                // Log password reset attempt
                opasLogger.LogSystemEvent("PasswordResetAttempt", $"Password reset attempt for user {request.Username} from {clientIP}", new {
                    Username = request.Username,
                    IP = clientIP
                });

                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    opasLogger.LogSystemEvent("PasswordResetValidationFailed", "Password reset validation failed - missing credentials", new {
                        Username = request.Username,
                        IP = clientIP
                    });
                    return Results.BadRequest(new { success = false, error = "Kullanıcı adı ve yeni şifre gereklidir" });
                }

                // Kullanıcıyı bul
                var pharmacist = await db.PharmacistAdmins
                    .Where(p => p.Username == request.Username.ToLowerInvariant())
                    .FirstOrDefaultAsync();

                if (pharmacist == null)
                {
                    opasLogger.LogSystemEvent("PasswordResetUserNotFound", $"Password reset failed - user not found: {request.Username}", new {
                        Username = request.Username,
                        IP = clientIP
                    });
                    return Results.Ok(new { success = false, error = "Kullanıcı bulunamadı" });
                }

                // Log password change attempt
                opasLogger.LogPasswordChange(pharmacist.Username, clientIP, true, "Password reset initiated");

                // Yeni şifre hash'le
                var (hash, salt) = HashPassword(request.NewPassword);
                
                // Şifreyi güncelle
                pharmacist.PasswordHash = hash;
                pharmacist.PasswordSalt = salt;
                pharmacist.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                // Log successful password reset
                opasLogger.LogPasswordChange(pharmacist.Username, clientIP, true, "Password reset completed successfully");

                // Log to management system
                managementLogging.LogGlobalSecurity("PasswordReset", $"Password reset completed for user {pharmacist.Username}", new {
                    Username = pharmacist.Username,
                    PharmacistId = pharmacist.PharmacistId,
                    TenantId = pharmacist.TenantId,
                    IP = clientIP
                });

                // Log to tenant-specific logs
                tenantLogging.LogTenantSecurity(pharmacist.TenantId, pharmacist.PharmacistId, "PasswordReset", clientIP, new {
                    Username = pharmacist.Username,
                    IP = clientIP
                });

                // Database'e log kaydet
                await dbLogging.LogPasswordResetAsync(pharmacist.Username, pharmacist.TenantId, clientIP, true);

                return Results.Ok(new
                {
                    success = true,
                    message = "Şifre başarıyla sıfırlandı",
                    username = pharmacist.Username
                });
            }
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
