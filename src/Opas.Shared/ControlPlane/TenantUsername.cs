using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.ControlPlane
{
    /// <summary>
    /// Control Plane DB'deki tenants_usernames tablosu - unique username kontrolü için
    /// </summary>
    public class TenantUsername
    {
        [Key]
        [MaxLength(50)]
        public string TId { get; set; } = default!; // tenants tablosundan

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = default!;
    }
}
