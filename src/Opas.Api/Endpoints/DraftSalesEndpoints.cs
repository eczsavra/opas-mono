using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Opas.Infrastructure.Logging;
using Opas.Shared.Logging;
using Opas.Shared.Tenant;
using System.Text.Json;

namespace Opas.Api.Endpoints;

public static class DraftSalesEndpoints
{
    public static void MapDraftSalesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenant/draft-sales")
            .WithTags("Draft Sales")
            .WithOpenApi();

        // GET /api/tenant/draft-sales - Load draft sales for tenant
        group.MapGet("/", async (
            [FromQuery] string tenantId,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    return Results.BadRequest(new DraftSalesResponse 
                    { 
                        Success = false, 
                        Message = "tenantId is required" 
                    });
                }

                try
                {
                    // Build tenant connection string (TNT_<GLN> → opas_tenant_<GLN>)
                    var gln = tenantId.Replace("TNT_", "");
                    var tenantConnStr = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";

                    using var conn = new NpgsqlConnection(tenantConnStr);
                    await conn.OpenAsync(ct);

                    // Get all incomplete draft sales (ordered by display_order for correct tab sequence)
                    var query = @"
                        SELECT id, tab_id, tab_label, products, is_completed, 
                               created_by, created_at, updated_at, completed_at
                        FROM draft_sales 
                        WHERE is_completed = FALSE
                        ORDER BY COALESCE(display_order, 0) ASC, created_at ASC";

                    var tabs = new List<DraftSalesDto>();

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync(ct))
                    {
                        while (await reader.ReadAsync(ct))
                        {
                            var productsJson = reader.GetString(3);
                            var products = JsonSerializer.Deserialize<List<DraftProductDto>>(productsJson) ?? new();

                            tabs.Add(new DraftSalesDto
                            {
                                TabId = reader.GetString(1),
                                TabLabel = reader.GetString(2),
                                Products = products,
                                IsCompleted = reader.GetBoolean(4),
                                CreatedBy = reader.GetString(5),
                                CreatedAt = reader.GetDateTime(6),
                                UpdatedAt = reader.GetDateTime(7),
                                CompletedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
                            });
                        }
                    }

                    opasLogger.LogDataAccess("system", "DraftSales", "Load", new { TenantId = tenantId, Count = tabs.Count });

                    // Calculate tabCounter: max tab number + 1
                    int calculatedTabCounter = 1;
                    if (tabs.Count > 0)
                    {
                        var maxTabNumber = tabs.Max(t => 
                        {
                            // Parse "Satış #1" → 1, "Satış #2" → 2, etc. (remove ALL non-digits)
                            var numberStr = System.Text.RegularExpressions.Regex.Replace(t.TabLabel, @"[^\d]", "");
                            return int.TryParse(numberStr, out var num) ? num : 0;
                        });
                        calculatedTabCounter = maxTabNumber + 1;
                    }

                    return Results.Ok(new DraftSalesResponse
                    {
                        Success = true,
                        Tabs = tabs,
                        TabCounter = calculatedTabCounter,
                        Message = $"Loaded {tabs.Count} draft sales"
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("DraftSales", "LoadError", new { TenantId = tenantId, Error = ex.Message });
                    return Results.Problem(
                        statusCode: 500,
                        title: "Failed to load draft sales",
                        detail: ex.Message
                    );
                }
            }
        })
        .WithName("GetDraftSales")
        .WithSummary("Load draft sales for tenant")
        .Produces<DraftSalesResponse>(200)
        .Produces(400)
        .Produces(500);

        // POST /api/tenant/draft-sales/sync - Sync draft sales from client
        group.MapPost("/sync", async (
            [FromBody] SyncDraftSalesRequest request,
            [FromQuery] string tenantId,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    return Results.BadRequest(new { success = false, message = "tenantId is required" });
                }

                try
                {
                    // Build tenant connection string
                    var gln = tenantId.Replace("TNT_", "");
                    var tenantConnStr = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";

                    using var conn = new NpgsqlConnection(tenantConnStr);
                    await conn.OpenAsync(ct);

                    // 🔥 CRITICAL: Delete tabs that are NOT in the sync request (user closed them!)
                    if (request.Tabs.Count > 0)
                    {
                        var tabIds = request.Tabs.Select(t => t.TabId).ToList();
                        var deleteQuery = @"
                            DELETE FROM draft_sales 
                            WHERE is_completed = FALSE 
                            AND tab_id != ALL(@tab_ids)";
                        
                        using var deleteCmd = new NpgsqlCommand(deleteQuery, conn);
                        deleteCmd.Parameters.AddWithValue("@tab_ids", tabIds.ToArray());
                        await deleteCmd.ExecuteNonQueryAsync(ct);
                    }
                    else
                    {
                        // If no tabs, delete all incomplete drafts (user closed everything!)
                        var deleteAllQuery = "DELETE FROM draft_sales WHERE is_completed = FALSE";
                        using var deleteCmd = new NpgsqlCommand(deleteAllQuery, conn);
                        await deleteCmd.ExecuteNonQueryAsync(ct);
                    }

                    // Upsert each tab (insert or update) with display_order to maintain tab sequence
                    int displayOrder = 0;
                    foreach (var tab in request.Tabs)
                    {
                        var productsJson = JsonSerializer.Serialize(tab.Products);

                        var upsertQuery = @"
                            INSERT INTO draft_sales (tab_id, tab_label, products, is_completed, created_by, created_at, updated_at, display_order)
                            VALUES (@tab_id, @tab_label, @products::jsonb, @is_completed, @created_by, @created_at, @updated_at, @display_order)
                            ON CONFLICT (tab_id) 
                            DO UPDATE SET 
                                tab_label = EXCLUDED.tab_label,
                                products = EXCLUDED.products,
                                updated_at = EXCLUDED.updated_at,
                                display_order = EXCLUDED.display_order
                            WHERE draft_sales.is_completed = FALSE";

                        using var cmd = new NpgsqlCommand(upsertQuery, conn);
                        cmd.Parameters.AddWithValue("@tab_id", tab.TabId);
                        cmd.Parameters.AddWithValue("@tab_label", tab.TabLabel);
                        cmd.Parameters.AddWithValue("@products", productsJson);
                        cmd.Parameters.AddWithValue("@is_completed", tab.IsCompleted);
                        cmd.Parameters.AddWithValue("@created_by", tab.CreatedBy);
                        cmd.Parameters.AddWithValue("@created_at", tab.CreatedAt);
                        cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@display_order", displayOrder++); // Increment order for each tab

                        await cmd.ExecuteNonQueryAsync(ct);
                    }

                    opasLogger.LogDataAccess("system", "DraftSales", "Sync", new { TenantId = tenantId, Count = request.Tabs.Count });

                    return Results.Ok(new { success = true, message = $"Synced {request.Tabs.Count} draft sales" });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("DraftSales", "SyncError", new { TenantId = tenantId, Error = ex.Message });
                    return Results.Problem(
                        statusCode: 500,
                        title: "Failed to sync draft sales",
                        detail: ex.Message
                    );
                }
            }
        })
        .WithName("SyncDraftSales")
        .WithSummary("Sync draft sales from client to backend")
        .Produces<object>(200)
        .Produces(400)
        .Produces(500);

        // DELETE /api/tenant/draft-sales/{tabId} - Delete completed sale
        group.MapDelete("/{tabId}", async (
            string tabId,
            [FromQuery] string tenantId,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(tabId))
                {
                    return Results.BadRequest(new { success = false, message = "tenantId and tabId are required" });
                }

                try
                {
                    // Build tenant connection string
                    var gln = tenantId.Replace("TNT_", "");
                    var tenantConnStr = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";

                    using var conn = new NpgsqlConnection(tenantConnStr);
                    await conn.OpenAsync(ct);

                    // Mark as completed (soft delete)
                    var updateQuery = @"
                        UPDATE draft_sales 
                        SET is_completed = TRUE, 
                            completed_at = @completed_at,
                            updated_at = @updated_at
                        WHERE tab_id = @tab_id";

                    using var cmd = new NpgsqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@tab_id", tabId);
                    cmd.Parameters.AddWithValue("@completed_at", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);

                    var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);

                    if (rowsAffected == 0)
                    {
                        return Results.NotFound(new { success = false, message = "Draft sale not found" });
                    }

                    opasLogger.LogDataAccess("system", "DraftSales", "Delete", new { TenantId = tenantId, TabId = tabId });

                    return Results.Ok(new { success = true, message = "Draft sale marked as completed" });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("DraftSales", "DeleteError", new { TenantId = tenantId, TabId = tabId, Error = ex.Message });
                    return Results.Problem(
                        statusCode: 500,
                        title: "Failed to delete draft sale",
                        detail: ex.Message
                    );
                }
            }
        })
        .WithName("DeleteDraftSale")
        .WithSummary("Mark draft sale as completed")
        .Produces<object>(200)
        .Produces(400)
        .Produces(404)
        .Produces(500);
    }
}

