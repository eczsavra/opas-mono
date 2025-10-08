namespace Opas.Shared.Stock;

/// <summary>
/// Stok hareketi DTO (Tüm hareket tipleri için ortak)
/// </summary>
public record StockMovementDto
{
    public string? Id { get; init; }
    public string? MovementNumber { get; init; }
    public DateTime MovementDate { get; init; } = DateTime.UtcNow;
    public string MovementType { get; init; } = string.Empty; // SALE_RETAIL, PURCHASE_DEPOT, vb.
    
    public string ProductId { get; init; } = string.Empty;
    public int QuantityChange { get; init; } // Pozitif = giriş, negatif = çıkış
    
    // Maliyet bilgileri (CARİ için kritik!)
    public decimal? UnitCost { get; init; }
    public decimal? TotalCost { get; init; }
    
    // Karekod bilgileri (ilaçlar için)
    public string? SerialNumber { get; init; }
    public string? LotNumber { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public string? Gtin { get; init; }
    
    // Batch bilgisi (OTC için)
    public string? BatchId { get; init; }
    
    // Referans (hangi işlemden geldi?)
    public string? ReferenceType { get; init; } // SALE_ORDER, PURCHASE_ORDER, vb.
    public string? ReferenceId { get; init; }
    
    // Lokasyon
    public string? LocationId { get; init; }
    
    // Kullanıcı
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    // Düzeltme
    public bool IsCorrection { get; init; }
    public string? CorrectionReason { get; init; }
    
    // Notlar
    public string? Notes { get; init; }
}

/// <summary>
/// Stok hareketi oluşturma isteği
/// </summary>
public record CreateStockMovementRequest
{
    public string MovementType { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    public int QuantityChange { get; init; }
    
    public decimal? UnitCost { get; init; }
    
    // Mal Fazlası (MF) - örn: 10+1, 20+3
    public int? BonusQuantity { get; init; } // Hediye/bedava adet
    public string? BonusRatio { get; init; } // Görsel gösterim için: "10+1", "20+3"
    
    // Karekod bilgileri (ilaçlar için - isteğe bağlı)
    public string? SerialNumber { get; init; }
    public string? LotNumber { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public string? Gtin { get; init; }
    
    // Batch bilgisi (OTC için - isteğe bağlı)
    public string? BatchId { get; init; }
    
    // Referans
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
    
    // Lokasyon
    public string? LocationId { get; init; }
    
    // Düzeltme
    public bool IsCorrection { get; init; }
    public string? CorrectionReason { get; init; }
    
    // Notlar
    public string? Notes { get; init; }
}

/// <summary>
/// Stok hareketi yanıtı
/// </summary>
public record StockMovementResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public StockMovementDto? Movement { get; init; }
}

/// <summary>
/// Stok hareketleri listesi yanıtı
/// </summary>
public record StockMovementsResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public List<StockMovementDto> Movements { get; init; } = new();
    public int TotalCount { get; init; }
}

