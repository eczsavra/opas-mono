using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Infrastructure.Logging;
using Opas.Shared.ControlPlane;
using Opas.Shared.Logging;
// Removed unused imports for password hashing

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
            ControlPlaneDbContext db,
            IOpasLogger opasLogger,
            DatabaseLoggingService dbLogging,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    opasLogger.LogUserLogin(request.Username ?? "empty", clientIP, false, "Missing credentials");
                    await dbLogging.LogUserLoginAsync(request.Username ?? "empty", "unknown", clientIP, httpContext.Request.Headers.UserAgent.ToString(), false, "Missing credentials");
                    return Results.BadRequest(new { success = false, error = "Kullanıcı adı ve şifre gereklidir" });
                }

                // Kullanıcıyı bul (username veya email ile) - tenants tablosundan
                var tenant = await db.Tenants
                    .AsNoTracking()
                    .Where(t => t.Username == request.Username.ToLowerInvariant() || t.Email == request.Username.ToLowerInvariant())
                    .FirstOrDefaultAsync();

                if (tenant == null)
                {
                    opasLogger.LogUserLogin(request.Username, clientIP, false, "User not found");
                    await dbLogging.LogUserLoginAsync(request.Username, "unknown", clientIP, httpContext.Request.Headers.UserAgent.ToString(), false, "User not found");
                    return Results.Ok(new { success = false, error = "Kullanıcı adı veya şifre hatalı" });
                }

                // Şifre kontrolü (plain text karşılaştırma)
                var passwordValid = request.Password == tenant.Password;
                
                if (!passwordValid)
                {
                    opasLogger.LogUserLogin(tenant.Username, clientIP, false, "Invalid password");
                    await dbLogging.LogUserLoginAsync(tenant.Username, tenant.TId, clientIP, httpContext.Request.Headers.UserAgent.ToString(), false, "Invalid password");
                    return Results.Ok(new { success = false, error = "Kullanıcı adı veya şifre hatalı" });
                }

                // Aktif kullanıcı kontrolü
                if (!tenant.IsActive)
                {
                    opasLogger.LogUserLogin(tenant.Username, clientIP, false, "Account inactive");
                    await dbLogging.LogUserLoginAsync(tenant.Username, tenant.TId, clientIP, httpContext.Request.Headers.UserAgent.ToString(), false, "Account inactive");
                    return Results.Ok(new { success = false, error = "Hesabınız aktif değil. Lütfen yöneticinizle iletişime geçin" });
                }

                // Kayıt tamamlanmış mı kontrolü
                if (!tenant.IsCompleted)
                {
                    opasLogger.LogUserLogin(tenant.Username, clientIP, false, "Registration not completed");
                    await dbLogging.LogUserLoginAsync(tenant.Username, tenant.TId, clientIP, httpContext.Request.Headers.UserAgent.ToString(), false, "Registration not completed");
                    return Results.Ok(new { success = false, error = "Kayıt işleminiz henüz tamamlanmamış. Lütfen kayıt işlemini tamamlayın" });
                }

                // Son giriş zamanını güncelle
                tenant.KayitGuncellenmeZamani = DateTime.UtcNow;
                db.Tenants.Update(tenant);
                await db.SaveChangesAsync();

                // Başarılı giriş logla
                opasLogger.LogUserLogin(tenant.Username, clientIP, true);
                
                // Database'e log kaydet
                await dbLogging.LogUserLoginAsync(tenant.Username, tenant.TId, clientIP, httpContext.Request.Headers.UserAgent.ToString(), true);

                // Başarılı giriş response
                return Results.Ok(new
                {
                    success = true,
                    message = "Giriş başarılı",
                    user = new
                    {
                        tenantId = tenant.TId,
                        username = tenant.Username,
                        email = tenant.Email,
                        firstName = tenant.Ad,
                        lastName = tenant.Soyad,
                        gln = tenant.Gln,
                        eczaneAdi = tenant.EczaneAdi,
                        ili = tenant.Ili,
                        ilcesi = tenant.Ilcesi,
                        isEmailVerified = tenant.IsEmailVerified,
                        isCepTelVerified = tenant.IsCepTelVerified,
                        isNviVerified = tenant.IsNviVerified,
                        isCompleted = tenant.IsCompleted,
                        lastLoginAt = tenant.KayitGuncellenmeZamani
                    }
                });
            }
        })
        .WithName("LoginPharmacist")
        .WithSummary("PharmacistAdmin giriş")
        .WithDescription("Kayıtlı eczacıların sisteme giriş yapması için kullanılır")
        .Produces<object>(200)
        .Produces<object>(400)
        .Produces<object>(401);

        // POST /api/auth/logout - Çıkış yapma
        group.MapPost("/logout", async (
            [FromBody] LogoutRequest request,
            ControlPlaneDbContext db,
            IOpasLogger opasLogger,
            DatabaseLoggingService dbLogging,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return Results.BadRequest(new { success = false, error = "Kullanıcı adı gereklidir" });
                }

                // Kullanıcıyı bul - tenants tablosundan
                var tenant = await db.Tenants
                    .AsNoTracking()
                    .Where(t => t.Username == request.Username.ToLowerInvariant())
                    .FirstOrDefaultAsync();

                // Log kaydet (kullanıcı var olsun ya da olmasın)
                opasLogger.LogUserLogout(request.Username, clientIP);
                await dbLogging.LogUserLogoutAsync(
                    request.Username, 
                    tenant?.TId ?? "unknown", 
                    clientIP
                );

                return Results.Ok(new { success = true, message = "Çıkış başarılı" });
            }
        })
        .WithName("LogoutPharmacist")
        .WithSummary("PharmacistAdmin çıkış")
        .WithDescription("Kullanıcının çıkış yapması için kullanılır")
        .Produces<object>(200)
        .Produces<object>(400);

        // GET /api/auth/login/check-username - Kullanıcı adı kontrolü
        group.MapGet("/check-username", async (
            [FromQuery] string username,
            ControlPlaneDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Results.BadRequest(new { success = false, error = "Kullanıcı adı gereklidir" });
            }

            var exists = await db.Tenants
                .AsNoTracking()
                .AnyAsync(t => t.Username == username.ToLowerInvariant());

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

    // Password verification method removed - now using plain text comparison
    // private static bool VerifyPassword(string password, string hash, string salt) - no longer needed
}

public record LoginRequest(
    string Username,
    string Password
);

public record LogoutRequest(
    string Username
);
