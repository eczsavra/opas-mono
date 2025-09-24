using System;
using System.ComponentModel.DataAnnotations;   // <-- ekle

namespace Opas.Shared.ControlPlane
{
    public class TenantRecord
    {
        [Key]                                     // <-- PK: TenantId (bizim verdiğimiz unique ID)
        [MaxLength(50)]
        public string TenantId { get; set; } = default!; // OPAS_TNT_000001 gibi
        
        [Required]
        [MaxLength(13)]
        public string PharmacistGln { get; set; } = default!; // Eczacının GLN'si (değişebilir)

        [Required, MaxLength(200)]
        public string PharmacyName { get; set; } = default!;
        
        // Eczane Sicil No (18341699 gibi) - eczaneye özel
        [MaxLength(20)]
        public string? PharmacyRegistrationNo { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? District { get; set; }

        [Required]
        public string TenantConnectionString { get; set; } = default!;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
