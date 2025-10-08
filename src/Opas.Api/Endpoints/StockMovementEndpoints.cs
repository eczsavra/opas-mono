using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Opas.Infrastructure.Logging;
using Opas.Shared.Logging;
using Opas.Shared.Stock;

namespace Opas.Api.Endpoints;

public static class StockMovementEndpoints
{
    public static void MapStockMovementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenant/stock/movements")
            .WithTags("Stock Movements")
            .WithDescription("Stok hareketleri yönetimi");

        // GET: Stok hareketlerini listele
        group.MapGet("/", GetStockMovements)
            .WithName("GetStockMovements")
            .WithDescription("Tenant'ın stok hareketlerini listeler");

        // GET: Tek bir stok hareketi detayı
        group.MapGet("/{id}", GetStockMovementById)
            .WithName("GetStockMovementById")
            .WithDescription("Belirli bir stok hareketinin detaylarını getirir");

        // POST: Yeni stok hareketi oluştur
        group.MapPost("/", CreateStockMovement)
            .WithName("CreateStockMovement")
            .WithDescription("Yeni stok hareketi kaydı oluşturur");

        // GET: Ürüne göre stok hareketleri
        group.MapGet("/product/{productId}", GetMovementsByProduct)
            .WithName("GetMovementsByProduct")
            .WithDescription("Belirli bir ürünün tüm stok hareketlerini getirir");
    }

    private static async Task<IResult> GetStockMovements(
        [FromQuery] string tenantId,
        IOpasLogger opasLogger,
        HttpContext httpContext,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? movementType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
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
                if (!string.IsNullOrEmpty(movementType))
                    whereClause += $" AND movement_type = @movementType";
                if (startDate.HasValue)
                    whereClause += " AND movement_date >= @startDate";
                if (endDate.HasValue)
                    whereClause += " AND movement_date <= @endDate";

                var offset = (page - 1) * pageSize;

                var query = $@"
                    SELECT 
                        id, movement_number, movement_date, movement_type,
                        product_id, quantity_change, unit_cost, total_cost,
                        serial_number, lot_number, expiry_date, gtin,
                        batch_id, reference_type, reference_id,
                        location_id, created_by, created_at,
                        is_correction, correction_reason, notes
                    FROM stock_movements
                    {whereClause}
                    ORDER BY movement_date DESC
                    LIMIT @pageSize OFFSET @offset";

                var countQuery = $"SELECT COUNT(*) FROM stock_movements {whereClause}";

                var movements = new List<StockMovementDto>();
                
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", offset);
                    if (!string.IsNullOrEmpty(movementType))
                        cmd.Parameters.AddWithValue("@movementType", movementType);
                    if (startDate.HasValue)
                        cmd.Parameters.AddWithValue("@startDate", startDate.Value);
                    if (endDate.HasValue)
                        cmd.Parameters.AddWithValue("@endDate", endDate.Value);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        movements.Add(new StockMovementDto
                        {
                            Id = reader.GetGuid(0).ToString(),
                            MovementNumber = reader.GetString(1),
                            MovementDate = reader.GetDateTime(2),
                            MovementType = reader.GetString(3),
                            ProductId = reader.GetGuid(4).ToString(),
                            QuantityChange = reader.GetInt32(5),
                            UnitCost = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                            TotalCost = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                            SerialNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                            LotNumber = reader.IsDBNull(9) ? null : reader.GetString(9),
                            ExpiryDate = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                            Gtin = reader.IsDBNull(11) ? null : reader.GetString(11),
                            BatchId = reader.IsDBNull(12) ? null : reader.GetGuid(12).ToString(),
                            ReferenceType = reader.IsDBNull(13) ? null : reader.GetString(13),
                            ReferenceId = reader.IsDBNull(14) ? null : reader.GetString(14),
                            LocationId = reader.IsDBNull(15) ? null : reader.GetGuid(15).ToString(),
                            CreatedBy = reader.GetString(16),
                            CreatedAt = reader.GetDateTime(17),
                            IsCorrection = reader.GetBoolean(18),
                            CorrectionReason = reader.IsDBNull(19) ? null : reader.GetString(19),
                            Notes = reader.IsDBNull(20) ? null : reader.GetString(20)
                        });
                    }
                }

                int totalCount;
                using (var countCmd = new NpgsqlCommand(countQuery, conn))
                {
                    if (!string.IsNullOrEmpty(movementType))
                        countCmd.Parameters.AddWithValue("@movementType", movementType);
                    if (startDate.HasValue)
                        countCmd.Parameters.AddWithValue("@startDate", startDate.Value);
                    if (endDate.HasValue)
                        countCmd.Parameters.AddWithValue("@endDate", endDate.Value);

                    totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));
                }

                opasLogger.LogDataAccess("system", "StockMovements", "List", new { 
                    TenantId = tenantId, 
                    Page = page,
                    Count = movements.Count,
                    TotalCount = totalCount
                });

                return Results.Ok(new StockMovementsResponse
                {
                    Success = true,
                    Movements = movements,
                    TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockMovements", "ListError", new { 
                    TenantId = tenantId, 
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to fetch stock movements",
                    detail: ex.Message
                );
            }
        }
    }

    private static async Task<IResult> GetStockMovementById(
        string id,
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
                        id, movement_number, movement_date, movement_type,
                        product_id, quantity_change, unit_cost, total_cost,
                        serial_number, lot_number, expiry_date, gtin,
                        batch_id, reference_type, reference_id,
                        location_id, created_by, created_at,
                        is_correction, correction_reason, notes
                    FROM stock_movements
                    WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", Guid.Parse(id));

                using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    var movement = new StockMovementDto
                    {
                        Id = reader.GetGuid(0).ToString(),
                        MovementNumber = reader.GetString(1),
                        MovementDate = reader.GetDateTime(2),
                        MovementType = reader.GetString(3),
                        ProductId = reader.GetGuid(4).ToString(),
                        QuantityChange = reader.GetInt32(5),
                        UnitCost = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                        TotalCost = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                        SerialNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                        LotNumber = reader.IsDBNull(9) ? null : reader.GetString(9),
                        ExpiryDate = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                        Gtin = reader.IsDBNull(11) ? null : reader.GetString(11),
                        BatchId = reader.IsDBNull(12) ? null : reader.GetGuid(12).ToString(),
                        ReferenceType = reader.IsDBNull(13) ? null : reader.GetString(13),
                        ReferenceId = reader.IsDBNull(14) ? null : reader.GetString(14),
                        LocationId = reader.IsDBNull(15) ? null : reader.GetGuid(15).ToString(),
                        CreatedBy = reader.GetString(16),
                        CreatedAt = reader.GetDateTime(17),
                        IsCorrection = reader.GetBoolean(18),
                        CorrectionReason = reader.IsDBNull(19) ? null : reader.GetString(19),
                        Notes = reader.IsDBNull(20) ? null : reader.GetString(20)
                    };

                    return Results.Ok(new StockMovementResponse
                    {
                        Success = true,
                        Movement = movement
                    });
                }

                return Results.NotFound(new { success = false, message = "Movement not found" });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockMovements", "GetByIdError", new { 
                    TenantId = tenantId, 
                    Id = id,
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to fetch stock movement",
                    detail: ex.Message
                );
            }
        }
    }

    private static async Task<IResult> CreateStockMovement(
        [FromBody] CreateStockMovementRequest request,
        [FromQuery] string tenantId,
        [FromQuery] string username,
        IOpasLogger opasLogger,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        using (OpasLogContext.EnrichFromHttpContext(httpContext))
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(username))
            {
                return Results.BadRequest(new { success = false, message = "tenantId and username are required" });
            }

            try
            {
                var gln = tenantId.Replace("TNT_", "");
                var tenantConnStr = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";

                using var conn = new NpgsqlConnection(tenantConnStr);
                await conn.OpenAsync(ct);

                // Otomatik hareket numarası üret
                string? movementNumber;
                using (var numCmd = new NpgsqlCommand("SELECT generate_stock_movement_number()", conn))
                {
                    movementNumber = (await numCmd.ExecuteScalarAsync(ct))?.ToString();
                }

                // Toplam maliyeti hesapla
                decimal? totalCost = null;
                if (request.UnitCost.HasValue)
                {
                    totalCost = request.UnitCost.Value * Math.Abs(request.QuantityChange);
                }

                var insertQuery = @"
                    INSERT INTO stock_movements (
                        movement_number, movement_type, product_id, quantity_change,
                        unit_cost, total_cost, serial_number, lot_number, expiry_date, gtin,
                        batch_id, reference_type, reference_id, location_id,
                        created_by, notes, is_correction, correction_reason
                    ) VALUES (
                        @movement_number, @movement_type, @product_id, @quantity_change,
                        @unit_cost, @total_cost, @serial_number, @lot_number, @expiry_date, @gtin,
                        @batch_id, @reference_type, @reference_id, @location_id,
                        @created_by, @notes, @is_correction, @correction_reason
                    ) RETURNING id, movement_date, created_at";

                Guid newId;
                DateTime movementDate;
                DateTime createdAt;

                using (var cmd = new NpgsqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@movement_number", movementNumber!);
                    cmd.Parameters.AddWithValue("@movement_type", request.MovementType);
                    cmd.Parameters.AddWithValue("@product_id", Guid.Parse(request.ProductId));
                    cmd.Parameters.AddWithValue("@quantity_change", request.QuantityChange);
                    cmd.Parameters.AddWithValue("@unit_cost", (object?)request.UnitCost ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@total_cost", (object?)totalCost ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@serial_number", (object?)request.SerialNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@lot_number", (object?)request.LotNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@expiry_date", (object?)request.ExpiryDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@gtin", (object?)request.Gtin ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@batch_id", string.IsNullOrEmpty(request.BatchId) ? DBNull.Value : Guid.Parse(request.BatchId));
                    cmd.Parameters.AddWithValue("@reference_type", (object?)request.ReferenceType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@reference_id", (object?)request.ReferenceId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@location_id", string.IsNullOrEmpty(request.LocationId) ? DBNull.Value : Guid.Parse(request.LocationId));
                    cmd.Parameters.AddWithValue("@created_by", username);
                    cmd.Parameters.AddWithValue("@notes", (object?)request.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@is_correction", request.IsCorrection);
                    cmd.Parameters.AddWithValue("@correction_reason", (object?)request.CorrectionReason ?? DBNull.Value);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    await reader.ReadAsync(ct);
                    newId = reader.GetGuid(0);
                    movementDate = reader.GetDateTime(1);
                    createdAt = reader.GetDateTime(2);
                }

                var movement = new StockMovementDto
                {
                    Id = newId.ToString(),
                    MovementNumber = movementNumber,
                    MovementDate = movementDate,
                    MovementType = request.MovementType,
                    ProductId = request.ProductId,
                    QuantityChange = request.QuantityChange,
                    UnitCost = request.UnitCost,
                    TotalCost = totalCost,
                    SerialNumber = request.SerialNumber,
                    LotNumber = request.LotNumber,
                    ExpiryDate = request.ExpiryDate,
                    Gtin = request.Gtin,
                    BatchId = request.BatchId,
                    ReferenceType = request.ReferenceType,
                    ReferenceId = request.ReferenceId,
                    LocationId = request.LocationId,
                    CreatedBy = username,
                    CreatedAt = createdAt,
                    Notes = request.Notes
                };

                // Update stock_summary with average cost calculation
                var updateSummaryQuery = @"
                    INSERT INTO stock_summary (
                        product_id, 
                        total_quantity, 
                        total_value, 
                        average_cost, 
                        last_movement_date
                    )
                    SELECT 
                        @product_id,
                        SUM(quantity_change) as total_qty,
                        SUM(total_cost) as total_val,
                        CASE 
                            WHEN SUM(quantity_change) > 0 
                            THEN SUM(total_cost) / SUM(quantity_change)
                            ELSE NULL
                        END as avg_cost,
                        MAX(created_at)
                    FROM stock_movements
                    WHERE product_id = @product_id
                    ON CONFLICT (product_id)
                    DO UPDATE SET
                        total_quantity = EXCLUDED.total_quantity,
                        total_value = EXCLUDED.total_value,
                        average_cost = EXCLUDED.average_cost,
                        last_movement_date = EXCLUDED.last_movement_date,
                        updated_at = NOW()";

                using (var updateCmd = new NpgsqlCommand(updateSummaryQuery, conn))
                {
                    updateCmd.Parameters.AddWithValue("@product_id", Guid.Parse(request.ProductId));
                    await updateCmd.ExecuteNonQueryAsync(ct);
                }

                opasLogger.LogDataAccess(username, "StockMovements", "Create", new { 
                    TenantId = tenantId,
                    MovementNumber = movementNumber,
                    MovementType = request.MovementType,
                    ProductId = request.ProductId,
                    QuantityChange = request.QuantityChange
                });

                return Results.Ok(new StockMovementResponse
                {
                    Success = true,
                    Message = "Stock movement created successfully",
                    Movement = movement
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockMovements", "CreateError", new { 
                    TenantId = tenantId,
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to create stock movement",
                    detail: ex.Message
                );
            }
        }
    }

    private static async Task<IResult> GetMovementsByProduct(
        string productId,
        [FromQuery] string tenantId,
        IOpasLogger opasLogger,
        HttpContext httpContext,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
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

                var offset = (page - 1) * pageSize;

                var query = @"
                    SELECT 
                        id, movement_number, movement_date, movement_type,
                        product_id, quantity_change, unit_cost, total_cost,
                        serial_number, lot_number, expiry_date, gtin,
                        batch_id, reference_type, reference_id,
                        location_id, created_by, created_at,
                        is_correction, correction_reason, notes
                    FROM stock_movements
                    WHERE product_id = @product_id
                    ORDER BY movement_date DESC
                    LIMIT @pageSize OFFSET @offset";

                var countQuery = "SELECT COUNT(*) FROM stock_movements WHERE product_id = @product_id";

                var movements = new List<StockMovementDto>();
                
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@product_id", Guid.Parse(productId));
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", offset);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        movements.Add(new StockMovementDto
                        {
                            Id = reader.GetGuid(0).ToString(),
                            MovementNumber = reader.GetString(1),
                            MovementDate = reader.GetDateTime(2),
                            MovementType = reader.GetString(3),
                            ProductId = reader.GetGuid(4).ToString(),
                            QuantityChange = reader.GetInt32(5),
                            UnitCost = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                            TotalCost = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                            SerialNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                            LotNumber = reader.IsDBNull(9) ? null : reader.GetString(9),
                            ExpiryDate = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                            Gtin = reader.IsDBNull(11) ? null : reader.GetString(11),
                            BatchId = reader.IsDBNull(12) ? null : reader.GetGuid(12).ToString(),
                            ReferenceType = reader.IsDBNull(13) ? null : reader.GetString(13),
                            ReferenceId = reader.IsDBNull(14) ? null : reader.GetString(14),
                            LocationId = reader.IsDBNull(15) ? null : reader.GetGuid(15).ToString(),
                            CreatedBy = reader.GetString(16),
                            CreatedAt = reader.GetDateTime(17),
                            IsCorrection = reader.GetBoolean(18),
                            CorrectionReason = reader.IsDBNull(19) ? null : reader.GetString(19),
                            Notes = reader.IsDBNull(20) ? null : reader.GetString(20)
                        });
                    }
                }

                int totalCount;
                using (var countCmd = new NpgsqlCommand(countQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@product_id", Guid.Parse(productId));
                    totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));
                }

                return Results.Ok(new StockMovementsResponse
                {
                    Success = true,
                    Movements = movements,
                    TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockMovements", "GetByProductError", new { 
                    TenantId = tenantId,
                    ProductId = productId,
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to fetch product movements",
                    detail: ex.Message
                );
            }
        }
    }
}

