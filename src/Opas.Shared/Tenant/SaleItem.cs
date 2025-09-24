using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.Tenant;

/// <summary>
/// Satış kalemi - Tenant specific
/// </summary>
public class SaleItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = default!;

    [Required]
    public int StockId { get; set; }
    public Stock Stock { get; set; } = default!;

    [Required]
    [MaxLength(20)]
    public string Gtin { get; set; } = default!; // Denormalized for performance

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = default!; // Denormalized

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalPrice { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "TRY";

    [MaxLength(20)]
    public string? BatchNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
