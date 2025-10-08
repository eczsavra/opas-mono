namespace Opas.Shared.Tenant;

/// <summary>
/// Satış tamamlama isteği (Draft → Final)
/// </summary>
public record CompleteSaleRequest
{
    public required string TabId { get; init; }
    public required List<SaleItemDto> Items { get; init; }
    public required PaymentInfo Payment { get; init; }
    public required string SaleType { get; init; } // NORMAL, CONSIGNMENT
    public CustomerInfo? Customer { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Satış kalemi bilgisi
/// </summary>
public record SaleItemDto
{
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }
    public string? ProductCategory { get; init; } // PHARMACEUTICAL, OTC
    
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public decimal? UnitCost { get; init; }
    public decimal DiscountRate { get; init; } = 0;
    public required decimal TotalPrice { get; init; }
    
    // İlaç takip bilgileri (opsiyonel)
    public string? SerialNumber { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public string? LotNumber { get; init; }
    public string? Gtin { get; init; }
}

/// <summary>
/// Ödeme bilgisi
/// </summary>
public record PaymentInfo
{
    public required string Method { get; init; } // CASH, CARD, CREDIT, CONSIGNMENT, IBAN, QR
    public required decimal Amount { get; init; }
    public string? TransactionId { get; init; } // Kart ödemesi için
    public string? Notes { get; init; }
}

/// <summary>
/// Müşteri bilgisi (opsiyonel - Cari modülü için hazırlık)
/// </summary>
public record CustomerInfo
{
    public string? CustomerId { get; init; } // İleride cari hesap ID
    public string? Name { get; init; }
    public string? TcNo { get; init; } // Reçeteli satış için
    public string? Phone { get; init; }
}

/// <summary>
/// Satış tamamlama cevabı
/// </summary>
public record CompleteSaleResponse
{
    public required string SaleId { get; init; }
    public required string SaleNumber { get; init; }
    public required DateTime SaleDate { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string PaymentMethod { get; init; }
    public required int ItemCount { get; init; }
    public bool StockDeducted { get; init; }
    public string? FiscalReceiptNumber { get; init; }
}

/// <summary>
/// Satış listesi için özet bilgi
/// </summary>
public record SaleDto
{
    public required string SaleId { get; init; }
    public required string SaleNumber { get; init; }
    public required DateTime SaleDate { get; init; }
    
    public required decimal SubtotalAmount { get; init; }
    public required decimal DiscountAmount { get; init; }
    public required decimal TotalAmount { get; init; }
    
    public required string PaymentMethod { get; init; }
    public required string PaymentStatus { get; init; }
    public required string SaleType { get; init; }
    
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    
    public required string CreatedBy { get; init; }
    public required DateTime CreatedAt { get; init; }
    
    public int ItemCount { get; init; }
    public string? FiscalReceiptNumber { get; init; }
    public string? FiscalStatus { get; init; }
}

/// <summary>
/// Satış detayı (satış + kalemler)
/// </summary>
public record SaleDetailDto
{
    public required SaleDto Sale { get; init; }
    public required List<SaleItemDetailDto> Items { get; init; }
}

/// <summary>
/// Satış kalemi detayı
/// </summary>
public record SaleItemDetailDto
{
    public required int Id { get; init; }
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }
    public string? ProductCategory { get; init; }
    
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public decimal? UnitCost { get; init; }
    public required decimal TotalPrice { get; init; }
    
    public string? SerialNumber { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public string? LotNumber { get; init; }
    public string? Gtin { get; init; }
    
    public bool StockDeducted { get; init; }
}

