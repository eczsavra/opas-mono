// InfraItsTokenEndpoints.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure;
using Opas.Infrastructure.Persistence;
using Opas.Shared.Logging;
using Opas.Infrastructure.Logging;

namespace Opas.Api.Endpoints.Infra;

/// <summary>
/// ITS token yönetimi endpoints - merkezi token servisi
/// </summary>
public static class InfraItsTokenEndpoints
{
    public static void MapInfraItsTokenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/infra/token")
            .WithTags("Infrastructure - ITS Token Management")
            .WithDescription("ITS token yönetimi");

        // POST /api/infra/token/its/refresh - ITS token yenileme
        group.MapPost("/its/refresh", async (
            ItsTokenService itsTokenService,
            IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                opasLogger.LogSystemEvent("ItsTokenRefresh", "Manual token refresh requested", new { IP = clientIP });

                var token = await itsTokenService.GetAndStoreTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    opasLogger.LogSystemEvent("ItsTokenRefresh", "Failed to refresh token", new { IP = clientIP });
                    return Results.Problem(
                        detail: "ITS token alınamadı. Kimlik bilgilerini kontrol edin.",
                        statusCode: 500,
                        title: "Token Alma Hatası"
                    );
                }

                opasLogger.LogSystemEvent("ItsTokenRefresh", "Token refreshed successfully", new { 
                    TokenLength = token.Length,
                    IP = clientIP 
                });

                return Results.Ok(new
                {
                    success = true,
                    tokenLength = token.Length,
                    message = "ITS token başarıyla yenilendi",
                    refreshedAt = DateTime.UtcNow
                });
            }
        })
        .WithName("RefreshItsToken")
        .WithSummary("ITS token yenile")
        .WithDescription("ITS'den yeni token alır ve database'e kaydeder")
        .Produces<object>(200)
        .Produces<object>(500);

        // GET /api/infra/token/its/status - ITS token durumu
        group.MapGet("/its/status", async (
            ControlPlaneDbContext db,
            IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                var token = await db.Tokens
                    .Where(x => x.Name == "ITS-Access")
                    .FirstOrDefaultAsync();

                if (token == null)
                {
                    opasLogger.LogSystemEvent("ItsTokenStatus", "No token found", new { IP = clientIP });
                    return Results.Ok(new
                    {
                        hasToken = false,
                        message = "ITS token bulunamadı"
                    });
                }

                var isExpired = token.ExpiresAtUtc <= DateTime.UtcNow;
                var timeRemaining = token.ExpiresAtUtc - DateTime.UtcNow;
                var remainingMinutes = timeRemaining?.TotalMinutes ?? 0;

                opasLogger.LogSystemEvent("ItsTokenStatus", "Token status checked", new { 
                    IsExpired = isExpired,
                    TimeRemaining = remainingMinutes,
                    IP = clientIP 
                });

                return Results.Ok(new
                {
                    hasToken = true,
                    isExpired = isExpired,
                    expiresAt = token.ExpiresAtUtc,
                    timeRemainingMinutes = (int)remainingMinutes,
                    createdAt = token.CreatedAt,
                    message = isExpired ? "Token süresi dolmuş" : $"Token geçerli ({(int)remainingMinutes} dakika kaldı)"
                });
            }
        })
        .WithName("ItsTokenStatus")
        .WithSummary("ITS token durumu")
        .WithDescription("Mevcut ITS token'ının durumunu kontrol eder")
        .Produces<object>(200);
    }
}
