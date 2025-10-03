using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Infrastructure.Logging;
using Opas.Shared.Logging;

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
                var tenant = await db.Tenants
                    .Where(t => t.Username == request.Username.ToLowerInvariant())
                    .FirstOrDefaultAsync();

                if (tenant == null)
                {
                    opasLogger.LogSystemEvent("PasswordResetUserNotFound", $"Password reset failed - user not found: {request.Username}", new {
                        Username = request.Username,
                        IP = clientIP
                    });
                    return Results.Ok(new { success = false, error = "Kullanıcı bulunamadı" });
                }

                // Log password change attempt
                opasLogger.LogPasswordChange(tenant.Username, clientIP, true, "Password reset initiated");

                // Şifreyi güncelle (plain text - tenant tablosunda hash yok)
                tenant.Password = request.NewPassword;
                tenant.KayitGuncellenmeZamani = DateTime.UtcNow;

                await db.SaveChangesAsync();

                // Tenant DB'deki tenant_info tablosunu da güncelle
                var gln = tenant.Gln;
                var tenantDbName = $"opas_tenant_{gln}";
                var baseConnectionString = db.Database.GetConnectionString();
                var tenantConnectionString = baseConnectionString?.Replace("Database=opas_control", $"Database={tenantDbName}");

                Console.WriteLine($"🔍 DEBUG - Tenant DB update: GLN={gln}, DB={tenantDbName}");
                Console.WriteLine($"🔍 DEBUG - Connection: {tenantConnectionString}");

                using var tenantConnection = new Npgsql.NpgsqlConnection(tenantConnectionString);
                await tenantConnection.OpenAsync();

                using var updateCommand = new Npgsql.NpgsqlCommand(
                    "UPDATE tenant_info SET password = @password, kayit_guncellenme_zamani = NOW() WHERE username = @username",
                    tenantConnection);
                
                updateCommand.Parameters.AddWithValue("@password", request.NewPassword);
                updateCommand.Parameters.AddWithValue("@username", tenant.Username);
                
                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

                Console.WriteLine($"🔍 DEBUG - SQL executed, rows affected: {rowsAffected}");

                // Log successful password reset
                opasLogger.LogPasswordChange(tenant.Username, clientIP, true, "Password reset completed successfully");

                return Results.Ok(new
                {
                    success = true,
                    message = "Şifre başarıyla sıfırlandı",
                    username = tenant.Username
                });
            }
        })
        .WithName("ResetPassword")
        .WithSummary("Şifre sıfırlama (test için)")
        .WithDescription("Test amaçlı şifre sıfırlama endpoint'i");
    }

}

public record PasswordResetRequest(
    string Username,
    string NewPassword
);
