using Opas.Domain.Primitives;

namespace Opas.Domain.Entities;

/// <summary>
/// Satış kalemi entity'si
/// </summary>
public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Guid StockId { get; set; }
    public string Gtin { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties (Domain layer'da sadece ID'ler)
    public Sale Sale { get; set; } = null!;
}
