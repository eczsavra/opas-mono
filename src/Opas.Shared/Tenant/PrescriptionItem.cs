using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.Tenant;

/// <summary>
/// Reçete kalemi - Tenant specific
/// </summary>
public class PrescriptionItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = default!;

    [Required]
    [MaxLength(20)]
    public string Gtin { get; set; } = default!; // İlaç barkodu

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = default!;

    [MaxLength(50)]
    public string? Strength { get; set; } // 500mg

    [MaxLength(50)]
    public string? Form { get; set; } // Tablet/Şurup

    public int PrescribedQuantity { get; set; } // Doktor yazdığı miktar

    public int? DispensedQuantity { get; set; } // Verilen miktar

    [MaxLength(200)]
    public string? Usage { get; set; } // Kullanım şekli: "Günde 3 kez 1 tablet"

    [MaxLength(100)]
    public string? Frequency { get; set; } // "3x1", "2x1"

    public int? DurationDays { get; set; } // Kaç gün kullanılacak

    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending/Dispensed/Partial

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
