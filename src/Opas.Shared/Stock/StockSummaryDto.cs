namespace Opas.Shared.Stock;

/// <summary>
/// Stok özet bilgisi DTO
/// </summary>
public record StockSummaryDto
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string? Gtin { get; init; }
    
    // İlaçlar için
    public int TotalTracked { get; init; } // Karekod ile takipli
    public int TotalUntracked { get; init; } // Barkod ile satılmış
    
    // OTC için
    public int TotalQuantity { get; init; } // Toplam adet
    
    // Finansal
    public decimal TotalValue { get; init; } // Stok değeri (maliyet bazında)
    public decimal? AverageCost { get; init; } // Ortalama maliyet
    
    // Tarihler
    public DateTime? LastMovementDate { get; init; }
    public DateTime? LastCountedDate { get; init; }
    public DateTime? NearestExpiryDate { get; init; } // En yakın SKT
    
    // Uyarılar
    public bool HasExpiringSoon { get; init; }
    public bool HasExpired { get; init; }
    public bool HasLowStock { get; init; }
    public bool NeedsAttention { get; init; }
    
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Stok özeti listesi yanıtı
/// </summary>
public record StockSummaryListResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public List<StockSummaryDto> Summary { get; init; } = new();
    public int TotalCount { get; init; }
}

/// <summary>
/// Tek ürün stok özeti yanıtı
/// </summary>
public record SingleStockSummaryResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public StockSummaryDto? Summary { get; init; }
}

