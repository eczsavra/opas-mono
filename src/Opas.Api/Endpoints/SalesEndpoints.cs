using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Opas.Infrastructure.Logging;
using Opas.Shared.Logging;
using Opas.Shared.Tenant;

namespace Opas.Api.Endpoints;

public static class SalesEndpoints
{
    public static void MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/opas/tenant/sales")
            .WithTags("Sales");

        group.MapPost("/complete", CompleteSale)
            .WithName("CompleteSale")
            .WithDescription("Satışı tamamla - Draft'tan kesinleşmiş satışa dönüştür");

        group.MapGet("/", GetSales)
            .WithName("GetSales")
            .WithDescription("Satış listesi");

        group.MapGet("/{saleId}", GetSaleDetail)
            .WithName("GetSaleDetail")
            .WithDescription("Satış detayı");
    }

    /// <summary>
    /// Satışı tamamla - Draft'tan kesinleşmiş satışa dönüştür
    /// </summary>
    private static async Task<IResult> CompleteSale(
        [FromBody] CompleteSaleRequest request,
        [FromHeader(Name = "X-TenantId")] string tenantId,
        [FromHeader(Name = "X-Username")] string username,
        [FromServices] IOpasLogger logger)
    {
        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(username))
        {
            return Results.BadRequest(new { error = "TenantId and Username are required" });
        }

        // Validate request
        if (request == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return Results.BadRequest(new { error = "Sale must have at least one item" });
        }

        if (request.Payment == null)
        {
            return Results.BadRequest(new { error = "Payment information is required" });
        }

        try
        {
            var connectionString = BuildTenantConnectionString(tenantId);

            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            // Transaction başlat
            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // 1. SALE OLUŞTUR
                var saleId = $"SALE_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
                var saleNumber = await GenerateSaleNumberAsync(conn, transaction);

                var subtotal = request.Items.Sum(i => i.TotalPrice);
                var discount = request.Items.Sum(i => i.TotalPrice * i.DiscountRate / 100);
                var total = subtotal - discount;

                var insertSaleSql = @"
                    INSERT INTO sales (
                        sale_id, sale_number, sale_date,
                        subtotal_amount, discount_amount, total_amount,
                        payment_method, payment_status, sale_type,
                        customer_id, customer_name, customer_tc, customer_phone,
                        notes, created_by, created_at, updated_at
                    ) VALUES (
                        @sale_id, @sale_number, NOW(),
                        @subtotal, @discount, @total,
                        @payment_method, @payment_status, @sale_type,
                        @customer_id, @customer_name, @customer_tc, @customer_phone,
                        @notes, @created_by, NOW(), NOW()
                    )";

                await using (var cmd = new NpgsqlCommand(insertSaleSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@sale_id", saleId);
                    cmd.Parameters.AddWithValue("@sale_number", saleNumber);
                    cmd.Parameters.AddWithValue("@subtotal", subtotal);
                    cmd.Parameters.AddWithValue("@discount", discount);
                    cmd.Parameters.AddWithValue("@total", total);
                    cmd.Parameters.AddWithValue("@payment_method", request.Payment.Method);
                    cmd.Parameters.AddWithValue("@payment_status", "COMPLETED");
                    cmd.Parameters.AddWithValue("@sale_type", request.SaleType ?? "NORMAL");
                    cmd.Parameters.AddWithValue("@customer_id", (object?)request.Customer?.CustomerId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@customer_name", (object?)request.Customer?.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@customer_tc", (object?)request.Customer?.TcNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@customer_phone", (object?)request.Customer?.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", (object?)request.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@created_by", username);

                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. SALE_ITEMS OLUŞTUR + STOK DÜŞÜM
                foreach (var item in request.Items)
                {
                    // a) Sale item ekle
                    var insertItemSql = @"
                        INSERT INTO sale_items (
                            sale_id, product_id, product_name, product_category,
                            quantity, unit_price, unit_cost, discount_rate, total_price,
                            serial_number, expiry_date, lot_number, gtin,
                            stock_deducted, created_at
                        ) VALUES (
                            @sale_id, @product_id, @product_name, @product_category,
                            @quantity, @unit_price, @unit_cost, @discount_rate, @total_price,
                            @serial_number, @expiry_date, @lot_number, @gtin,
                            true, NOW()
                        )";

                    await using (var cmd = new NpgsqlCommand(insertItemSql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@sale_id", saleId);
                        cmd.Parameters.AddWithValue("@product_id", Guid.Parse(item.ProductId));
                        cmd.Parameters.AddWithValue("@product_name", item.ProductName);
                        cmd.Parameters.AddWithValue("@product_category", (object?)item.ProductCategory ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                        cmd.Parameters.AddWithValue("@unit_price", item.UnitPrice);
                        cmd.Parameters.AddWithValue("@unit_cost", (object?)item.UnitCost ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@discount_rate", item.DiscountRate);
                        cmd.Parameters.AddWithValue("@total_price", item.TotalPrice);
                        cmd.Parameters.AddWithValue("@serial_number", (object?)item.SerialNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@expiry_date", (object?)item.ExpiryDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@lot_number", (object?)item.LotNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@gtin", (object?)item.Gtin ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // b) Stok hareketi oluştur (SALE tipi, eksi miktar)
                    var movementNumber = await GenerateStockMovementNumberAsync(conn, transaction);
                    var insertMovementSql = @"
                        INSERT INTO stock_movements (
                            movement_number, movement_type, product_id, quantity_change,
                            unit_cost, total_cost, serial_number, expiry_date, lot_number,
                            notes, created_by, created_at
                        ) VALUES (
                            @movement_number, 'SALE_RETAIL', @product_id, @quantity_change,
                            @unit_cost, @total_cost, @serial_number, @expiry_date, @lot_number,
                            @notes, @created_by, NOW()
                        )";

                    await using (var cmd = new NpgsqlCommand(insertMovementSql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@movement_number", movementNumber);
                        cmd.Parameters.AddWithValue("@product_id", Guid.Parse(item.ProductId));
                        cmd.Parameters.AddWithValue("@quantity_change", -item.Quantity); // EKSİ (çıkış)
                        cmd.Parameters.AddWithValue("@unit_cost", (object?)item.UnitCost ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@total_cost", 
                            item.UnitCost.HasValue ? item.UnitCost.Value * item.Quantity : DBNull.Value);
                        cmd.Parameters.AddWithValue("@serial_number", (object?)item.SerialNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@expiry_date", (object?)item.ExpiryDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@lot_number", (object?)item.LotNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@notes", $"Satış #{saleNumber}");
                        cmd.Parameters.AddWithValue("@created_by", username);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // c) Stock summary güncelle
                    await UpdateStockSummaryAsync(conn, transaction, Guid.Parse(item.ProductId));
                }

                // 3. DRAFT_SALES TEMİZLE (Tab'ı sil)
                var deleteDraftSql = "DELETE FROM draft_sales WHERE tab_id = @tab_id";
                await using (var cmd = new NpgsqlCommand(deleteDraftSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@tab_id", request.TabId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4. TRANSACTION COMMIT
                await transaction.CommitAsync();

                // 5. LOG
                logger.LogDataAccess("CompleteSale", "Sale completed successfully", $"SaleId: {saleId}, Amount: {total}");

                // 6. RESPONSE
                return Results.Ok(new CompleteSaleResponse
                {
                    SaleId = saleId,
                    SaleNumber = saleNumber,
                    SaleDate = DateTime.UtcNow,
                    TotalAmount = total,
                    PaymentMethod = request.Payment.Method,
                    ItemCount = request.Items.Count,
                    StockDeducted = true,
                    FiscalReceiptNumber = null // İleride YN ÖKC'den gelecek
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to complete sale for TabId: {request.TabId}");

            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Satış tamamlanamadı"
            );
        }
    }

    /// <summary>
    /// Satış listesi
    /// </summary>
    private static async Task<IResult> GetSales(
        [FromHeader(Name = "X-TenantId")] string tenantId,
        [FromHeader(Name = "X-Username")] string username,
        int page = 1,
        int pageSize = 20,
        string? startDate = null,
        string? endDate = null)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return Results.BadRequest(new { error = "TenantId is required" });
        }

        try
        {
            var connectionString = BuildTenantConnectionString(tenantId);

            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            var whereClauses = new List<string> { "s.is_deleted = false" };
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(startDate))
            {
                whereClauses.Add("s.sale_date >= @start_date");
                parameters["@start_date"] = DateTime.Parse(startDate);
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                whereClauses.Add("s.sale_date <= @end_date");
                parameters["@end_date"] = DateTime.Parse(endDate);
            }

            var whereClause = string.Join(" AND ", whereClauses);
            var offset = (page - 1) * pageSize;

            var query = $@"
                SELECT 
                    s.*,
                    COUNT(si.id) as item_count
                FROM sales s
                LEFT JOIN sale_items si ON s.sale_id = si.sale_id
                WHERE {whereClause}
                GROUP BY s.sale_id
                ORDER BY s.sale_date DESC
                LIMIT @page_size OFFSET @offset";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@page_size", pageSize);
            cmd.Parameters.AddWithValue("@offset", offset);

            foreach (var (key, value) in parameters)
            {
                cmd.Parameters.AddWithValue(key, value);
            }

            var sales = new List<SaleDto>();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                sales.Add(new SaleDto
                {
                    SaleId = reader.GetString(reader.GetOrdinal("sale_id")),
                    SaleNumber = reader.GetString(reader.GetOrdinal("sale_number")),
                    SaleDate = reader.GetDateTime(reader.GetOrdinal("sale_date")),
                    SubtotalAmount = reader.GetDecimal(reader.GetOrdinal("subtotal_amount")),
                    DiscountAmount = reader.GetDecimal(reader.GetOrdinal("discount_amount")),
                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
                    PaymentMethod = reader.GetString(reader.GetOrdinal("payment_method")),
                    PaymentStatus = reader.GetString(reader.GetOrdinal("payment_status")),
                    SaleType = reader.GetString(reader.GetOrdinal("sale_type")),
                    CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name")) 
                        ? null : reader.GetString(reader.GetOrdinal("customer_name")),
                    CustomerPhone = reader.IsDBNull(reader.GetOrdinal("customer_phone")) 
                        ? null : reader.GetString(reader.GetOrdinal("customer_phone")),
                    CreatedBy = reader.GetString(reader.GetOrdinal("created_by")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    ItemCount = reader.GetInt32(reader.GetOrdinal("item_count")),
                    FiscalReceiptNumber = reader.IsDBNull(reader.GetOrdinal("fiscal_receipt_number")) 
                        ? null : reader.GetString(reader.GetOrdinal("fiscal_receipt_number")),
                    FiscalStatus = reader.IsDBNull(reader.GetOrdinal("fiscal_status")) 
                        ? null : reader.GetString(reader.GetOrdinal("fiscal_status"))
                });
            }

            return Results.Ok(sales);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Satış detayı
    /// </summary>
    private static async Task<IResult> GetSaleDetail(
        string saleId,
        [FromHeader(Name = "X-TenantId")] string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return Results.BadRequest(new { error = "TenantId is required" });
        }

        try
        {
            var connectionString = BuildTenantConnectionString(tenantId);

            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            // Sale bilgisi
            var saleQuery = "SELECT * FROM sales WHERE sale_id = @sale_id";
            await using var saleCmd = new NpgsqlCommand(saleQuery, conn);
            saleCmd.Parameters.AddWithValue("@sale_id", saleId);

            SaleDto? sale = null;
            await using (var reader = await saleCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    sale = new SaleDto
                    {
                        SaleId = reader.GetString(reader.GetOrdinal("sale_id")),
                        SaleNumber = reader.GetString(reader.GetOrdinal("sale_number")),
                        SaleDate = reader.GetDateTime(reader.GetOrdinal("sale_date")),
                        SubtotalAmount = reader.GetDecimal(reader.GetOrdinal("subtotal_amount")),
                        DiscountAmount = reader.GetDecimal(reader.GetOrdinal("discount_amount")),
                        TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
                        PaymentMethod = reader.GetString(reader.GetOrdinal("payment_method")),
                        PaymentStatus = reader.GetString(reader.GetOrdinal("payment_status")),
                        SaleType = reader.GetString(reader.GetOrdinal("sale_type")),
                        CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name")) 
                            ? null : reader.GetString(reader.GetOrdinal("customer_name")),
                        CustomerPhone = reader.IsDBNull(reader.GetOrdinal("customer_phone")) 
                            ? null : reader.GetString(reader.GetOrdinal("customer_phone")),
                        CreatedBy = reader.GetString(reader.GetOrdinal("created_by")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                        ItemCount = 0,
                        FiscalReceiptNumber = reader.IsDBNull(reader.GetOrdinal("fiscal_receipt_number")) 
                            ? null : reader.GetString(reader.GetOrdinal("fiscal_receipt_number")),
                        FiscalStatus = reader.IsDBNull(reader.GetOrdinal("fiscal_status")) 
                            ? null : reader.GetString(reader.GetOrdinal("fiscal_status"))
                    };
                }
            }

            if (sale == null)
            {
                return Results.NotFound(new { error = "Sale not found" });
            }

            // Sale items
            var itemsQuery = "SELECT * FROM sale_items WHERE sale_id = @sale_id ORDER BY id";
            await using var itemsCmd = new NpgsqlCommand(itemsQuery, conn);
            itemsCmd.Parameters.AddWithValue("@sale_id", saleId);

            var items = new List<SaleItemDetailDto>();
            await using (var reader = await itemsCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    items.Add(new SaleItemDetailDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        ProductId = reader.GetString(reader.GetOrdinal("product_id")),
                        ProductName = reader.GetString(reader.GetOrdinal("product_name")),
                        ProductCategory = reader.IsDBNull(reader.GetOrdinal("product_category")) 
                            ? null : reader.GetString(reader.GetOrdinal("product_category")),
                        Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                        UnitPrice = reader.GetDecimal(reader.GetOrdinal("unit_price")),
                        UnitCost = reader.IsDBNull(reader.GetOrdinal("unit_cost")) 
                            ? null : reader.GetDecimal(reader.GetOrdinal("unit_cost")),
                        TotalPrice = reader.GetDecimal(reader.GetOrdinal("total_price")),
                        SerialNumber = reader.IsDBNull(reader.GetOrdinal("serial_number")) 
                            ? null : reader.GetString(reader.GetOrdinal("serial_number")),
                        ExpiryDate = reader.IsDBNull(reader.GetOrdinal("expiry_date")) 
                            ? null : reader.GetDateTime(reader.GetOrdinal("expiry_date")),
                        LotNumber = reader.IsDBNull(reader.GetOrdinal("lot_number")) 
                            ? null : reader.GetString(reader.GetOrdinal("lot_number")),
                        Gtin = reader.IsDBNull(reader.GetOrdinal("gtin")) 
                            ? null : reader.GetString(reader.GetOrdinal("gtin")),
                        StockDeducted = reader.GetBoolean(reader.GetOrdinal("stock_deducted"))
                    });
                }
            }

            return Results.Ok(new SaleDetailDto
            {
                Sale = sale with { ItemCount = items.Count },
                Items = items
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    // ========================================
    // HELPER METHODS
    // ========================================

    private static async Task<string> GenerateSaleNumberAsync(NpgsqlConnection conn, NpgsqlTransaction transaction)
    {
        var query = "SELECT generate_sale_number()";
        await using var cmd = new NpgsqlCommand(query, conn, transaction);
        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString() ?? $"SL-{DateTime.UtcNow:yyyyMMdd}-001";
    }

    private static async Task<string> GenerateStockMovementNumberAsync(NpgsqlConnection conn, NpgsqlTransaction transaction)
    {
        var query = "SELECT generate_stock_movement_number()";
        await using var cmd = new NpgsqlCommand(query, conn, transaction);
        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString() ?? "SM000001";
    }

    private static async Task UpdateStockSummaryAsync(NpgsqlConnection conn, NpgsqlTransaction transaction, Guid productId)
    {
        // Basitleştirilmiş - sadece temel bilgileri güncelle
        var updateQuery = @"
            INSERT INTO stock_summary (
                product_id, 
                total_quantity, 
                total_value, 
                average_cost,
                last_movement_date,
                updated_at
            )
            SELECT 
                @product_id::uuid,
                COALESCE(SUM(quantity_change), 0)::int,
                COALESCE(SUM(total_cost), 0)::decimal,
                CASE 
                    WHEN COALESCE(SUM(quantity_change), 0) > 0 
                    THEN COALESCE(SUM(total_cost), 0) / COALESCE(SUM(quantity_change), 1)
                    ELSE 0
                END::decimal,
                MAX(created_at),
                NOW()
            FROM stock_movements
            WHERE product_id = @product_id::uuid
            ON CONFLICT (product_id) DO UPDATE SET
                total_quantity = EXCLUDED.total_quantity,
                total_value = EXCLUDED.total_value,
                average_cost = EXCLUDED.average_cost,
                last_movement_date = EXCLUDED.last_movement_date,
                has_low_stock = CASE WHEN EXCLUDED.total_quantity < 10 THEN TRUE ELSE FALSE END,
                needs_attention = CASE WHEN EXCLUDED.total_quantity < 10 THEN TRUE ELSE FALSE END,
                updated_at = NOW()";

        await using var cmd = new NpgsqlCommand(updateQuery, conn, transaction);
        cmd.Parameters.AddWithValue("@product_id", productId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static string BuildTenantConnectionString(string tenantId)
    {
        var gln = tenantId.Replace("TNT_", "");
        return $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";
    }
}
