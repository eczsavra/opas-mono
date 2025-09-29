using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Search.Models;
using Opas.Infrastructure.Persistence;

namespace Opas.Infrastructure.Search.Services;

public class SearchService : ISearchService
{
    private readonly PublicDbContext _dbContext;

    public SearchService(PublicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        try
        {
            var query = _dbContext.CentralProducts.AsQueryable();

            // Search filter - Case insensitive search
            if (!string.IsNullOrEmpty(request.Query))
            {
                var searchLower = request.Query.ToLower();
                query = query.Where(p => (p.DrugName != null && p.DrugName.ToLower().Contains(searchLower)) || 
                                       (p.Gtin != null && p.Gtin.ToLower().Contains(searchLower)) ||
                                       (p.ManufacturerName != null && p.ManufacturerName.ToLower().Contains(searchLower)));
            }

            // Letter filter - Harf filtresi
            if (!string.IsNullOrEmpty(request.LetterFilter))
            {
                if (request.LetterFilter == "0-9")
                {
                    query = query.Where(p => p.DrugName != null && char.IsDigit(p.DrugName[0]));
                }
                else if (request.LetterFilter == "Özel Karakterler")
                {
                    query = query.Where(p => p.DrugName != null && 
                        !char.IsLetter(p.DrugName[0]) && !char.IsDigit(p.DrugName[0]));
                }
                else
                {
                    var letter = request.LetterFilter.ToUpper();
                    query = query.Where(p => p.DrugName != null && p.DrugName.ToUpper().StartsWith(letter));
                }
            }

            // Active/Inactive filter
            if (request.IsActive.HasValue)
            {
                query = query.Where(p => p.Active == request.IsActive.Value);
            }

            // Pagination
            var totalCount = await query.CountAsync();
            var products = await query
                .Skip(request.Offset)
                .Take(request.Limit)
                .ToListAsync();

            return new SearchResponse 
            { 
                Success = true,
                Data = products.Cast<object>().ToList(),
                Meta = new { 
                    total = totalCount,
                    offset = request.Offset,
                    limit = request.Limit,
                    hasMore = request.Offset + request.Limit < totalCount
                }
            };
        }
        catch (Exception ex)
        {
            return new SearchResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }
}
