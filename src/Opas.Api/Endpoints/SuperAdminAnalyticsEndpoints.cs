using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;

namespace Opas.Api.Endpoints;

public static class SuperAdminAnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapSuperAdminAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/superadmin/analytics")
            .WithTags("SuperAdmin Analytics")
            .WithOpenApi();

        // GET /api/superadmin/analytics/dashboard - Dashboard istatistikleri
        group.MapGet("/dashboard", async (ControlPlaneDbContext dbContext) =>
        {
            try
            {
                // Toplam ürün sayısı (aktif/pasif)
                var totalProducts = await dbContext.CentralProducts.CountAsync();
                var activeProducts = await dbContext.CentralProducts.CountAsync(p => p.IsActive);
                var passiveProducts = totalProducts - activeProducts;

                // Toplam GLN sayısı
                var totalGlns = await dbContext.GlnRegistry.CountAsync();

                // Son sync tarihi
                var lastSyncDate = await dbContext.CentralProducts
                    .Where(p => p.LastItsSyncAt != null)
                    .MaxAsync(p => p.LastItsSyncAt);

                // Şehir bazında GLN dağılımı
                var cityDistribution = await dbContext.GlnRegistry
                    .Where(g => !string.IsNullOrEmpty(g.City))
                    .GroupBy(g => g.City)
                    .Select(g => new { City = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                // Son 7 günlük sync istatistikleri
                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
                var recentSyncs = await dbContext.CentralProducts
                    .Where(p => p.LastItsSyncAt >= sevenDaysAgo)
                    .GroupBy(p => p.LastItsSyncAt!.Value.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        products = new
                        {
                            total = totalProducts,
                            active = activeProducts,
                            passive = passiveProducts,
                            activePercentage = totalProducts > 0 ? Math.Round((double)activeProducts / totalProducts * 100, 1) : 0
                        },
                        glns = new
                        {
                            total = totalGlns
                        },
                        sync = new
                        {
                            lastSyncDate = lastSyncDate,
                            recentSyncs = recentSyncs
                        },
                        distribution = new
                        {
                            cities = cityDistribution
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Dashboard Analytics Error",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("GetDashboardAnalytics")
        .WithDescription("SuperAdmin dashboard için sistem geneli istatistikleri getirir");

        // GET /api/superadmin/analytics/tenants - Tenant istatistikleri
        group.MapGet("/tenants", async (ControlPlaneDbContext dbContext) =>
        {
            try
            {
                // Tenant sayısı (GLN registry'den)
                var totalTenants = await dbContext.GlnRegistry.CountAsync();

                // Aktif tenant sayısı (son 30 gün içinde sync olan)
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var activeTenants = await dbContext.CentralProducts
                    .Where(p => p.LastItsSyncAt >= thirtyDaysAgo)
                    .Select(p => p.ManufacturerGln)
                    .Distinct()
                    .CountAsync();

                // Şehir bazında tenant dağılımı
                var tenantCityDistribution = await dbContext.GlnRegistry
                    .Where(g => !string.IsNullOrEmpty(g.City))
                    .GroupBy(g => g.City)
                    .Select(g => new { City = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(15)
                    .ToListAsync();

                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        total = totalTenants,
                        active = activeTenants,
                        inactive = totalTenants - activeTenants,
                        cityDistribution = tenantCityDistribution
                    }
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Tenant Analytics Error",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("GetTenantAnalytics")
        .WithDescription("Tenant istatistiklerini getirir");

        return app;
    }
}
