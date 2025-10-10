using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Domain.Entities;
using Opas.Shared.Logging;
using Opas.Infrastructure.Logging;
using Opas.Shared.MultiTenancy;
using Opas.Shared.Product;
using Npgsql;

namespace Opas.Api.Endpoints;

// DTO for stock_summary JOIN (lightweight)
public class ProductStockDto
{
    public Guid product_id { get; set; }
    public int total_quantity { get; set; }
    public decimal? average_cost { get; set; }
}

/// <summary>
/// Tenant ürün yönetimi API endpoints
/// </summary>
public static class TenantProductEndpoints
{
    public static void MapTenantProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenant/products")
            .WithTags("Tenant Products")
            .WithDescription("Tenant ürün yönetimi");

        // GET /api/tenant/products - Tenant ürün listesi
        group.MapGet("/", async (
            TenantDbContext tenantDb,
            IOpasLogger opasLogger,
            ITenantContextAccessor tenantContextAccessor,
            HttpContext httpContext,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var tenantId = tenantContextAccessor.Current?.TenantId ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                opasLogger.LogSystemEvent("ProductList", $"Product list requested for tenant {tenantId}", new { 
                    TenantId = tenantId,
                    Page = page,
                    PageSize = pageSize,
                    Search = search,
                    IsActive = isActive,
                    IP = clientIP 
                });

                try
                {
                    var query = tenantDb.Products.AsQueryable();

                    // Gelişmiş arama filtresi (fuzzy/partial matching)
                    if (!string.IsNullOrEmpty(search))
                    {
                        // Arama terimini parçalara ayır
                        var searchTerms = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        
                        // Her bir kelime için arama yap (sadece DrugName ve Gtin)
                        foreach (var term in searchTerms)
                        {
                            var searchTerm = term.Trim();
                            query = query.Where(p => 
                                (p.DrugName != null && EF.Functions.ILike(p.DrugName, $"%{searchTerm}%")) ||
                                (p.Gtin != null && EF.Functions.ILike(p.Gtin, $"%{searchTerm}%")));
                        }
                    }

                    // Aktif/pasif filtresi
                    if (isActive.HasValue)
                    {
                        query = query.Where(p => p.IsActive == isActive.Value);
                    }

                    // Toplam sayı
                    var totalCount = await query.CountAsync();

                    // Sayfalama ve sıralama
                    var productsList = await query
                        .OrderBy(p => p.DrugName)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    // STOK BİLGİSİNİ EKLE (Ayrı sorgu ile)
                    var productIds = productsList.Select(p => p.Id).ToList();
                    var stockData = await tenantDb.Database
                        .SqlQuery<ProductStockDto>($@"
                            SELECT product_id, total_quantity, average_cost 
                            FROM stock_summary 
                            WHERE product_id = ANY({productIds})")
                        .ToListAsync();

                    var stockDict = stockData.ToDictionary(s => s.product_id);

                    var products = productsList.Select(p =>
                    {
                        var productIdStr = p.Id;
                        var hasStock = stockDict.ContainsKey(productIdStr);
                        
                        return new
                        {
                            product_id = productIdStr,
                            gtin = p.Gtin,
                            drug_name = p.DrugName,
                            manufacturer_name = p.ManufacturerName,
                            manufacturer_gln = p.ManufacturerGln,
                            price = p.Price,
                            is_active = p.IsActive,
                            last_its_sync_at = p.LastItsSyncAt,
                            category = p.Category,
                            has_datamatrix = p.HasDatamatrix,
                            requires_expiry_tracking = p.RequiresExpiryTracking,
                            is_controlled = p.IsControlled,
                            created_at = p.CreatedAtUtc,
                            updated_at = p.UpdatedAtUtc,
                            stock_quantity = hasStock ? stockDict[productIdStr].total_quantity : 0,
                            unit_cost = hasStock ? stockDict[productIdStr].average_cost : (decimal?)null
                        };
                    }).ToList();

                    return Results.Ok(new
                    {
                        data = products,
                        pagination = new
                        {
                            page,
                            pageSize,
                            totalCount,
                            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                            hasNext = page * pageSize < totalCount,
                            hasPrevious = page > 1
                        },
                        filters = new
                        {
                            search,
                            isActive
                        },
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("ProductList", $"Failed to get product list for tenant {tenantId}: {ex.Message}", new { 
                        TenantId = tenantId,
                        Error = ex.Message,
                        IP = clientIP
                    });

                    return Results.Problem(
                        title: "Product List Failed",
                        detail: $"Failed to retrieve product list: {ex.Message}",
                        statusCode: 500
                    );
                }
            }
        })
        .WithName("GetTenantProducts")
        .WithSummary("Tenant ürün listesi")
        .WithDescription("Belirtilen tenant'ın ürün listesini getirir")
        .Produces<object>(200)
        .Produces<ProblemDetails>(500);

        // GET /api/tenant/products/{id} - Specific product detail
        group.MapGet("/{id:guid}", async (
            Guid id,
            TenantDbContext tenantDb,
            IOpasLogger opasLogger,
            ITenantContextAccessor tenantContextAccessor,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var tenantId = tenantContextAccessor.Current?.TenantId ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    var product = await tenantDb.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (product == null)
                    {
                        return Results.NotFound(new { message = "Product not found" });
                    }

                    opasLogger.LogSystemEvent("ProductDetail", $"Product detail accessed for {product.DrugName}", new { 
                        TenantId = tenantId,
                        ProductId = id,
                        ProductName = product.DrugName,
                        IP = clientIP 
                    });

                    return Results.Ok(new
                    {
                        id = product.Id,
                        gtin = product.Gtin,
                        drugName = product.DrugName,
                        manufacturerName = product.ManufacturerName,
                        manufacturerGln = product.ManufacturerGln,
                        price = product.Price,
                        priceHistory = product.PriceHistory,
                        isActive = product.IsActive,
                        lastItsSyncAt = product.LastItsSyncAt,
                        createdAt = product.CreatedAtUtc,
                        updatedAt = product.UpdatedAtUtc
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("ProductDetail", $"Failed to get product detail: {ex.Message}", new { 
                        TenantId = tenantId,
                        ProductId = id,
                        Error = ex.Message,
                        IP = clientIP
                    });

                    return Results.Problem(
                        title: "Product Detail Failed",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            }
        })
        .WithName("GetTenantProductDetail")
        .WithSummary("Ürün detayı")
        .WithDescription("Belirtilen ürünün detay bilgilerini getirir")
        .Produces<object>(200)
        .Produces<object>(404)
        .Produces<ProblemDetails>(500);

        // PUT /api/tenant/products/{id} - Update product (price, name)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateProductRequest request,
            TenantDbContext tenantDb,
            IOpasLogger opasLogger,
            ITenantContextAccessor tenantContextAccessor,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var tenantId = tenantContextAccessor.Current?.TenantId ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    var product = await tenantDb.Products.FirstOrDefaultAsync(p => p.Id == id);
                    if (product == null)
                    {
                        return Results.NotFound(new { message = "Product not found" });
                    }

                    var oldName = product.DrugName;
                    var oldPrice = product.Price;

                    // Ürün adını güncelle
                    if (!string.IsNullOrEmpty(request.DrugName) && request.DrugName != product.DrugName)
                    {
                        product.DrugName = request.DrugName;
                    }

                    // Fiyat güncelle ve tarihçe ekle
                    if (request.Price.HasValue && request.Price.Value != product.Price)
                    {
                        product.Price = request.Price.Value;
                        // TODO: Price history güncelleme logic buraya gelecek
                    }

                    product.MarkUpdated();
                    await tenantDb.SaveChangesAsync();

                    opasLogger.LogSystemEvent("ProductUpdate", $"Product {product.DrugName} updated", new { 
                        TenantId = tenantId,
                        ProductId = id,
                        ProductName = product.DrugName,
                        OldName = oldName,
                        NewName = product.DrugName,
                        OldPrice = oldPrice,
                        NewPrice = product.Price,
                        IP = clientIP 
                    });

                    return Results.Ok(new
                    {
                        success = true,
                        message = "Product updated successfully",
                        product = new
                        {
                            id = product.Id,
                            drugName = product.DrugName,
                            price = product.Price,
                            updatedAt = product.UpdatedAtUtc
                        }
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("ProductUpdate", $"Failed to update product: {ex.Message}", new { 
                        TenantId = tenantId,
                        ProductId = id,
                        Error = ex.Message,
                        IP = clientIP
                    });

                    return Results.Problem(
                        title: "Product Update Failed",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            }
        })
        .WithName("UpdateTenantProduct")
        .WithSummary("Ürün güncelle")
        .WithDescription("Ürün adı ve fiyatını günceller")
        .Produces<object>(200)
        .Produces<object>(404)
        .Produces<ProblemDetails>(500);

        // POST /api/tenant/products/seed-mock-data - Seed 100 mock products
        group.MapPost("/seed-mock-data", async (
            [AsParameters] TenantRequest tenantReq,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    var connStr = BuildTenantConnectionString(tenantReq.TenantId);
                    await using var conn = new NpgsqlConnection(connStr);
                    await conn.OpenAsync(ct);

                    // Generate 100 mock products
                    var mockProducts = ProductMockData.Generate100Products();
                    
                    Console.WriteLine($"🎯 Generated {mockProducts.Count} mock products");
                    
                    var createdCount = 0;
                    var errors = new List<string>();
                    var skippedCount = 0;

                    foreach (var product in mockProducts)
                    {
                        try
                        {
                            var insertQuery = @"
                                INSERT INTO products (
                                    id, gtin, drug_name, manufacturer_name, manufacturer_gln,
                                    price, category, has_datamatrix, requires_expiry_tracking, is_controlled,
                                    is_active, created_at_utc, created_by
                                ) VALUES (
                                    @id, @gtin, @drugName, @manufacturerName, @manufacturerGln,
                                    @price, @category, @hasDatamatrix, @requiresExpiryTracking, @isControlled,
                                    @isActive, NOW(), @createdBy
                                )
                                ON CONFLICT (gtin) DO NOTHING";

                            await using var cmd = new NpgsqlCommand(insertQuery, conn);
                            cmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                            cmd.Parameters.AddWithValue("@gtin", product.Gtin);
                            cmd.Parameters.AddWithValue("@drugName", product.DrugName);
                            cmd.Parameters.AddWithValue("@manufacturerName", product.ManufacturerName);
                            cmd.Parameters.AddWithValue("@manufacturerGln", product.ManufacturerGln);
                            cmd.Parameters.AddWithValue("@price", product.Price);
                            cmd.Parameters.AddWithValue("@category", product.Category);
                            cmd.Parameters.AddWithValue("@hasDatamatrix", product.HasDatamatrix);
                            cmd.Parameters.AddWithValue("@requiresExpiryTracking", product.RequiresExpiryTracking);
                            cmd.Parameters.AddWithValue("@isControlled", product.IsControlled);
                            cmd.Parameters.AddWithValue("@isActive", product.IsActive);
                            cmd.Parameters.AddWithValue("@createdBy", "mock"); // ✅ Mock data identifier

                            var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
                            if (rowsAffected > 0)
                            {
                                createdCount++;
                                Console.WriteLine($"✅ Created: {product.DrugName}");
                            }
                            else
                            {
                                skippedCount++;
                                Console.WriteLine($"⚠️ Skipped (duplicate GTIN): {product.Gtin} - {product.DrugName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{product.DrugName}: {ex.Message}");
                            Console.WriteLine($"❌ Error: {product.DrugName} - {ex.Message}");
                        }
                    }
                    
                    Console.WriteLine($"📊 Summary - Created: {createdCount}, Skipped: {skippedCount}, Errors: {errors.Count}");

                    opasLogger.LogDataAccess(tenantReq.Username, "Product", "SeedMockData", new { 
                        TenantId = tenantReq.TenantId,
                        Created = createdCount,
                        Errors = errors.Count,
                        Total = mockProducts.Count
                    });

                    return Results.Ok(new { 
                        message = $"Successfully seeded {createdCount} mock products (skipped {skippedCount} duplicates)",
                        created = createdCount,
                        skipped = skippedCount,
                        errors = errors.Count,
                        total = mockProducts.Count,
                        errorDetails = errors
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("ProductSeed", $"Failed to seed mock products: {ex.Message}", new { 
                        TenantId = tenantReq.TenantId,
                        Error = ex.Message
                    });

                    return Results.Problem(
                        title: "Mock Product Seed Failed",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            }
        })
        .WithName("SeedMockProducts")
        .WithSummary("Mock ürün verisi oluştur")
        .WithDescription("100 adet mock ürün ekler (created_by='mock' ile tanınabilir)")
        .Produces<object>(200)
        .Produces<ProblemDetails>(500);
    }

    public record UpdateProductRequest(string? DrugName, decimal? Price);

    private static string BuildTenantConnectionString(string tenantId)
    {
        var gln = tenantId.Replace("TNT_", "");
        return $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";
    }
}
