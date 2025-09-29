using Microsoft.AspNetCore.Mvc;
using Opas.Infrastructure.Search.Services;
using Opas.Infrastructure.Search.Models;

namespace Opas.Api.Endpoints;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/search")
            .WithTags("Search");

        group.MapGet("/products", async (
            ISearchService searchService,
            [FromQuery] string? query = null,
            [FromQuery] string? letterFilter = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 50) =>
        {
            var request = new SearchRequest 
            { 
                Query = query ?? string.Empty,
                LetterFilter = letterFilter,
                IsActive = isActive,
                Offset = offset,
                Limit = limit
            };
            var response = await searchService.SearchAsync(request);
            return Results.Ok(response);
        });
    }
}
