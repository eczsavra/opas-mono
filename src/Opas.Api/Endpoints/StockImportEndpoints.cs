using Microsoft.AspNetCore.Mvc;
using Opas.Infrastructure.Services;
using Opas.Shared.Logging;
using Opas.Shared.MultiTenancy;
using Opas.Shared.Stock;
using Npgsql;

namespace Opas.Api.Endpoints;

public static class StockImportEndpoints
{
    public static void MapStockImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/opas/tenant/stock/import")
            .WithTags("Stock Import");
            // TODO: Add proper authentication when auth system is configured
            // .RequireAuthorization();

        group.MapPost("/analyze", AnalyzeImportFile)
            .WithName("AnalyzeImportFile")
            .WithDescription("Analyze uploaded file and detect products")
            .DisableAntiforgery();

        group.MapPost("/execute", ExecuteImport)
            .WithName("ExecuteImport")
            .WithDescription("Execute bulk stock import from confirmed rows");
    }

    private static async Task<IResult> AnalyzeImportFile(
        [FromForm] IFormFile file,
        [FromServices] StockImportService importService,
        [FromServices] ITenantProvider tenantProvider,
        [FromServices] IConfiguration config,
        [FromServices] IOpasLogger logger,
        HttpContext httpContext)
    {
        var tenantId = tenantProvider.TenantId;
        var username = httpContext.Request.Headers["X-Username"].FirstOrDefault()
                    ?? httpContext.Request.Cookies["username"]
                    ?? "unknown";

        logger.LogDataAccess(
            username,
            "StockImport",
            "AnalyzeFile",
            new { FileName = file.FileName, FileSize = file.Length, IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() }
        );

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "No file uploaded" });
        }

        // Validate file size (max 10MB)
        if (file.Length > 10 * 1024 * 1024)
        {
            return Results.BadRequest(new { error = "File size exceeds 10MB limit" });
        }

        // Detect file type from extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileType = extension switch
        {
            ".xlsx" or ".xls" => "excel",
            ".csv" => "csv",
            ".tsv" or ".txt" => "tsv",
            _ => null
        };

        if (fileType == null)
        {
            return Results.BadRequest(new { error = $"Unsupported file type: {extension}" });
        }

        try
        {
            // Read file content
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileContent = memoryStream.ToArray();

            // Build tenant connection string
            var baseCs = config["Database:Postgres:ConnectionString"]!;
            var gln = tenantId.StartsWith("TNT_") ? tenantId.Substring(4) : tenantId;
            var tenantDbName = $"opas_tenant_{gln}";
            var tenantCs = baseCs.Replace("Database=opas_public", $"Database={tenantDbName}");

            // Analyze file
            var request = new AnalyzeImportFileRequest
            {
                FileName = file.FileName,
                FileType = fileType,
                FileContent = fileContent
            };

            var result = await importService.AnalyzeFileAsync(request, tenantId, tenantCs);

            logger.LogDataAccess(
                username,
                "StockImport",
                "AnalyzeSuccess",
                new { TotalRows = result.TotalRows, MatchedRows = result.MatchedRows, UnmatchedRows = result.UnmatchedRows, IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() }
            );

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Stock import analysis failed",
                new { IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() }
            );

            return Results.Problem(
                title: "Import Analysis Failed",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    private static async Task<IResult> ExecuteImport(
        [FromBody] ExecuteImportRequest request,
        [FromServices] ITenantProvider tenantProvider,
        [FromServices] IConfiguration config,
        [FromServices] IOpasLogger logger,
        HttpContext httpContext)
    {
        var tenantId = tenantProvider.TenantId;
        var username = httpContext.Request.Headers["X-Username"].FirstOrDefault()
                    ?? httpContext.Request.Cookies["username"]
                    ?? "unknown";

        logger.LogDataAccess(
            username,
            "StockImport",
            "ExecuteImport",
            new { RowCount = request.Rows.Count, IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() }
        );

        // Build tenant connection string
        var baseCs = config["Database:Postgres:ConnectionString"]!;
        var gln = tenantId.StartsWith("TNT_") ? tenantId.Substring(4) : tenantId;
        var tenantDbName = $"opas_tenant_{gln}";
        var tenantCs = baseCs.Replace("Database=opas_public", $"Database={tenantDbName}");

        int successful = 0;
        int failed = 0;
        var errors = new List<ImportError>();

        foreach (var row in request.Rows)
        {
            try
            {
                await InsertStockMovementAsync(row, username, tenantCs);
                successful++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add(new ImportError
                {
                    RowNumber = row.RowNumber,
                    Error = ex.Message
                });
            }
        }

        logger.LogDataAccess(
            username,
            "StockImport",
            "ExecuteComplete",
            new { Successful = successful, Failed = failed, IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() }
        );

        var response = new ExecuteImportResponse
        {
            TotalProcessed = request.Rows.Count,
            Successful = successful,
            Failed = failed,
            Errors = errors.Count > 0 ? errors : null
        };

        return Results.Ok(response);
    }

    private static async Task InsertStockMovementAsync(
        ConfirmedImportRow row,
        string username,
        string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // If this is a new product, create it first
        var productId = row.ProductId;
        if (row.IsNewProduct && row.NewProduct != null)
        {
            productId = await CreateNewProductAsync(conn, row.NewProduct, username);
        }

        // Get next movement number
        var movementNumber = await GenerateMovementNumberAsync(conn);

        // Insert stock movement
        var insertQuery = @"
            INSERT INTO stock_movements (
                movement_number, movement_type, product_id, quantity_change,
                unit_cost, total_cost, bonus_quantity, serial_number, expiry_date,
                notes, created_by, created_at
            ) VALUES (
                @movement_number, 'PURCHASE', @product_id, @quantity,
                @unit_cost, @total_cost, @bonus_quantity, @serial_number, @expiry_date,
                @notes, @created_by, NOW()
            )";

        await using var cmd = new NpgsqlCommand(insertQuery, conn);
        cmd.Parameters.AddWithValue("@movement_number", movementNumber);
        cmd.Parameters.AddWithValue("@product_id", productId);
        cmd.Parameters.AddWithValue("@quantity", row.Quantity);
        cmd.Parameters.AddWithValue("@unit_cost", (object?)row.UnitCost ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@total_cost", 
            row.UnitCost.HasValue ? row.UnitCost.Value * row.Quantity : DBNull.Value);
        cmd.Parameters.AddWithValue("@bonus_quantity", (object?)row.BonusQuantity ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@serial_number", (object?)row.SerialNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@expiry_date", (object?)row.ExpiryDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@notes", (object?)row.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@created_by", username);

        await cmd.ExecuteNonQueryAsync();

        // Update stock_summary
        await UpdateStockSummaryAsync(conn, productId);
    }

    private static async Task<string> CreateNewProductAsync(
        NpgsqlConnection conn,
        NewProductInfo newProduct,
        string username)
    {
        // Generate product ID
        var productId = $"PROD_{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
        
        // If GTIN provided, use it in product_id
        if (!string.IsNullOrWhiteSpace(newProduct.Gtin))
        {
            productId = $"PROD_{newProduct.Gtin}";
        }

        var insertQuery = @"
            INSERT INTO products (
                product_id, drug_name, gtin, manufacturer_name, 
                price, description, category, is_active, 
                created_at, updated_at
            ) VALUES (
                @product_id, @drug_name, @gtin, @manufacturer_name,
                @price, @description, 'OTC', true,
                NOW(), NOW()
            )
            ON CONFLICT (product_id) DO NOTHING";

        await using var cmd = new NpgsqlCommand(insertQuery, conn);
        cmd.Parameters.AddWithValue("@product_id", productId);
        cmd.Parameters.AddWithValue("@drug_name", newProduct.ProductName);
        cmd.Parameters.AddWithValue("@gtin", (object?)newProduct.Gtin ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@manufacturer_name", (object?)newProduct.Manufacturer ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@price", (object?)newProduct.Price ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@description", (object?)newProduct.Description ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
        
        return productId;
    }

    private static async Task<string> GenerateMovementNumberAsync(NpgsqlConnection conn)
    {
        var query = "SELECT generate_stock_movement_number()";
        await using var cmd = new NpgsqlCommand(query, conn);
        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString() ?? "SM000001";
    }

    private static async Task UpdateStockSummaryAsync(NpgsqlConnection conn, string productId)
    {
        var updateQuery = @"
            INSERT INTO stock_summary (
                product_id, total_tracked, total_untracked, total_value, average_cost,
                last_movement_at, updated_at
            )
            SELECT 
                @product_id,
                COALESCE(SUM(CASE WHEN serial_number IS NOT NULL THEN quantity_change ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN serial_number IS NULL THEN quantity_change ELSE 0 END), 0),
                COALESCE(SUM(total_cost), 0),
                CASE 
                    WHEN SUM(quantity_change) > 0 
                    THEN SUM(total_cost) / SUM(quantity_change)
                    ELSE 0
                END,
                MAX(created_at),
                NOW()
            FROM stock_movements
            WHERE product_id = @product_id
            ON CONFLICT (product_id) DO UPDATE SET
                total_tracked = EXCLUDED.total_tracked,
                total_untracked = EXCLUDED.total_untracked,
                total_value = EXCLUDED.total_value,
                average_cost = EXCLUDED.average_cost,
                last_movement_at = EXCLUDED.last_movement_at,
                updated_at = NOW()";

        await using var cmd = new NpgsqlCommand(updateQuery, conn);
        cmd.Parameters.AddWithValue("@product_id", productId);
        await cmd.ExecuteNonQueryAsync();
    }
}

