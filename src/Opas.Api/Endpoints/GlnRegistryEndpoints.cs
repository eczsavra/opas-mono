using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Shared.Logging;

namespace Opas.Api.Endpoints;

public static class GlnRegistryEndpoints
{
    public static void MapGlnRegistryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gln-registry")
            .WithTags("GLN Registry")
            .WithDescription("GLN Paydaş Listesi");

        // GET /api/gln-registry - Tüm GLN listesi (pagination ile)
        group.MapGet("/", async (
            PublicDbContext db,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string? city = null,
            [FromQuery] string? town = null,
            [FromQuery] bool? active = null) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            opasLogger.LogSystemEvent("GlnRegistryList", "GLN registry list requested", new { 
                ClientIP = clientIP,
                Page = page,
                PageSize = pageSize,
                Search = search,
                City = city,
                Town = town,
                Active = active
            });

            var query = db.GlnRegistry.AsNoTracking();

            // Arama filtresi
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(g => 
                    EF.Functions.ILike(g.Gln, $"%{search}%") ||
                    EF.Functions.ILike(g.CompanyName ?? "", $"%{search}%") ||
                    EF.Functions.ILike(g.Authorized ?? "", $"%{search}%"));
            }

            // Şehir filtresi
            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(g => EF.Functions.ILike(g.City ?? "", $"%{city}%"));
            }

            // İlçe filtresi
            if (!string.IsNullOrWhiteSpace(town))
            {
                query = query.Where(g => EF.Functions.ILike(g.Town ?? "", $"%{town}%"));
            }

            // Aktif/Pasif filtresi
            if (active.HasValue)
            {
                query = query.Where(g => g.Active == active.Value);
            }

            // Toplam kayıt sayısı
            var totalCount = await query.CountAsync();

            // Sayfalama ve sıralama
            var glnList = await query
                .OrderBy(g => g.CompanyName ?? g.Gln)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new
                {
                    id = g.Id,
                    gln = g.Gln,
                    companyName = g.CompanyName,
                    authorized = g.Authorized,
                    email = g.Email,
                    phone = g.Phone,
                    city = g.City,
                    town = g.Town,
                    address = g.Address,
                    active = g.Active ?? true,
                    source = g.Source,
                    importedAt = g.ImportedAtUtc
                })
                .ToListAsync();

            return Results.Ok(new
            {
                success = true,
                data = glnList,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                currentPage = page,
                pageSize
            });
        })
        .WithName("GetGlnRegistryList")
        .WithDescription("GLN paydaş listesini getirir");

        // GET /api/gln-registry/stats - İstatistikler
        group.MapGet("/stats", async (
            PublicDbContext db,
            IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            opasLogger.LogSystemEvent("GlnRegistryStats", "GLN registry stats requested", new { 
                ClientIP = clientIP 
            });

            var totalCount = await db.GlnRegistry.CountAsync();
            var activeCount = await db.GlnRegistry.Where(g => g.Active == true).CountAsync();
            
            var cityCounts = await db.GlnRegistry
                .Where(g => !string.IsNullOrEmpty(g.City))
                .GroupBy(g => g.City)
                .Select(g => new { city = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToListAsync();

            return Results.Ok(new
            {
                success = true,
                stats = new
                {
                    total = totalCount,
                    active = activeCount,
                    inactive = totalCount - activeCount,
                    topCities = cityCounts
                }
            });
        })
        .WithName("GetGlnRegistryStats")
        .WithDescription("GLN paydaş istatistiklerini getirir");

        // GET /api/gln-registry/towns/{city} - Şehire göre ilçe listesi
        group.MapGet("/towns/{city}", async (
            string city,
            PublicDbContext db,
            IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            opasLogger.LogSystemEvent("GlnRegistryTowns", "GLN registry towns requested", new { 
                ClientIP = clientIP,
                City = city
            });

            var towns = await db.GlnRegistry
                .Where(g => !string.IsNullOrEmpty(g.City) && EF.Functions.ILike(g.City, $"%{city}%"))
                .Where(g => !string.IsNullOrEmpty(g.Town))
                .GroupBy(g => g.Town)
                .Select(g => new { town = g.Key, count = g.Count() })
                .OrderBy(x => x.town)
                .ToListAsync();

            return Results.Ok(new
            {
                success = true,
                data = towns
            });
        })
        .WithName("GetGlnRegistryTowns")
        .WithDescription("Belirtilen şehirdeki ilçe listesini getirir");
    }
}
