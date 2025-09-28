using Microsoft.AspNetCore.Mvc;
using Opas.Infrastructure.Services;
using Opas.Shared.Logging;

namespace Opas.Api.Endpoints;

public static class TenantProductSyncEndpoints
{
    public static void MapTenantProductSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenant/products/sync")
            .WithTags("Tenant Product Sync")
            .WithDescription("Merkezi DB'den tenant'lara ürün sync işlemleri");

        // POST /api/tenant/products/sync/{tenantId} - Belirli tenant'a sync
        group.MapPost("/{tenantId}", async (
            string tenantId,
            TenantProductSyncService syncService,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            [FromQuery] bool onlyNew = true) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            opasLogger.LogSystemEvent("TenantProductSyncRequest", "Manual sync requested", new { 
                TenantId = tenantId, 
                OnlyNew = onlyNew,
                ClientIP = clientIP 
            });

            try
            {
                var syncedCount = await syncService.SyncProductsToTenantAsync(tenantId, onlyNew);
                
                opasLogger.LogSystemEvent("TenantProductSync", "Manual sync completed", new { 
                    TenantId = tenantId, 
                    SyncedCount = syncedCount,
                    ClientIP = clientIP 
                });

                return Results.Ok(new
                {
                    success = true,
                    tenantId,
                    syncedCount,
                    onlyNew,
                    message = $"{syncedCount} ürün {tenantId} tenant'ına sync edildi."
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("TenantProductSync", "Manual sync failed", new { 
                    TenantId = tenantId, 
                    Error = ex.Message,
                    ClientIP = clientIP 
                });

                return Results.Problem(
                    detail: ex.Message, 
                    statusCode: 500, 
                    title: "Tenant Product Sync Hatası"
                );
            }
        })
        .WithName("SyncProductsToTenant")
        .WithSummary("Merkezi DB'den belirli tenant'a ürün sync eder")
        .WithDescription("Merkezi 'central_products' tablosundan belirtilen tenant DB'sine ürün sync eder.");

        // POST /api/tenant/products/sync/all - Tüm tenant'lara sync
        group.MapPost("/all", async (
            TenantProductSyncService syncService,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            [FromQuery] bool onlyNew = true) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            opasLogger.LogSystemEvent("TenantProductSyncAllRequest", "Manual sync all requested", new { 
                OnlyNew = onlyNew,
                ClientIP = clientIP 
            });

            try
            {
                var results = await syncService.SyncProductsToAllTenantsAsync(onlyNew);
                var totalSynced = results.Values.Where(v => v > 0).Sum();
                var successCount = results.Values.Count(v => v >= 0);
                var errorCount = results.Values.Count(v => v < 0);
                
                opasLogger.LogSystemEvent("TenantProductSyncAll", "Manual sync all completed", new { 
                    TotalSynced = totalSynced,
                    SuccessfulTenants = successCount,
                    FailedTenants = errorCount,
                    ClientIP = clientIP 
                });

                return Results.Ok(new
                {
                    success = true,
                    totalSynced,
                    successfulTenants = successCount,
                    failedTenants = errorCount,
                    onlyNew,
                    results,
                    message = $"{totalSynced} ürün toplamda {successCount} tenant'a sync edildi."
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("TenantProductSyncAll", "Manual sync all failed", new { 
                    Error = ex.Message,
                    ClientIP = clientIP 
                });

                return Results.Problem(
                    detail: ex.Message, 
                    statusCode: 500, 
                    title: "Tenant Product Sync All Hatası"
                );
            }
        })
        .WithName("SyncProductsToAllTenants")
        .WithSummary("Merkezi DB'den tüm tenant'lara ürün sync eder")
        .WithDescription("Merkezi 'central_products' tablosundan tüm aktif tenant DB'lerine ürün sync eder.");

        // GET /api/tenant/products/sync/status/{tenantId} - Tenant sync durumu
        group.MapGet("/status/{tenantId}", (
            string tenantId,
            TenantProductSyncService syncService,
            IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            try
            {
                // Bu endpoint için basit bir status check yapabiliriz
                // Örneğin tenant'ın kaç ürünü var, son sync zamanı vs.
                
                return Results.Ok(new
                {
                    success = true,
                    tenantId,
                    message = "Status check endpoint - implement as needed"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message, 
                    statusCode: 500, 
                    title: "Tenant Sync Status Hatası"
                );
            }
        })
        .WithName("GetTenantSyncStatus")
        .WithSummary("Tenant'ın ürün sync durumunu getirir")
        .WithDescription("Belirtilen tenant'ın ürün sync istatistiklerini döndürür.");
    }
}
