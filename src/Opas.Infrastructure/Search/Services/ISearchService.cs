using Opas.Infrastructure.Search.Models;

namespace Opas.Infrastructure.Search.Services;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(SearchRequest request);
}
