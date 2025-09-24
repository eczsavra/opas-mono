using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Opas.Infrastructure.Persistence;
using Opas.Shared.MultiTenancy;

namespace Opas.Api.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /tenant/ping
        app.MapGet("/tenant/ping", (ITenantContextAccessor accessor) =>
        {
            var t = accessor.Current;
            return Results.Ok(new { ok = true, tenant = new { id = t.TenantId, region = t.Region } });
        });

        // GET /tenant/id
        app.MapGet("/tenant/id", (ITenantProvider provider) =>
        {
            return Results.Ok(new { ok = true, tenantId = provider.TenantId });
        });

        // GET /tenant/dbctx
        app.MapGet("/tenant/dbctx", (IServiceProvider sp) =>
        {
            var db = sp.GetService<TenantDbContext>();
            if (db is null)
            {
                return Results.Json(new {
                    ok = false,
                    error = "tenant db context not registered",
                    hint = "Set Database:Postgres:ConnectionString to register DbContexts"
                }, statusCode: 503);
            }

            // Db’ye sorgu atmadan sadece property döndür
            return Results.Ok(new { ok = true, tenantIdFromDbContext = db.TenantId });
        });

        return app;
    }
}
