namespace Opas.Shared.ControlPlane;

public class GlnRecord
{
    public int Id { get; set; }
    public string Gln { get; set; } = default!;   // 13 hane, 868 ile başlar

    // ITS response: companyName
    public string? CompanyName { get; set; }

    // ITS response: authorized (yetkili kişi)
    public string? Authorized { get; set; }

    // ITS response: email
    public string? Email { get; set; }

    // ITS response: phone
    public string? Phone { get; set; }

    // ITS response: city
    public string? City { get; set; }

    // ITS response: town (ilçe)
    public string? Town { get; set; }

    // ITS response: address
    public string? Address { get; set; }

    // ITS response: active (True, False değerini almaktadır)
    public bool? Active { get; set; }

    // Source of the GLN record (e.g., "its-import", "manual")
    public string? Source { get; set; }

    // Import zamanı
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
}
