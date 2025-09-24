namespace Opas.Api.Control;

public sealed class TenantUpsertDto
{
    public string? TenantId { get; set; } // Our unique ID (optional for create, required for update)
    public string PharmacistGln { get; set; } = default!; // Pharmacist's GLN
    public string PharmacyName { get; set; } = default!;
    public string? PharmacyRegistrationNo { get; set; } // Eczane sicil no
    public string? City { get; set; }
    public string? District { get; set; }
    public string TenantConnectionString { get; set; } = default!;
}
