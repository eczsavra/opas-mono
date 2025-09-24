using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Opas.Infrastructure.Persistence;
using Opas.Shared.ControlPlane;
using Opas.Shared.Validation;
using Opas.Api.Control;
using Microsoft.AspNetCore.Mvc;

namespace Opas.Api.Endpoints;

public static class ControlEndpoints
{
    public static IEndpointRouteBuilder MapControlEndpoints(this IEndpointRouteBuilder app)
    {
        // /control/health
        app.MapGet("/control/health", async (IServiceProvider sp) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var conn =
                configuration["Database:ControlPlane:ConnectionString"]
                ?? configuration["ControlPlane:ConnectionString"];

            var hasConnectionString = !string.IsNullOrWhiteSpace(conn);
            var contextRegistered = sp.GetService<ControlPlaneDbContext>() is not null;

            bool canConnect = false;
            string? error = null;

            if (contextRegistered)
            {
                try
                {
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

                    await db.Database.OpenConnectionAsync();
                    canConnect = db.Database.GetDbConnection().State == System.Data.ConnectionState.Open;
                    await db.Database.CloseConnectionAsync();

                    await using var npg = new NpgsqlConnection(conn);
                    await npg.OpenAsync();
                    await npg.CloseAsync();
                }
                catch (Exception ex)
                {
                    error = ex.GetType().Name + ": " + ex.Message;
                    canConnect = false;
                }
            }

            return Results.Ok(new
            {
                provider = "Npgsql",
                hasConnectionString,
                contextRegistered,
                canConnect,
                error
            });
        })
        .WithName("ControlHealth");


        return app;
    }
}
