using Microsoft.AspNetCore.Mvc;
using Opas.Infrastructure.Services;
using Opas.Shared.Logging;
using Opas.Infrastructure.Logging;

namespace Opas.Api.Endpoints;

public static class CentralProductEndpoints
{
    public static void MapCentralProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/central/products")
            .WithTags("Central Products")
            .WithDescription("Merkezi DB ürün yönetimi - ITS'den beslenme");

        // POST /api/central/products/sync-from-its - ITS'den merkezi DB'ye sync
        group.MapPost("/sync-from-its", async (
            CentralProductSyncService syncService,
            IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            opasLogger.LogSystemEvent("CentralProductSyncRequest", "Manual sync from ITS requested", new { 
                ClientIP = clientIP
            });

            try
            {
                var syncedCount = await syncService.SyncProductsFromItsAsync();
                
                opasLogger.LogSystemEvent("CentralProductSyncRequest", "Completed successfully", new { 
                    ClientIP = clientIP,
                    SyncedCount = syncedCount
                });

                return Results.Ok(new
                {
                    success = true,
                    syncedCount,
                    message = $"{syncedCount} ürün merkezi DB'ye sync edildi"
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("CentralProductSyncRequest", $"Failed: {ex.Message}", new { 
                    ClientIP = clientIP,
                    Error = ex.Message
                });

                return Results.Problem(
                    title: "Central Product Sync Error",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("SyncProductsFromIts")
        .WithDescription("ITS'den ürünleri çeker ve merkezi DB'ye sync eder");

        // GET /api/central/products/stats - Merkezi DB istatistikleri
        group.MapGet("/stats", async (
            CentralProductSyncService syncService,
            IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            try
            {
                var totalCount = await syncService.GetCentralProductCountAsync();
                var activeCount = await syncService.GetActiveProductCountAsync();

                opasLogger.LogSystemEvent("CentralProductStats", "Stats requested", new { 
                    ClientIP = clientIP,
                    TotalCount = totalCount,
                    ActiveCount = activeCount
                });

                return Results.Ok(new
                {
                    success = true,
                    stats = new
                    {
                        totalProducts = totalCount,
                        activeProducts = activeCount,
                        inactiveProducts = totalCount - activeCount
                    }
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Central Product Stats Error",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("GetCentralProductStats")
        .WithDescription("Merkezi DB ürün istatistikleri");
    }
}
