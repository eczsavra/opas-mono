using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Domain.Entities;
using Opas.Shared.Logging;
using Opas.Infrastructure.Logging;
using Opas.Shared.MultiTenancy;

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
    }

    public record UpdateProductRequest(string? DrugName, decimal? Price);
}
