namespace Opas.Shared.Tenant;

/// <summary>
/// DTO for draft sales data (incomplete sales tabs)
/// </summary>
public record DraftSalesDto
{
    public string TabId { get; init; } = string.Empty;
    public string TabLabel { get; init; } = string.Empty;
    public List<DraftProductDto> Products { get; init; } = new();
    public bool IsCompleted { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record DraftProductDto
{
    public string Id { get; init; } = string.Empty;
    public string Gtin { get; init; } = string.Empty;
    public string DrugName { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public decimal TotalPrice { get; init; }
    
    // ✅ CRITICAL: Additional fields for persistence
    public string? Category { get; init; }
    public decimal? UnitCost { get; init; }
    public int? StockQuantity { get; init; }
    public string? SerialNumber { get; init; }
    public string? ExpiryDate { get; init; }
    public string? LotNumber { get; init; }
}

/// <summary>
/// Request to sync draft sales from client to backend
/// </summary>
public record SyncDraftSalesRequest
{
    public List<DraftSalesDto> Tabs { get; init; } = new();
    public string? ActiveTabId { get; init; }
    public int TabCounter { get; init; }
}

/// <summary>
/// Response with synced draft sales
/// </summary>
public record DraftSalesResponse
{
    public bool Success { get; init; }
    public List<DraftSalesDto> Tabs { get; init; } = new();
    public string? ActiveTabId { get; init; }
    public int TabCounter { get; init; }
    public string? Message { get; init; }
}

