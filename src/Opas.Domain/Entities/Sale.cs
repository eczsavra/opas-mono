using Opas.Domain.Primitives;

namespace Opas.Domain.Entities;

/// <summary>
/// Satış işlemi entity'si
/// </summary>
public class Sale : BaseEntity
{
    public string SaleNumber { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Guid? PrescriptionId { get; set; }
    public DateTime SaleDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties (Domain layer'da sadece ID'ler)
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
