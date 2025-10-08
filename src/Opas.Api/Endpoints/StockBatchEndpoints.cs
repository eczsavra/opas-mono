using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Opas.Infrastructure.Logging;
using Opas.Shared.Logging;
using Opas.Shared.Stock;

namespace Opas.Api.Endpoints;

public static class StockBatchEndpoints
{
    public static void MapStockBatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenant/stock/batches")
            .WithTags("Stock Batches")
            .WithDescription("OTC ürün parti yönetimi (SKT grupları)");

        // GET: Ürüne göre batch'leri listele
        group.MapGet("/product/{productId}", GetBatchesByProduct)
            .WithName("GetBatchesByProduct")
            .WithDescription("Belirli bir ürünün tüm batch'lerini getirir");

        // POST: Yeni batch oluştur
        group.MapPost("/", CreateBatch)
            .WithName("CreateBatch")
            .WithDescription("Yeni batch (parti) oluşturur");

        // PUT: Batch miktarını güncelle (satış sonrası)
        group.MapPut("/{batchId}/quantity", UpdateBatchQuantity)
            .WithName("UpdateBatchQuantity")
            .WithDescription("Batch miktarını günceller (satış/düzeltme)");

        // GET: Aktif batch'leri listele
        group.MapGet("/active", GetActiveBatches)
            .WithName("GetActiveBatches")
            .WithDescription("Aktif (quantity > 0) batch'leri listeler");

