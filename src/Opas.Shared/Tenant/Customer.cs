using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.Tenant;

/// <summary>
/// Eczane müşteri kaydı - Tenant specific
/// </summary>
public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(11)] // TC Kimlik No
    public string TcNumber { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = default!;

    [MaxLength(15)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public DateTime BirthDate { get; set; }

    [MaxLength(10)]
    public string? Gender { get; set; } // M/F

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
