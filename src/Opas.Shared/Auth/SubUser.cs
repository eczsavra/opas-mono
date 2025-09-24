using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.Auth;

/// <summary>
/// Sub User entity - Pharmacy employees (not pharmacist admin)
/// Created by PharmacistAdmin via dashboard (future feature)
/// </summary>
public class SubUser
{
    public int Id { get; set; }
    
    // CRITICAL: Smart ID - unique across all user types
    [Required]
    [MaxLength(10)]
    public string SubUserId { get; set; } = default!; // SUB_000001 format
    
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
    
    // CRITICAL: SubUser belongs to a tenant (created by PharmacistAdmin)
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = default!; // Same as PharmacistAdmin's TenantId
    
    // Reference to who created this sub user
    public int CreatedByPharmacistAdminId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = default!;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = default!;
    
    [MaxLength(11)]
    public string? TcNumber { get; set; }
    
    public int? BirthYear { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Password salt for additional security
    [Required]
    [MaxLength(128)]
    public string PasswordSalt { get; set; } = default!;
    
    // Sub user roles - will be expanded later
    [MaxLength(50)]
    public string Role { get; set; } = "EczaneTeknikeri"; // EczaneTeknikeri, StajyerEczaci, IkinciEczaci, YardimciEczaci, Cirak, EczanePersoneli
}
