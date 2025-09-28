using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Services;
using Opas.Shared.Logging;
using Opas.Infrastructure.Logging;
using Opas.Infrastructure.Persistence;

namespace Opas.Api.Endpoints;

public static class CentralProductEndpoints
{
    public static void MapCentralProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/central/products")
            .WithTags("Central Products")
            .WithDescription("Merkezi DB ürün yönetimi - ITS'den beslenme");

        Console.WriteLine("🔧 CentralProductEndpoints mapped to /api/central/products");

        // POST /api/central/products/sync-from-its - ITS'den merkezi DB'ye sync
        group.MapPost("/sync-from-its", async (
            CentralProductSyncService syncService,
            IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            opasLogger.LogSystemEvent("CentralProductSyncRequest", "Manual sync from ITS requested", new { 
                ClientIP = clientIP
            });

            try
            {
                var syncedCount = await syncService.SyncProductsFromItsAsync();
                
                opasLogger.LogSystemEvent("CentralProductSyncRequest", "Completed successfully", new { 
                    ClientIP = clientIP,
                    SyncedCount = syncedCount
                });

                return Results.Ok(new
                {
                    success = true,
                    syncedCount,
                    message = $"{syncedCount} ürün merkezi DB'ye sync edildi"
                });
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("CentralProductSyncRequest", $"Failed: {ex.Message}", new { 
                    ClientIP = clientIP,
                    Error = ex.Message
                });

                return Results.Problem(
                    title: "Central Product Sync Error",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("SyncProductsFromIts")
        .WithDescription("ITS'den ürünleri çeker ve merkezi DB'ye sync eder");

        // GET /api/central/products - Merkezi DB ürünleri listele
        Console.WriteLine("🔧 Mapping GET / endpoint for central products");
        group.MapGet("/", async (
            ControlPlaneDbContext dbContext,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? letterFilter = null, // Yeni: harf filtresi
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 50,
            HttpContext httpContext = default!
        ) =>
        {
            try
            {
                var query = dbContext.CentralProducts.AsQueryable();

                // Search filter - Case insensitive search
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(p => (p.DrugName != null && p.DrugName.ToLower().Contains(searchLower)) || 
                                           (p.Gtin != null && p.Gtin.ToLower().Contains(searchLower)) ||
                                           (p.ManufacturerName != null && p.ManufacturerName.ToLower().Contains(searchLower)));
                }

                // Active/Inactive filter
                if (isActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == isActive.Value);
                }

                // Letter filter - Harf filtresi
                if (!string.IsNullOrEmpty(letterFilter))
                {
                    if (letterFilter == "0-9")
                    {
                        // Rakamlarla başlayan ürünler
                        query = query.Where(p => p.DrugName != null && 
                            p.DrugName.Length > 0 && 
                            char.IsDigit(p.DrugName[0]));
                    }
                    else if (letterFilter == "Özel Karakterler")
                    {
                        // Harf ve rakam olmayan karakterlerle başlayan ürünler
                        query = query.Where(p => p.DrugName != null && 
                            p.DrugName.Length > 0 && 
                            !char.IsLetter(p.DrugName[0]) && 
                            !char.IsDigit(p.DrugName[0]));
                    }
                    else
                    {
                        // Normal harfle başlayan ürünler
                        query = query.Where(p => p.DrugName != null && 
                            p.DrugName.Length > 0 && 
                            p.DrugName.ToUpper().StartsWith(letterFilter.ToUpper()));
                    }
                }

                // Total count
                var totalCount = await query.CountAsync();

                // Pagination ile ürünleri getir
                var products = await query
                    .OrderBy(p => p.DrugName)
                    .Skip(offset)
                    .Take(limit)
                    .Select(p => new
                    {
                        p.Id,
                        p.Gtin,
                        p.DrugName,
                        p.ManufacturerName,
                        p.ManufacturerGln,
                        p.IsActive,
                        p.IsImported,
                        p.Price,
                        p.LastItsSyncAt,
                        p.CreatedAtUtc,
                        p.UpdatedAtUtc
                    })
                    .ToListAsync();

                return Results.Ok(new
                {
                    success = true,
                    data = products,
                    meta = new
                    {
                        offset = offset,
                        limit = limit,
                        totalItems = totalCount,
                        hasMore = (offset + limit) < totalCount
                    }
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Central Products List Error",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("ListCentralProducts")
        .WithDescription("Merkezi DB'deki tüm ürünleri listeler");

        // GET /api/central/products/letter-counts - Harf başına ürün sayıları
        group.MapGet("/letter-counts", async (
            ControlPlaneDbContext dbContext,
            HttpContext httpContext = default!
        ) =>
        {
            try
            {
                // Tüm ürünleri memory'ye çek ve orada işle
                var allProducts = await dbContext.CentralProducts
                    .Where(p => p.DrugName != null && p.DrugName.Length > 0)
                    .Select(p => new { p.DrugName, p.IsActive })
                    .ToListAsync();

                // Tüm harfler için sayıları hesapla
                var letterCounts = new Dictionary<string, object>();

                // A-Z harfleri
                for (char c = 'A'; c <= 'Z'; c++)
                {
                    var letter = c.ToString();
                    var activeCount = allProducts.Count(p => p.DrugName.ToUpper().StartsWith(letter) && p.IsActive);
                    var passiveCount = allProducts.Count(p => p.DrugName.ToUpper().StartsWith(letter) && !p.IsActive);
                    
                    letterCounts[letter] = new { active = activeCount, passive = passiveCount };
                }

                // 0-9 rakamları
                var digitActiveCount = allProducts.Count(p => char.IsDigit(p.DrugName[0]) && p.IsActive);
                var digitPassiveCount = allProducts.Count(p => char.IsDigit(p.DrugName[0]) && !p.IsActive);
                
                letterCounts["0-9"] = new { active = digitActiveCount, passive = digitPassiveCount };

                // Özel karakterler
                var specialActiveCount = allProducts.Count(p => !char.IsLetter(p.DrugName[0]) && !char.IsDigit(p.DrugName[0]) && p.IsActive);
                var specialPassiveCount = allProducts.Count(p => !char.IsLetter(p.DrugName[0]) && !char.IsDigit(p.DrugName[0]) && !p.IsActive);
                
                letterCounts["Özel Karakterler"] = new { active = specialActiveCount, passive = specialPassiveCount };

                return Results.Ok(new
                {
                    success = true,
                    data = letterCounts
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Letter Counts Error",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("GetLetterCounts")
        .WithDescription("Harf başına aktif/pasif ürün sayılarını getirir");

        // GET /api/central/products/stats - Merkezi DB istatistikleri
        group.MapGet("/stats", async (
            CentralProductSyncService syncService,
            IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            try
            {
                var totalCount = await syncService.GetCentralProductCountAsync();
                var activeCount = await syncService.GetActiveProductCountAsync();

                opasLogger.LogSystemEvent("CentralProductStats", "Stats requested", new { 
                    ClientIP = clientIP,
                    TotalCount = totalCount,
                    ActiveCount = activeCount
                });

                return Results.Ok(new
                {
                    success = true,
                    stats = new
                    {
                        totalProducts = totalCount,
                        activeProducts = activeCount,
                        inactiveProducts = totalCount - activeCount
                    }
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Central Product Stats Error",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("GetCentralProductStats")
        .WithDescription("Merkezi DB ürün istatistikleri");
    }
}
