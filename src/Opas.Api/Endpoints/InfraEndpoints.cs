using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Opas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Opas.Api.Endpoints;

public static class InfraEndpoints
{
    public static IEndpointRouteBuilder MapInfraEndpoints(this IEndpointRouteBuilder app)
    {
        // Infrastructure health check
        app.MapGet("/infra/status", ([FromServices] IConfiguration cfg, [FromServices] IServiceProvider sp) =>
        {
            var publicDb = sp.GetService<PublicDbContext>();
            var tenantDb = sp.GetService<TenantDbContext>();
            var controlDb = sp.GetService<ControlPlaneDbContext>();

            return Results.Ok(new
            {
                timestamp = DateTime.UtcNow,
                infrastructure = new
                {
                    publicDb = publicDb != null ? "registered" : "not-registered",
                    tenantDb = tenantDb != null ? "registered" : "not-registered", 
                    controlDb = controlDb != null ? "registered" : "not-registered"
                },
                configuration = new
                {
                    hasPublicConnection = !string.IsNullOrWhiteSpace(cfg["Database:Postgres:ConnectionString"]),
                    hasControlConnection = !string.IsNullOrWhiteSpace(cfg["Database:ControlPlane:ConnectionString"]),
                    serverHost = cfg["Server:Host"] ?? "not-set",
                    serverPort = cfg["Server:Port"] ?? "not-set"
                }
            });
        })
        .WithName("InfraStatus");

        // Database connectivity test
        app.MapGet("/infra/db/test", async ([FromServices] IServiceProvider sp) =>
        {
            var results = new Dictionary<string, object>();

            // Test Public DB
            try
            {
                var publicDb = sp.GetService<PublicDbContext>();
                if (publicDb != null)
                {
                    await publicDb.Database.OpenConnectionAsync();
                    results["publicDb"] = new { status = "connected", state = publicDb.Database.GetDbConnection().State.ToString() };
                    await publicDb.Database.CloseConnectionAsync();
                }
                else
                {
                    results["publicDb"] = new { status = "not-registered" };
                }
            }
            catch (Exception ex)
            {
                results["publicDb"] = new { status = "error", error = ex.Message };
            }

            // Test Control Plane DB
            try
            {
                var controlDb = sp.GetService<ControlPlaneDbContext>();
                if (controlDb != null)
                {
                    await controlDb.Database.OpenConnectionAsync();
                    results["controlDb"] = new { status = "connected", state = controlDb.Database.GetDbConnection().State.ToString() };
                    await controlDb.Database.CloseConnectionAsync();
                }
                else
                {
                    results["controlDb"] = new { status = "not-registered" };
                }
            }
            catch (Exception ex)
            {
                results["controlDb"] = new { status = "error", error = ex.Message };
            }

            return Results.Ok(new
            {
                timestamp = DateTime.UtcNow,
                databases = results
            });
        })
        .WithName("InfraDbTest");

        return app;
    }
}
