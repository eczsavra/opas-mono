using Opas.Domain.Primitives;

namespace Opas.Domain.Entities;

/// <summary>
/// Ödeme işlemi entity'si
/// </summary>
public class Payment : BaseEntity
{
    public Guid SaleId { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Transfer, etc.
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties (Domain layer'da sadece ID'ler)
    public Sale Sale { get; set; } = null!;
}
