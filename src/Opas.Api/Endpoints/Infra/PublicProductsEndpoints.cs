using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Opas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Opas.Api.Endpoints.Infra;

public static class PublicProductsEndpoints
{
    public static IEndpointRouteBuilder MapPublicProductsEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /public/products
        app.MapGet("/public/products", async ([FromServices] IConfiguration cfg,
                                              [FromServices] IServiceProvider sp,
                                              int take = 20,
                                              string? q = null) =>
        {
            // 1) CS yoksa: asla DbContext'e dokunma → direkt 503
            var cs = cfg["Database:Postgres:ConnectionString"];
            if (string.IsNullOrWhiteSpace(cs))
            {
                return Results.Json(new
                {
                    ok = false,
                    error = "public db not configured (no connection string)",
                    hint = "Set Database:Postgres:ConnectionString in appsettings.Development.json"
                }, statusCode: 503);
            }

            // 2) CS var → DbContext kontrolü
            var db = sp.GetService<PublicDbContext>();
            if (db is null)
            {
                return Results.Json(new
                {
                    ok = false,
                    error = "public db context not registered",
                    hint = "Check AddInfrastructure(...) registration"
                }, statusCode: 503);
            }

            try
            {
                var query = db.Products.AsNoTracking().Where(p => p.IsActive);

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var term = q.Trim();
                    // PostgreSQL ILIKE
                    query = query.Where(p =>
                        EF.Functions.ILike(p.Name, $"%{term}%") ||
                        p.Gtin.Contains(term));
                }

                var list = await query
                    .OrderBy(p => p.Name)
                    .Take(Math.Clamp(take, 1, 100))
                    .Select(p => new { p.Id, p.Gtin, p.Name, p.Form, p.Strength, p.Manufacturer })
                    .ToListAsync();

                return Results.Ok(list);
            }
            catch
            {
                return Results.Json(new
                {
                    ok = false,
                    error = "database unreachable",
                    hint = "Ensure Postgres is running and the ConnectionString is correct"
                }, statusCode: 503);
            }
        })
        .WithName("PublicProducts");

        return app;
    }
}
