using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.Tenant;

/// <summary>
/// Eczane stok kaydı - Tenant specific
/// </summary>
public class Stock
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)] // GTIN/Barcode
    public string Gtin { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = default!;

    [MaxLength(50)]
    public string? ProductForm { get; set; } // Tablet/Şurup

    [MaxLength(50)]
    public string? Strength { get; set; } // 500mg

    [MaxLength(120)]
    public string? Manufacturer { get; set; }

    [MaxLength(20)]
    public string? BatchNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int QuantityInStock { get; set; }

    public int? MinimumStock { get; set; } // Alert threshold

    public decimal PurchasePrice { get; set; }

    public decimal SalePrice { get; set; }

    [MaxLength(3)] // TRY, USD
    public string Currency { get; set; } = "TRY";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
