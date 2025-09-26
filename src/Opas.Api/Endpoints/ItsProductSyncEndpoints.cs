using Microsoft.AspNetCore.Mvc;
using Opas.Infrastructure.Services;
using Opas.Shared.Logging;
using Opas.Infrastructure.Logging;

namespace Opas.Api.Endpoints;

/// <summary>
/// ITS ürün senkronizasyon endpoints
/// </summary>
public static class ItsProductSyncEndpoints
{
    public static void MapItsProductSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/its/sync")
            .WithTags("ITS Product Sync")
            .WithDescription("ITS ürün senkronizasyon işlemleri");

        // POST /api/its/sync/tenant/{tenantId} - Belirli tenant için sync
        group.MapPost("/tenant/{tenantId}", async (
            string tenantId,
            ItsTenantSyncService syncService,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            [FromQuery] bool onlyNew = true) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                opasLogger.LogSystemEvent("TenantProductSync", $"Manual sync requested for tenant {tenantId}", new { 
                    TenantId = tenantId, 
                    OnlyNew = onlyNew, 
                    IP = clientIP 
                });

                try
                {
                    await syncService.SyncTenantProductsAsync(tenantId, onlyNew);
                    
                    return Results.Ok(new
                    {
                        success = true,
                        message = $"Tenant {tenantId} product sync completed successfully",
                        tenantId = tenantId,
                        onlyNew = onlyNew,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("TenantProductSync", $"Sync failed for tenant {tenantId}: {ex.Message}", new { 
                        TenantId = tenantId,
                        Error = ex.Message,
                        IP = clientIP
                    });

                    return Results.Problem(
                        title: "Product Sync Failed",
                        detail: $"Failed to sync products for tenant {tenantId}: {ex.Message}",
                        statusCode: 500
                    );
                }
            }
        })
        .WithName("SyncTenantProducts")
        .WithSummary("Belirli tenant için ürün senkronizasyonu")
        .WithDescription("ITS'den alınan ürünleri belirtilen tenant'ın DB'sine senkronize eder")
        .Produces<object>(200)
        .Produces<ProblemDetails>(500);

        // POST /api/its/sync/all - Tüm tenantlar için sync
        group.MapPost("/all", async (
            ItsTenantSyncService syncService,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            [FromQuery] bool onlyNew = true) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                opasLogger.LogSystemEvent("AllTenantsProductSync", "Manual sync requested for all tenants", new { 
                    OnlyNew = onlyNew, 
                    IP = clientIP 
                });

                try
                {
                    await syncService.SyncAllTenantsAsync(onlyNew);
                    
                    return Results.Ok(new
                    {
                        success = true,
                        message = "All tenants product sync completed successfully",
                        onlyNew = onlyNew,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("AllTenantsProductSync", $"Sync failed for all tenants: {ex.Message}", new { 
                        Error = ex.Message,
                        IP = clientIP
                    });

                    return Results.Problem(
                        title: "Product Sync Failed",
                        detail: $"Failed to sync products for all tenants: {ex.Message}",
                        statusCode: 500
                    );
                }
            }
        })
        .WithName("SyncAllTenantsProducts")
        .WithSummary("Tüm tenantlar için ürün senkronizasyonu")
        .WithDescription("ITS'den alınan ürünleri tüm aktif tenantların DB'lerine senkronize eder")
        .Produces<object>(200)
        .Produces<ProblemDetails>(500);

        // POST /api/its/sync/tenant/{tenantId}/full - Belirli tenant için full sync (customization sıfırla)
        group.MapPost("/tenant/{tenantId}/full", async (
            string tenantId,
            ItsTenantSyncService syncService,
            IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                opasLogger.LogSystemEvent("TenantProductFullSync", $"Full sync requested for tenant {tenantId} (WARNING: customizations will be lost)", new { 
                    TenantId = tenantId, 
                    IP = clientIP 
                });

                try
                {
                    // Full sync - tüm customization'lar sıfırlanır
                    await syncService.SyncTenantProductsAsync(tenantId, onlyNew: false);
                    
                    return Results.Ok(new
                    {
                        success = true,
                        message = $"Full sync completed for tenant {tenantId}. All customizations have been reset to ITS defaults.",
                        tenantId = tenantId,
                        warning = "All customizations (prices, names) have been overwritten with ITS data",
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("TenantProductFullSync", $"Full sync failed for tenant {tenantId}: {ex.Message}", new { 
                        TenantId = tenantId,
                        Error = ex.Message,
                        IP = clientIP
                    });

                    return Results.Problem(
                        title: "Full Product Sync Failed",
                        detail: $"Failed to perform full sync for tenant {tenantId}: {ex.Message}",
                        statusCode: 500
                    );
                }
            }
        })
        .WithName("FullSyncTenantProducts")
        .WithSummary("Belirli tenant için full ürün senkronizasyonu (customization sıfırla)")
        .WithDescription("⚠️ UYARI: Tüm tenant customization'ları (fiyat, isim değişiklikleri) silinir ve ITS verisi ile değiştirilir!")
        .Produces<object>(200)
        .Produces<ProblemDetails>(500);
    }
}