        // GET: Yakın SKT'li batch'ler
        group.MapGet("/expiring-soon", GetExpiringBatches)
            .WithName("GetExpiringBatches")
            .WithDescription("6 ay içinde dolacak batch'leri listeler");
    }

    private static async Task<IResult> GetBatchesByProduct(
        string productId,
        [FromQuery] string tenantId,
        IOpasLogger opasLogger,
        HttpContext httpContext,
        [FromQuery] bool activeOnly = false,
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

                var whereClause = "WHERE product_id = @product_id";
                if (activeOnly)
                    whereClause += " AND is_active = TRUE AND quantity > 0";

                var query = $@"
                    SELECT 
                        id, product_id, batch_number, expiry_date,
                        quantity, initial_quantity, unit_cost, total_cost,
                        location_id, is_active, created_by, created_at, notes
                    FROM stock_batches
                    {whereClause}
                    ORDER BY expiry_date ASC"; // FIFO için önce yakın SKT

                var batches = new List<BatchDto>();
                
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@product_id", Guid.Parse(productId));

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        batches.Add(new BatchDto
                        {
                            Id = reader.GetGuid(0).ToString(),
                            ProductId = reader.GetGuid(1).ToString(),
                            BatchNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ExpiryDate = reader.GetDateTime(3),
                            Quantity = reader.GetInt32(4),
                            InitialQuantity = reader.GetInt32(5),
                            UnitCost = reader.GetDecimal(6),
                            TotalCost = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                            LocationId = reader.IsDBNull(8) ? null : reader.GetGuid(8).ToString(),
                            IsActive = reader.GetBoolean(9),
                            CreatedBy = reader.GetString(10),
                            CreatedAt = reader.GetDateTime(11),
                            Notes = reader.IsDBNull(12) ? null : reader.GetString(12)
                        });
                    }
                }

                opasLogger.LogDataAccess("system", "StockBatches", "GetByProduct", new { 
                    TenantId = tenantId,
                    ProductId = productId,
                    Count = batches.Count 
                });

                return Results.Ok(new BatchListResponse
                {
                    Success = true,
                    Batches = batches,
                    TotalCount = batches.Count
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockBatches", "GetByProductError", new { 
                    TenantId = tenantId,
                    ProductId = productId,
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to fetch batches",
                    detail: ex.Message
                );
            }
        }
    }

    private static async Task<IResult> CreateBatch(
        [FromBody] CreateBatchRequest request,
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

                // Batch numarası: kullanıcı verdiyse kullan, yoksa otomatik üret
                string? batchNumber = request.BatchNumber;
                if (string.IsNullOrEmpty(batchNumber))
                {
                    using (var numCmd = new NpgsqlCommand("SELECT generate_batch_number()", conn))
                    {
                        batchNumber = (await numCmd.ExecuteScalarAsync(ct))?.ToString();
                    }
                }

                // Toplam maliyet
                var totalCost = request.UnitCost * request.Quantity;

                var insertQuery = @"
                    INSERT INTO stock_batches (
                        product_id, batch_number, expiry_date,
                        quantity, initial_quantity, unit_cost, total_cost,
                        location_id, created_by, notes
                    ) VALUES (
                        @product_id, @batch_number, @expiry_date,
                        @quantity, @initial_quantity, @unit_cost, @total_cost,
                        @location_id, @created_by, @notes
                    ) RETURNING id, created_at, is_active";

                Guid newId;
                DateTime createdAt;
                bool isActive;

                using (var cmd = new NpgsqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@product_id", Guid.Parse(request.ProductId));
                    cmd.Parameters.AddWithValue("@batch_number", batchNumber!);
                    cmd.Parameters.AddWithValue("@expiry_date", request.ExpiryDate.Date);
                    cmd.Parameters.AddWithValue("@quantity", request.Quantity);
                    cmd.Parameters.AddWithValue("@initial_quantity", request.Quantity);
                    cmd.Parameters.AddWithValue("@unit_cost", request.UnitCost);
                    cmd.Parameters.AddWithValue("@total_cost", totalCost);
                    cmd.Parameters.AddWithValue("@location_id", string.IsNullOrEmpty(request.LocationId) ? DBNull.Value : Guid.Parse(request.LocationId));
                    cmd.Parameters.AddWithValue("@created_by", username);
                    cmd.Parameters.AddWithValue("@notes", (object?)request.Notes ?? DBNull.Value);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    await reader.ReadAsync(ct);
                    newId = reader.GetGuid(0);
                    createdAt = reader.GetDateTime(1);
                    isActive = reader.GetBoolean(2);
                }

                var batch = new BatchDto
                {
                    Id = newId.ToString(),
                    ProductId = request.ProductId,
                    BatchNumber = batchNumber,
                    ExpiryDate = request.ExpiryDate,
                    Quantity = request.Quantity,
                    InitialQuantity = request.Quantity,
                    UnitCost = request.UnitCost,
                    TotalCost = totalCost,
                    LocationId = request.LocationId,
                    IsActive = isActive,
                    CreatedBy = username,
                    CreatedAt = createdAt,
                    Notes = request.Notes
                };

                opasLogger.LogDataAccess(username, "StockBatches", "Create", new { 
                    TenantId = tenantId,
                    BatchNumber = batchNumber,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                });

                return Results.Ok(new BatchResponse
                {
                    Success = true,
                    Message = "Batch created successfully",
                    Batch = batch
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockBatches", "CreateError", new { 
                    TenantId = tenantId,
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to create batch",
                    detail: ex.Message
                );
            }
        }
    }

    private static async Task<IResult> UpdateBatchQuantity(
        string batchId,
        [FromQuery] int newQuantity,
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

                // Yeni toplam maliyet hesapla
                var updateQuery = @"
                    UPDATE stock_batches
                    SET 
                        quantity = @new_quantity,
                        total_cost = @new_quantity * unit_cost,
                        is_active = CASE WHEN @new_quantity > 0 THEN TRUE ELSE FALSE END
                    WHERE id = @batch_id
                    RETURNING 
                        product_id, batch_number, expiry_date,
                        quantity, initial_quantity, unit_cost, total_cost,
                        location_id, is_active, created_by, created_at, notes";

                using (var cmd = new NpgsqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@batch_id", Guid.Parse(batchId));
                    cmd.Parameters.AddWithValue("@new_quantity", newQuantity);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        var batch = new BatchDto
                        {
                            Id = batchId,
                            ProductId = reader.GetGuid(0).ToString(),
                            BatchNumber = reader.IsDBNull(1) ? null : reader.GetString(1),
                            ExpiryDate = reader.GetDateTime(2),
                            Quantity = reader.GetInt32(3),
                            InitialQuantity = reader.GetInt32(4),
                            UnitCost = reader.GetDecimal(5),
                            TotalCost = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                            LocationId = reader.IsDBNull(7) ? null : reader.GetGuid(7).ToString(),
                            IsActive = reader.GetBoolean(8),
                            CreatedBy = reader.GetString(9),
                            CreatedAt = reader.GetDateTime(10),
                            Notes = reader.IsDBNull(11) ? null : reader.GetString(11)
                        };

                        opasLogger.LogDataAccess(username, "StockBatches", "UpdateQuantity", new { 
                            TenantId = tenantId,
                            BatchId = batchId,
                            NewQuantity = newQuantity
                        });

                        return Results.Ok(new BatchResponse
                        {
                            Success = true,
                            Message = "Batch quantity updated successfully",
                            Batch = batch
                        });
                    }

                    return Results.NotFound(new { success = false, message = "Batch not found" });
                }
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockBatches", "UpdateQuantityError", new { 
                    TenantId = tenantId,
                    BatchId = batchId,
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to update batch quantity",
                    detail: ex.Message
                );
            }
        }
    }

    private static async Task<IResult> GetActiveBatches(
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
                        id, product_id, batch_number, expiry_date,
                        quantity, initial_quantity, unit_cost, total_cost,
                        location_id, is_active, created_by, created_at, notes
                    FROM stock_batches
                    WHERE is_active = TRUE AND quantity > 0
                    ORDER BY expiry_date ASC
                    LIMIT @pageSize OFFSET @offset";

                var countQuery = "SELECT COUNT(*) FROM stock_batches WHERE is_active = TRUE AND quantity > 0";

                var batches = new List<BatchDto>();
                
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", offset);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        batches.Add(new BatchDto
                        {
                            Id = reader.GetGuid(0).ToString(),
                            ProductId = reader.GetGuid(1).ToString(),
                            BatchNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ExpiryDate = reader.GetDateTime(3),
                            Quantity = reader.GetInt32(4),
                            InitialQuantity = reader.GetInt32(5),
                            UnitCost = reader.GetDecimal(6),
                            TotalCost = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                            LocationId = reader.IsDBNull(8) ? null : reader.GetGuid(8).ToString(),
                            IsActive = reader.GetBoolean(9),
                            CreatedBy = reader.GetString(10),
                            CreatedAt = reader.GetDateTime(11),
                            Notes = reader.IsDBNull(12) ? null : reader.GetString(12)
                        });
                    }
                }

                int totalCount;
                using (var countCmd = new NpgsqlCommand(countQuery, conn))
                {
                    totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));
                }

                opasLogger.LogDataAccess("system", "StockBatches", "GetActive", new { 
                    TenantId = tenantId,
                    Count = batches.Count 
                });

                return Results.Ok(new BatchListResponse
                {
                    Success = true,
                    Batches = batches,
                    TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockBatches", "GetActiveError", new { 
                    TenantId = tenantId,
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to fetch active batches",
                    detail: ex.Message
                );
            }
        }
    }

    private static async Task<IResult> GetExpiringBatches(
        [FromQuery] string tenantId,
        IOpasLogger opasLogger,
        HttpContext httpContext,
        [FromQuery] int daysAhead = 180, // 6 ay = 180 gün
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
                        id, product_id, batch_number, expiry_date,
                        quantity, initial_quantity, unit_cost, total_cost,
                        location_id, is_active, created_by, created_at, notes
                    FROM stock_batches
                    WHERE is_active = TRUE 
                    AND quantity > 0
                    AND expiry_date <= CURRENT_DATE + INTERVAL '@days_ahead days'
                    ORDER BY expiry_date ASC";

                var batches = new List<BatchDto>();
                
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@days_ahead", daysAhead);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        batches.Add(new BatchDto
                        {
                            Id = reader.GetGuid(0).ToString(),
                            ProductId = reader.GetGuid(1).ToString(),
                            BatchNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ExpiryDate = reader.GetDateTime(3),
                            Quantity = reader.GetInt32(4),
                            InitialQuantity = reader.GetInt32(5),
                            UnitCost = reader.GetDecimal(6),
                            TotalCost = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                            LocationId = reader.IsDBNull(8) ? null : reader.GetGuid(8).ToString(),
                            IsActive = reader.GetBoolean(9),
                            CreatedBy = reader.GetString(10),
                            CreatedAt = reader.GetDateTime(11),
                            Notes = reader.IsDBNull(12) ? null : reader.GetString(12)
                        });
                    }
                }

                opasLogger.LogDataAccess("system", "StockBatches", "GetExpiring", new { 
                    TenantId = tenantId,
                    DaysAhead = daysAhead,
                    Count = batches.Count 
                });

                return Results.Ok(new BatchListResponse
                {
                    Success = true,
                    Message = $"Batches expiring within {daysAhead} days",
                    Batches = batches,
                    TotalCount = batches.Count
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("StockBatches", "GetExpiringError", new { 
                    TenantId = tenantId,
                    Error = ex.Message 
                });
                return Results.Problem(
                    statusCode: 500,
                    title: "Failed to fetch expiring batches",
                    detail: ex.Message
                );
            }
        }
    }
}

