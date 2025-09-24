using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Opas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Opas.Api.Endpoints.Infra;

public static class InfraHealthEndpoints
{
    public static IEndpointRouteBuilder MapInfraHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /infra/health
        app.MapGet("/infra/health", ([FromServices] IConfiguration cfg,
                                     [FromServices] IServiceProvider sp) =>
        {
            var cs = cfg["Database:Postgres:ConnectionString"];
            var hasCs = !string.IsNullOrWhiteSpace(cs);

            var publicCtxRegistered = sp.GetService<PublicDbContext>() is not null;
            var tenantCtxRegistered = sp.GetService<TenantDbContext>() is not null;

            return Results.Ok(new
            {
                provider = "Npgsql",
                hasConnectionString = hasCs,
                contextsRegistered = new { publicCtx = publicCtxRegistered, tenantCtx = tenantCtxRegistered }
            });
        })
        .WithName("InfraHealth");

        return app;
    }
}
