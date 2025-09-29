namespace Opas.Infrastructure.Search.Models;

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string? LetterFilter { get; set; }
    public bool? IsActive { get; set; }
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 50;
}
