using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.Auth;

/// <summary>
/// Pharmacist Admin entity - Main tenant owner and admin
/// Each pharmacy tenant has exactly ONE pharmacist admin
/// </summary>
public class PharmacistAdmin
{
    public int Id { get; set; }
    
    // CRITICAL: Smart ID - unique across all user types
    [Required]
    [MaxLength(10)]
    public string PharmacistId { get; set; } = default!; // PHM_000001 format
    
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = default!;
    
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = default!;
    
    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = default!;
    
    [MaxLength(15)]
    public string? Phone { get; set; }
    
    // CRITICAL: GLN belongs to pharmacist (person), not tenant!
    [Required]
    [MaxLength(13)]
    public string PersonalGln { get; set; } = default!; // Eczacının GLN'si
    
    // CRITICAL: Tenant ID is separate from GLN (our generated ID)
    [Required]  
    [MaxLength(50)]
    public string TenantId { get; set; } = default!; // OPAS_TNT_000001 gibi
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = default!;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = default!;
    
    [MaxLength(11)]
    public string? TcNumber { get; set; }
    
    public int? BirthYear { get; set; }
    
    // Eczane Sicil No (18341699 gibi)
    [MaxLength(20)]
    public string? PharmacyRegistrationNo { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public bool IsPhoneVerified { get; set; } = false;
    public bool IsNviVerified { get; set; } = false;
    
    // Tenant Status for C Approach (Staged Provisioning)
    [MaxLength(20)]
    public string TenantStatus { get; set; } = "Pending"; // Pending, Provisioning, Active, Failed
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Password salt for additional security
    [Required]
    [MaxLength(128)]
    public string PasswordSalt { get; set; } = default!;
    
    // Always PharmacyAdmin - no other role for pharmacist
    [MaxLength(50)]
    public string Role { get; set; } = "PharmacyAdmin";
}
