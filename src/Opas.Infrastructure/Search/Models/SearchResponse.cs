namespace Opas.Infrastructure.Search.Models;

public class SearchResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<object> Data { get; set; } = new();
    public object Meta { get; set; } = new();
}
