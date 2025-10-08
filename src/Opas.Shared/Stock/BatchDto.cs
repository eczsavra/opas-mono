namespace Opas.Shared.Stock;

/// <summary>
/// Batch (OTC ürün partisi) DTO
/// </summary>
public record BatchDto
{
    public string? Id { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public string? BatchNumber { get; init; }
    public DateTime ExpiryDate { get; init; }
    public int Quantity { get; init; }
    public int InitialQuantity { get; init; }
    public decimal UnitCost { get; init; }
    public decimal? TotalCost { get; init; }
    public string? LocationId { get; init; }
    public bool IsActive { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Batch oluşturma isteği
/// </summary>
public record CreateBatchRequest
{
    public string ProductId { get; init; } = string.Empty;
    public string? BatchNumber { get; init; } // Boş bırakılırsa otomatik üretilir
    public DateTime ExpiryDate { get; init; }
    public int Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public string? LocationId { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Batch yanıtı
/// </summary>
public record BatchResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public BatchDto? Batch { get; init; }
}

/// <summary>
/// Batch listesi yanıtı
/// </summary>
public record BatchListResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public List<BatchDto> Batches { get; init; } = new();
    public int TotalCount { get; init; }
}

