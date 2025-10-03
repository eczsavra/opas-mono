using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Opas.Api.Endpoints;

public static class TenantProductListEndpoints
{
    public static void MapTenantProductListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenant/products-view")
            .WithTags("Tenant Product View")
            .WithOpenApi();

        group.MapGet("/", GetProductList)
            .WithName("GetProductList")
            .WithSummary("Get product list from tenant database")
            .Produces<List<ProductListItemDto>>()
            .Produces(400)
            .Produces(500);

        group.MapGet("/stats", GetProductStats)
            .WithName("GetProductStats")
            .WithSummary("Get product statistics")
            .Produces<ProductStatsDto>()
            .Produces(400)
            .Produces(500);
    }

    private static async Task<IResult> GetProductList(
        [FromServices] IConfiguration configuration,
        HttpContext httpContext,
        [FromQuery] int page = 0,
        [FromQuery] int limit = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? manufacturer = null,
        [FromQuery] bool? active = null,
        [FromQuery] string? sortBy = "drug_name",
        [FromQuery] string? sortOrder = "asc",
        CancellationToken ct = default)
    {
        try
        {
            // Get tenant ID from header
            var tenantId = httpContext.Request.Headers["x-tenant-id"].FirstOrDefault();
            if (string.IsNullOrEmpty(tenantId))
            {
                return Results.BadRequest("Tenant ID not found in headers");
            }

            // Extract GLN from tenant ID
            var gln = tenantId.Replace("TNT_", "");
            var tenantDbName = $"opas_tenant_{gln}";
            
            // Build tenant-specific connection string
            var baseConnectionString = configuration["Database:Postgres:ConnectionString"];
            var tenantConnectionString = baseConnectionString?.Replace("Database=opas_public", $"Database={tenantDbName}");

            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync(ct);

            // Build WHERE clause
            var whereConditions = new List<string> { "is_deleted = false" };
            var parameters = new List<NpgsqlParameter>();

            if (active.HasValue)
            {
                whereConditions.Add("is_active = @active");
                parameters.Add(new NpgsqlParameter("@active", active.Value));
            }

            if (!string.IsNullOrEmpty(search))
            {
                whereConditions.Add("(LOWER(drug_name) LIKE @search OR LOWER(gtin) LIKE @search OR LOWER(manufacturer_name) LIKE @search)");
                parameters.Add(new NpgsqlParameter("@search", $"%{search.ToLower()}%"));
            }

            if (!string.IsNullOrEmpty(manufacturer))
            {
                whereConditions.Add("LOWER(manufacturer_name) LIKE @manufacturer");
                parameters.Add(new NpgsqlParameter("@manufacturer", $"%{manufacturer.ToLower()}%"));
            }

            // Build ORDER BY clause
            var validSortColumns = new[] { "drug_name", "gtin", "manufacturer_name", "price", "created_at_utc" };
            if (!validSortColumns.Contains(sortBy))
                sortBy = "drug_name";
            
            var orderDirection = sortOrder?.ToLower() == "desc" ? "DESC" : "ASC";

            // Count query
            var countSql = $@"
                SELECT COUNT(*) 
                FROM products 
                WHERE {string.Join(" AND ", whereConditions)}";

            using var countCommand = new NpgsqlCommand(countSql, connection);
            countCommand.Parameters.AddRange(parameters.ToArray());
            var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(ct));

            // Main query
            var sql = $@"
                SELECT 
                    id,
                    gtin,
                    COALESCE(drug_name, '') as drug_name,
                    COALESCE(manufacturer_gln, '') as manufacturer_gln,
                    COALESCE(manufacturer_name, '') as manufacturer_name,
                    COALESCE(price, 0) as price,
                    price_history,
                    is_active,
                    last_its_sync_at,
                    created_at_utc,
                    updated_at_utc
                FROM products 
                WHERE {string.Join(" AND ", whereConditions)}
                ORDER BY {sortBy} {orderDirection}
                LIMIT @limit OFFSET @offset";

            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());
            command.Parameters.Add(new NpgsqlParameter("@limit", limit));
            command.Parameters.Add(new NpgsqlParameter("@offset", page * limit));

            using var reader = await command.ExecuteReaderAsync(ct);

            var products = new List<ProductListItemDto>();
            while (await reader.ReadAsync(ct))
            {
                products.Add(new ProductListItemDto
                {
                    Id = reader.GetGuid(0).ToString(),
                    Gtin = reader.GetString(1),
                    DrugName = reader.GetString(2),
                    ManufacturerGln = reader.GetString(3),
                    ManufacturerName = reader.GetString(4),
                    Price = reader.GetDecimal(5),
                    PriceHistory = reader.IsDBNull(6) ? "[]" : reader.GetString(6),
                    IsActive = reader.GetBoolean(7),
                    LastItsSyncAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    CreatedAt = reader.GetDateTime(9).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    UpdatedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                });
            }

            return Results.Ok(new
            {
                data = products,
                totalCount,
                page,
                limit,
                totalPages = (int)Math.Ceiling((double)totalCount / limit)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Product List Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return Results.Problem(
                detail: $"Error fetching product list: {ex.Message}",
                statusCode: 500
            );
        }
    }

    private static async Task<IResult> GetProductStats(
        [FromServices] IConfiguration configuration,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        try
        {
            var tenantId = httpContext.Request.Headers["x-tenant-id"].FirstOrDefault();
            if (string.IsNullOrEmpty(tenantId))
            {
                return Results.BadRequest("Tenant ID not found in headers");
            }

            var gln = tenantId.Replace("TNT_", "");
            var tenantDbName = $"opas_tenant_{gln}";
            
            var baseConnectionString = configuration["Database:Postgres:ConnectionString"];
            var tenantConnectionString = baseConnectionString?.Replace("Database=opas_public", $"Database={tenantDbName}");

            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync(ct);

            var sql = @"
                SELECT 
                    COUNT(*) as total_products,
                    COUNT(CASE WHEN is_active = true THEN 1 END) as active_products,
                    COUNT(CASE WHEN is_active = false THEN 1 END) as inactive_products,
                    COUNT(DISTINCT manufacturer_name) as total_manufacturers,
                    AVG(price) as average_price,
                    MAX(created_at_utc) as last_added
                FROM products 
                WHERE is_deleted = false";

            using var command = new NpgsqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync(ct);

            if (await reader.ReadAsync(ct))
            {
                var stats = new ProductStatsDto
                {
                    TotalProducts = reader.GetInt32(0),
                    ActiveProducts = reader.GetInt32(1),
                    InactiveProducts = reader.GetInt32(2),
                    TotalManufacturers = reader.GetInt32(3),
                    AveragePrice = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                    LastAdded = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                return Results.Ok(stats);
            }

            return Results.Ok(new ProductStatsDto());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Product Stats Error: {ex.Message}");
            return Results.Problem(
                detail: $"Error fetching product stats: {ex.Message}",
                statusCode: 500
            );
        }
    }
}

// DTOs
public class ProductListItemDto
{
    public string Id { get; set; } = default!;
    public string Gtin { get; set; } = default!;
    public string DrugName { get; set; } = default!;
    public string ManufacturerGln { get; set; } = default!;
    public string ManufacturerName { get; set; } = default!;
    public decimal Price { get; set; }
    public string PriceHistory { get; set; } = default!;
    public bool IsActive { get; set; }
    public string? LastItsSyncAt { get; set; }
    public string CreatedAt { get; set; } = default!;
    public string? UpdatedAt { get; set; }
}

public class ProductStatsDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int InactiveProducts { get; set; }
    public int TotalManufacturers { get; set; }
    public decimal AveragePrice { get; set; }
    public string? LastAdded { get; set; }
}
