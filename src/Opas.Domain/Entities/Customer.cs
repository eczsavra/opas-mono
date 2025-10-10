namespace Opas.Domain.Entities;

/// <summary>
/// Hasta/Müşteri entity - Tenant-specific
/// Bir hasta birden fazla eczanenin müşterisi olabilir (farklı tenant'larda aynı global_patient_id)
/// </summary>
public class Customer
{
    /// <summary>
    /// Tenant-specific ID (human-readable)
    /// Format: HAS-{initials}-{GLN7}-{sequence}
    /// Örnek: HAS-AY-1530144-0000024
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Global Patient ID (cross-tenant tracking)
    /// Format:
    /// - TC varsa: HAS-{TC_NO} (örn: HAS-12345678901)
    /// - Yabancı: HAS-F-{PASSPORT} (örn: HAS-F-AB1234567)
    /// - Bebek: HAS-P-{PARENT_TC}-{YYYYMMDD} (örn: HAS-P-98765432109-20251009)
    /// </summary>
    public string GlobalPatientId { get; set; } = string.Empty;

    /// <summary>
    /// Müşteri tipi: INDIVIDUAL, FOREIGN, INFANT
    /// </summary>
    public string CustomerType { get; set; } = "INDIVIDUAL";

    // Kimlik Bilgileri
    public string? TcNo { get; set; }
    public string? PassportNo { get; set; }

    // Ebeveyn Bilgileri (bebek için)
    public string? MotherTc { get; set; }
    public string? FatherTc { get; set; }
    
    // Vasi/Veli Bilgileri (18 yaş altı için - komşu, akraba, kurum vs. olabilir)
    public string? GuardianTc { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianRelation { get; set; } // Anne, Baba, Amca, Komşu, Kurum vs.

    // Kişisel Bilgiler
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public int? BirthYear { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }

    // Adres
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public string? Street { get; set; }
    public string? BuildingNo { get; set; }
    public string? ApartmentNo { get; set; }

    // Acil Durum
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }

    // Meta
    public string? Notes { get; set; }
    public bool KvkkConsent { get; set; }
    public DateTime? KvkkConsentDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

