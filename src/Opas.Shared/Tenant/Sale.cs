using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.Tenant;

/// <summary>
/// Eczane satış kaydı - Tenant specific
/// </summary>
public class Sale
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string SaleNumber { get; set; } = default!; // Fiş numarası

    public int? CustomerId { get; set; } // Nullable - Anonim satış olabilir
    public Customer? Customer { get; set; }

    public int? PrescriptionId { get; set; } // Reçeteli ise
    public Prescription? Prescription { get; set; }

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal NetAmount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "TRY";

    [MaxLength(20)]
    public string PaymentMethod { get; set; } = "Cash"; // Cash/Card/Insurance

    [MaxLength(20)]
    public string Status { get; set; } = "Completed"; // Completed/Cancelled/Returned

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
