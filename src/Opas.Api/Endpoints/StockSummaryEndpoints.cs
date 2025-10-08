using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Opas.Infrastructure.Logging;
using Opas.Shared.Logging;
using Opas.Shared.Stock;

namespace Opas.Api.Endpoints;

public static class StockSummaryEndpoints
{
    public static void MapStockSummaryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenant/stock/summary")
            .WithTags("Stock Summary")
            .WithDescription("Stok özet bilgileri (hızlı sorgu için cache)");

        // GET: Tüm ürünlerin stok özeti
        group.MapGet("/", GetAllStockSummary)
            .WithName("GetAllStockSummary")
            .WithDescription("Tenant'ın tüm ürünlerinin stok özetini getirir");

        // GET: Tek bir ürünün stok özeti
        group.MapGet("/{productId}", GetStockSummaryByProduct)
            .WithName("GetStockSummaryByProduct")
            .WithDescription("Belirli bir ürünün stok özetini getirir");

        // POST: Stok özetini yeniden hesapla
        group.MapPost("/recalculate/{productId}", RecalculateStockSummary)
            .WithName("RecalculateStockSummary")
            .WithDescription("Ürün için stok özetini yeniden hesaplar (manuel sync)");

        // GET: Uyarı gerektiren ürünler
        group.MapGet("/alerts", GetStockAlerts)
            .WithName("GetStockAlerts")
            .WithDescription("SKT yakın, stok az, vb. uyarı gerektiren ürünleri listeler");
    }

    private static async Task<IResult> GetAllStockSummary(
        [FromQuery] string tenantId,
        IOpasLogger opasLogger,
        HttpContext httpContext,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool? hasLowStock = null,
        [FromQuery] bool? hasExpiringSoon = null,
        CancellationToken ct = default)
    {
        using (OpasLogContext.EnrichFromHttpContext(httpContext))
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Results.BadRequest(new { success = false, message = "tenantId is required" });
            }

            try
            {
                var gln = tenantId.Replace("TNT_", "");
                var tenantConnStr = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";

                using var conn = new NpgsqlConnection(tenantConnStr);
                await conn.OpenAsync(ct);

                var whereClause = "WHERE 1=1";
                if (hasLowStock.HasValue)
                    whereClause += $" AND has_low_stock = {hasLowStock.Value}";
                if (hasExpiringSoon.HasValue)
                    whereClause += $" AND has_expiring_soon = {hasExpiringSoon.Value}";

                var offset = (page - 1) * pageSize;

                var query = $@"
                    SELECT 
                        s.product_id, s.total_tracked, s.total_untracked, s.total_quantity,
                        s.total_value, s.average_cost, s.last_movement_date, s.last_counted_date,
                        s.has_expiring_soon, s.has_expired, s.has_low_stock, s.needs_attention,
                        s.updated_at,
                        p.drug_name, p.gtin,
                        (SELECT MIN(expiry_date) FROM stock_movements 
                         WHERE product_id = s.product_id AND expiry_date IS NOT NULL) as nearest_expiry_date
                    FROM stock_summary s
                    INNER JOIN products p ON s.product_id = p.id
                    {whereClause}
                    ORDER BY s.updated_at DESC
                    LIMIT @pageSize OFFSET @offset";

                var countQuery = $"SELECT COUNT(*) FROM stock_summary s {whereClause}";

                var summaries = new List<StockSummaryDto>();
                
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", offset);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        summaries.Add(new StockSummaryDto
                        {
                            ProductId = reader.GetGuid(0).ToString(),
                            TotalTracked = reader.GetInt32(1),
                            TotalUntracked = reader.GetInt32(2),
                            TotalQuantity = reader.GetInt32(3),
                            TotalValue = reader.GetDecimal(4),
                            AverageCost = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                            LastMovementDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                            LastCountedDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                            HasExpiringSoon = reader.GetBoolean(8),
                            HasExpired = reader.GetBoolean(9),
                            HasLowStock = reader.GetBoolean(10),
                            NeedsAttention = reader.GetBoolean(11),
                            UpdatedAt = reader.GetDateTime(12),
                            ProductName = reader.GetString(13),
                            Gtin = reader.IsDBNull(14) ? null : reader.GetString(14),
                            NearestExpiryDate = reader.IsDBNull(15) ? null : reader.GetDateTime(15)
                        });
                    }
                }

                int totalCount;
                using (var countCmd = new NpgsqlCommand(countQuery, conn))
                {
                    totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));
                }

                opasLogger.LogDataAccess("system", "StockSummary", "List", new { 
                    TenantId = tenantId, 
                    Count = summaries.Count 
                });

                return Results.Ok(new StockSummaryListResponse
                {
                    Success = true,
                    Summary = summaries,
                    TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockSummary", "ListError", new { 
                    TenantId = tenantId, 
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to fetch stock summary",
                    detail: ex.Message
                );
            }
        }
    }

    private static async Task<IResult> GetStockSummaryByProduct(
        string productId,
        [FromQuery] string tenantId,
        IOpasLogger opasLogger,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        using (OpasLogContext.EnrichFromHttpContext(httpContext))
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Results.BadRequest(new { success = false, message = "tenantId is required" });
            }

            try
            {
                var gln = tenantId.Replace("TNT_", "");
                var tenantConnStr = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";

                using var conn = new NpgsqlConnection(tenantConnStr);
                await conn.OpenAsync(ct);

                var query = @"
                    SELECT 
                        s.product_id, s.total_tracked, s.total_untracked, s.total_quantity,
                        s.total_value, s.average_cost, s.last_movement_date, s.last_counted_date,
                        s.has_expiring_soon, s.has_expired, s.has_low_stock, s.needs_attention,
                        s.updated_at,
                        p.drug_name, p.gtin
                    FROM stock_summary s
                    INNER JOIN products p ON s.product_id = p.id
                    WHERE s.product_id = @product_id";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@product_id", Guid.Parse(productId));

                StockSummaryDto? summary = null;
                
                using (var reader = await cmd.ExecuteReaderAsync(ct))
                {
                    if (await reader.ReadAsync(ct))
                    {
                        summary = new StockSummaryDto
                        {
                            ProductId = reader.GetGuid(0).ToString(),
                            TotalTracked = reader.GetInt32(1),
                            TotalUntracked = reader.GetInt32(2),
                            TotalQuantity = reader.GetInt32(3),
                            TotalValue = reader.GetDecimal(4),
                            AverageCost = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                            LastMovementDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                            LastCountedDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                            HasExpiringSoon = reader.GetBoolean(8),
                            HasExpired = reader.GetBoolean(9),
                            HasLowStock = reader.GetBoolean(10),
                            NeedsAttention = reader.GetBoolean(11),
                            UpdatedAt = reader.GetDateTime(12),
                            ProductName = reader.GetString(13),
                            Gtin = reader.IsDBNull(14) ? null : reader.GetString(14)
                        };
                    }
                }

                if (summary != null)
                {
                    return Results.Ok(new SingleStockSummaryResponse
                    {
                        Success = true,
                        Summary = summary
                    });
                }

                // Ürün var ama summary yok - yeni hesapla
                return await RecalculateAndReturn(productId, tenantId, conn, opasLogger, ct);
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockSummary", "GetByProductError", new { 
                    TenantId = tenantId,
                    ProductId = productId,
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to fetch stock summary",
                    detail: ex.Message
                );
            }
        }
    }

    private static async Task<IResult> RecalculateStockSummary(
        string productId,
        [FromQuery] string tenantId,
        IOpasLogger opasLogger,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        using (OpasLogContext.EnrichFromHttpContext(httpContext))
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Results.BadRequest(new { success = false, message = "tenantId is required" });
            }

            try
            {
                var gln = tenantId.Replace("TNT_", "");
                var tenantConnStr = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";

                using var conn = new NpgsqlConnection(tenantConnStr);
                await conn.OpenAsync(ct);

                return await RecalculateAndReturn(productId, tenantId, conn, opasLogger, ct);
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockSummary", "RecalculateError", new { 
                    TenantId = tenantId,
                    ProductId = productId,
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to recalculate stock summary",
                    detail: ex.Message
                );
            }
        }
    }

    private static async Task<IResult> GetStockAlerts(
        [FromQuery] string tenantId,
        IOpasLogger opasLogger,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        using (OpasLogContext.EnrichFromHttpContext(httpContext))
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Results.BadRequest(new { success = false, message = "tenantId is required" });
            }

            try
            {
                var gln = tenantId.Replace("TNT_", "");
                var tenantConnStr = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";

                using var conn = new NpgsqlConnection(tenantConnStr);
                await conn.OpenAsync(ct);

                var query = @"
                    SELECT 
                        s.product_id, s.total_tracked, s.total_untracked, s.total_quantity,
                        s.total_value, s.average_cost, s.last_movement_date, s.last_counted_date,
                        s.has_expiring_soon, s.has_expired, s.has_low_stock, s.needs_attention,
                        s.updated_at,
                        p.drug_name, p.gtin
                    FROM stock_summary s
                    INNER JOIN products p ON s.product_id = p.id
                    WHERE s.needs_attention = TRUE 
                       OR s.has_low_stock = TRUE 
                       OR s.has_expiring_soon = TRUE
                       OR s.has_expired = TRUE
                    ORDER BY 
                        CASE WHEN s.has_expired THEN 1
                             WHEN s.has_low_stock THEN 2
                             WHEN s.has_expiring_soon THEN 3
                             ELSE 4 END,
                        s.updated_at DESC";

                var alerts = new List<StockSummaryDto>();
                
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        alerts.Add(new StockSummaryDto
                        {
                            ProductId = reader.GetGuid(0).ToString(),
                            TotalTracked = reader.GetInt32(1),
                            TotalUntracked = reader.GetInt32(2),
                            TotalQuantity = reader.GetInt32(3),
                            TotalValue = reader.GetDecimal(4),
                            AverageCost = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                            LastMovementDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                            LastCountedDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                            HasExpiringSoon = reader.GetBoolean(8),
                            HasExpired = reader.GetBoolean(9),
                            HasLowStock = reader.GetBoolean(10),
                            NeedsAttention = reader.GetBoolean(11),
                            UpdatedAt = reader.GetDateTime(12),
                            ProductName = reader.GetString(13),
                            Gtin = reader.IsDBNull(14) ? null : reader.GetString(14)
                        });
                    }
                }

                opasLogger.LogDataAccess("system", "StockSummary", "GetAlerts", new { 
                    TenantId = tenantId, 
                    AlertCount = alerts.Count 
                });

                return Results.Ok(new StockSummaryListResponse
                {
                    Success = true,
                    Summary = alerts,
                    TotalCount = alerts.Count
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockSummary", "GetAlertsError", new { 
                    TenantId = tenantId, 
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to fetch stock alerts",
                    detail: ex.Message
                );
            }
        }
    }

    // Helper method: Stok özetini yeniden hesapla ve döndür
    private static async Task<IResult> RecalculateAndReturn(
        string productId,
        string tenantId,
        NpgsqlConnection conn,
        IOpasLogger opasLogger,
        CancellationToken ct)
    {
        // İlaçlar için: Seri no takipli stok say
        var trackedQuery = @"
            SELECT COUNT(*) 
            FROM stock_items_serial 
            WHERE product_id = @product_id 
            AND status = 'IN_STOCK'
            AND tracking_status = 'TRACKED'";

        var untrackedQuery = @"
            SELECT COUNT(*) 
            FROM stock_items_serial 
            WHERE product_id = @product_id 
            AND status = 'IN_STOCK'
            AND tracking_status = 'UNTRACKED'";

        // OTC için: Batch'lerden toplam miktar
        var batchQuery = @"
            SELECT COALESCE(SUM(quantity), 0)
            FROM stock_batches
            WHERE product_id = @product_id
            AND is_active = TRUE";

        // Toplam değer ve ortalama maliyet
        var costQuery = @"
            SELECT 
                COALESCE(SUM(quantity_change * COALESCE(unit_cost, 0)), 0) as total_value,
                COALESCE(AVG(unit_cost), 0) as avg_cost
            FROM stock_movements
            WHERE product_id = @product_id
            AND unit_cost IS NOT NULL";

        // Son hareket tarihi
        var lastMovementQuery = @"
            SELECT MAX(movement_date)
            FROM stock_movements
            WHERE product_id = @product_id";

        int totalTracked = 0;
        int totalUntracked = 0;
        int totalFromBatches = 0;
        decimal totalValue = 0;
        decimal avgCost = 0;
        DateTime? lastMovement = null;

        var productGuid = Guid.Parse(productId);

        using (var cmd1 = new NpgsqlCommand(trackedQuery, conn))
        {
            cmd1.Parameters.AddWithValue("@product_id", productGuid);
            totalTracked = Convert.ToInt32(await cmd1.ExecuteScalarAsync(ct) ?? 0);
        }

        using (var cmd2 = new NpgsqlCommand(untrackedQuery, conn))
        {
            cmd2.Parameters.AddWithValue("@product_id", productGuid);
            totalUntracked = Convert.ToInt32(await cmd2.ExecuteScalarAsync(ct) ?? 0);
        }

        using (var cmd3 = new NpgsqlCommand(batchQuery, conn))
        {
            cmd3.Parameters.AddWithValue("@product_id", productGuid);
            totalFromBatches = Convert.ToInt32(await cmd3.ExecuteScalarAsync(ct) ?? 0);
        }

        using (var cmd4 = new NpgsqlCommand(costQuery, conn))
        {
            cmd4.Parameters.AddWithValue("@product_id", productGuid);
            using var reader = await cmd4.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                totalValue = reader.GetDecimal(0);
                avgCost = reader.GetDecimal(1);
            }
        }

        using (var cmd5 = new NpgsqlCommand(lastMovementQuery, conn))
        {
            cmd5.Parameters.AddWithValue("@product_id", productGuid);
            var result = await cmd5.ExecuteScalarAsync(ct);
            if (result != null && result != DBNull.Value)
                lastMovement = (DateTime)result;
        }

        var totalQty = totalTracked + totalUntracked + totalFromBatches;

        // Uyarıları kontrol et (basit versiyon - geliştirilecek)
        bool hasLowStock = totalQty < 10; // Örnek: 10'dan az
        bool hasExpiringSoon = false; // TODO: SKT kontrolü eklenecek
        bool hasExpired = false; // TODO: SKT kontrolü eklenecek
        bool needsAttention = hasLowStock || hasExpiringSoon || hasExpired;

        // Upsert summary
        var upsertQuery = @"
            INSERT INTO stock_summary (
                product_id, total_tracked, total_untracked, total_quantity,
                total_value, average_cost, last_movement_date,
                has_expiring_soon, has_expired, has_low_stock, needs_attention,
                updated_at
            ) VALUES (
                @product_id, @total_tracked, @total_untracked, @total_quantity,
                @total_value, @average_cost, @last_movement_date,
                @has_expiring_soon, @has_expired, @has_low_stock, @needs_attention,
                NOW()
            )
            ON CONFLICT (product_id) DO UPDATE SET
                total_tracked = EXCLUDED.total_tracked,
                total_untracked = EXCLUDED.total_untracked,
                total_quantity = EXCLUDED.total_quantity,
                total_value = EXCLUDED.total_value,
                average_cost = EXCLUDED.average_cost,
                last_movement_date = EXCLUDED.last_movement_date,
                has_expiring_soon = EXCLUDED.has_expiring_soon,
                has_expired = EXCLUDED.has_expired,
                has_low_stock = EXCLUDED.has_low_stock,
                needs_attention = EXCLUDED.needs_attention,
                updated_at = NOW()";

        using (var cmd6 = new NpgsqlCommand(upsertQuery, conn))
        {
            cmd6.Parameters.AddWithValue("@product_id", productGuid);
            cmd6.Parameters.AddWithValue("@total_tracked", totalTracked);
            cmd6.Parameters.AddWithValue("@total_untracked", totalUntracked);
            cmd6.Parameters.AddWithValue("@total_quantity", totalQty);
            cmd6.Parameters.AddWithValue("@total_value", totalValue);
            cmd6.Parameters.AddWithValue("@average_cost", avgCost);
            cmd6.Parameters.AddWithValue("@last_movement_date", (object?)lastMovement ?? DBNull.Value);
            cmd6.Parameters.AddWithValue("@has_expiring_soon", hasExpiringSoon);
            cmd6.Parameters.AddWithValue("@has_expired", hasExpired);
            cmd6.Parameters.AddWithValue("@has_low_stock", hasLowStock);
            cmd6.Parameters.AddWithValue("@needs_attention", needsAttention);

            await cmd6.ExecuteNonQueryAsync(ct);
        }

        // Ürün adını al
        string productName = "";
        string? gtin = null;
        var productQuery = "SELECT drug_name, gtin FROM products WHERE id = @product_id";
        using (var cmd7 = new NpgsqlCommand(productQuery, conn))
        {
            cmd7.Parameters.AddWithValue("@product_id", productGuid);
            using var reader = await cmd7.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                productName = reader.GetString(0);
                gtin = reader.IsDBNull(1) ? null : reader.GetString(1);
            }
        }

        var summary = new StockSummaryDto
        {
            ProductId = productId,
            TotalTracked = totalTracked,
            TotalUntracked = totalUntracked,
            TotalQuantity = totalQty,
            TotalValue = totalValue,
            AverageCost = avgCost,
            LastMovementDate = lastMovement,
            HasExpiringSoon = hasExpiringSoon,
            HasExpired = hasExpired,
            HasLowStock = hasLowStock,
            NeedsAttention = needsAttention,
            UpdatedAt = DateTime.UtcNow,
            ProductName = productName,
            Gtin = gtin
        };

        opasLogger.LogDataAccess("system", "StockSummary", "Recalculate", new { 
            TenantId = tenantId,
            ProductId = productId,
            TotalQuantity = totalQty
        });

        return Results.Ok(new SingleStockSummaryResponse
        {
            Success = true,
            Message = "Stock summary recalculated successfully",
            Summary = summary
        });
    }
}

