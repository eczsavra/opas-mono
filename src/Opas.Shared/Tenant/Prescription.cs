using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.Tenant;

/// <summary>
/// Reçete kaydı - Tenant specific
/// </summary>
public class Prescription
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string PrescriptionNumber { get; set; } = default!; // Reçete numarası

    [Required]
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public string DoctorName { get; set; } = default!;

    [MaxLength(100)]
    public string? HospitalName { get; set; }

    [MaxLength(50)]
    public string? DoctorLicenseNumber { get; set; }

    public DateTime PrescriptionDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active/Used/Expired

    [MaxLength(20)]
    public string? InsuranceType { get; set; } // SGK/Private

    [MaxLength(50)]
    public string? InsuranceNumber { get; set; }

    public decimal? InsuranceCoverage { get; set; } // Percentage

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
