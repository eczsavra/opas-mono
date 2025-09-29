using System;
using System.ComponentModel.DataAnnotations;

namespace Opas.Shared.ControlPlane
{
    /// <summary>
    /// Control Plane DB'deki tenants tablosu - yeni yapı
    /// </summary>
    public class Tenant
    {
        [Key]
        [MaxLength(50)]
        public string TId { get; set; } = default!; // TNT_"glnno" format

        [Required]
        [MaxLength(13)]
        public string Gln { get; set; } = default!;

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "eczane";

        [MaxLength(200)]
        public string? EczaneAdi { get; set; }

        [MaxLength(100)]
        public string? Ili { get; set; }

        [MaxLength(100)]
        public string? Ilcesi { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        [MaxLength(100)]
        public string Ad { get; set; } = default!;

        [Required]
        [MaxLength(100)]
        public string Soyad { get; set; } = default!;

        [MaxLength(11)]
        public string? TcNo { get; set; }

        public int? DogumYili { get; set; }

        public bool IsNviVerified { get; set; } = false;

        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = default!;

        public bool IsEmailVerified { get; set; } = false;

        [MaxLength(15)]
        public string? CepTel { get; set; }

        public bool IsCepTelVerified { get; set; } = false;

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = default!;

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = default!;

        public bool IsCompleted { get; set; } = false;

        public DateTime KayitOlusturulmaZamani { get; set; } = DateTime.UtcNow;

        public DateTime? KayitGuncellenmeZamani { get; set; }

        public DateTime? KayitSilinmeZamani { get; set; }
    }
}
