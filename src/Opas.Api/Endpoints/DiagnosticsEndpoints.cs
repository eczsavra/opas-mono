using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

namespace Opas.Api.Endpoints;

public static class DiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/_endpoints", ([FromServices] IEnumerable<EndpointDataSource> sources) =>
        {
            var list = sources
                .SelectMany(s => s.Endpoints)
                .OfType<RouteEndpoint>()
                .Select(e => new { pattern = e.RoutePattern.RawText, order = e.Order });

            return Results.Ok(list);
        });

        return app;
    }
}
