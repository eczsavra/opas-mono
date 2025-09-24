using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.Auth;

/// <summary>
/// User account entity for pharmacy registration and login
/// </summary>
public class User
{
    public int Id { get; set; }
    
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
    
    [Required]
    [MaxLength(13)]
    public string PharmacyGln { get; set; } = default!; // GLN ile bağlantı
    
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
    public bool IsEmailVerified { get; set; } = false;
    public bool IsPhoneVerified { get; set; } = false;
    public bool IsNviVerified { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Password salt for additional security
    [Required]
    [MaxLength(128)]
    public string PasswordSalt { get; set; } = default!;
    
    // Role information
    [MaxLength(50)]
    public string Role { get; set; } = "PharmacyAdmin"; // PharmacyAdmin, PharmacyUser, SuperAdmin
}
