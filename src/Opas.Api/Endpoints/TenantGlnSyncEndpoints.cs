using Microsoft.AspNetCore.Mvc;
using Opas.Infrastructure.Services;
using Opas.Shared.Logging;

namespace Opas.Api.Endpoints;

public static class TenantGlnSyncEndpoints
{
    public sealed record TenantGlnSyncResponse(
        bool Success,
        string TenantId,
        int SyncedCount,
        string Message
    );

    public sealed record AllTenantGlnSyncResponse(
        bool Success,
        Dictionary<string, int> Results,
        string Message
    );

    public static IEndpointRouteBuilder MapTenantGlnSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenant/gln/sync")
            .WithTags("Tenant GLN Sync")
            .WithDescription("GLN sync operations for tenant databases");

        // Belirli tenant'a GLN sync
        group.MapPost("/{tenantId}", async (
            [FromRoute] string tenantId,
            [FromServices] TenantGlnSyncService syncService,
            [FromServices] IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            try
            {
                opasLogger.LogSystemEvent("TenantGlnSyncRequest", "Manual sync requested", new
                {
                    TenantId = tenantId,
                    ClientIP = clientIP
                });

                var syncedCount = await syncService.SyncGlnsToTenantAsync(tenantId, httpContext.RequestAborted);

                opasLogger.LogSystemEvent("TenantGlnSync", "Manual sync completed", new
                {
                    TenantId = tenantId,
                    SyncedCount = syncedCount,
                    ClientIP = clientIP
                });

                return Results.Ok(new TenantGlnSyncResponse(
                    true,
                    tenantId,
                    syncedCount,
                    $"{syncedCount} GLN {tenantId} tenant'ına sync edildi."
                ));
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("TenantGlnSync", "Manual sync failed", new
                {
                    TenantId = tenantId,
                    Error = ex.Message,
                    ClientIP = clientIP
                });

                return Results.Problem(
                    title: "GLN Sync Error",
                    detail: $"GLN sync failed for tenant {tenantId}: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("SyncGlnToTenant")
        .WithSummary("Sync GLNs to specific tenant")
        .WithDescription("Sync all GLNs from central database to specific tenant database");

        // Tüm tenant'lara GLN sync
        group.MapPost("/all", async (
            [FromServices] TenantGlnSyncService syncService,
            [FromServices] IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            try
            {
                opasLogger.LogSystemEvent("TenantGlnSyncAll", "Manual sync all requested", new
                {
                    ClientIP = clientIP
                });

                var results = await syncService.SyncGlnsToAllTenantsAsync(httpContext.RequestAborted);

                var totalSynced = results.Values.Where(x => x > 0).Sum();
                var successCount = results.Values.Count(x => x > 0);
                var failureCount = results.Values.Count(x => x < 0);

                opasLogger.LogSystemEvent("TenantGlnSyncAll", "Manual sync all completed", new
                {
                    TotalSynced = totalSynced,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    ClientIP = clientIP
                });

                return Results.Ok(new AllTenantGlnSyncResponse(
                    true,
                    results,
                    $"GLN sync completed - {successCount} success, {failureCount} failures, {totalSynced} total GLNs synced"
                ));
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("TenantGlnSyncAll", "Manual sync all failed", new
                {
                    Error = ex.Message,
                    ClientIP = clientIP
                });

                return Results.Problem(
                    title: "GLN Sync All Error",
                    detail: $"GLN sync failed for all tenants: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("SyncGlnToAllTenants")
        .WithSummary("Sync GLNs to all tenants")
        .WithDescription("Sync all GLNs from central database to all tenant databases");

        // GLN sync status/stats
        group.MapGet("/status", async (
            [FromServices] TenantGlnSyncService syncService,
            HttpContext httpContext) =>
        {
            try
            {
                // Bu endpoint daha sonra implement edilecek
                return Results.Ok(new
                {
                    Status = "GLN Sync Service Active",
                    LastSync = "Manual trigger only",
                    Message = "Use POST endpoints to trigger sync operations"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "GLN Sync Status Error",
                    detail: $"Error retrieving GLN sync status: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetGlnSyncStatus")
        .WithSummary("Get GLN sync status")
        .WithDescription("Get current GLN sync service status and information");

        return app;
    }
}
