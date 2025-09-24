using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.ControlPlane;

public class TokenStore
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;

    [Required]
    public string Token { get; set; } = default!;

    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
