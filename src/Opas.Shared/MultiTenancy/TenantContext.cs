namespace Opas.Shared.MultiTenancy;

/// <summary>
/// Request scope’unda taşınacak basit tenant bilgisi.
/// Şimdilik sadece veri sınıfı; DI/middleware yok.
/// </summary>
public sealed class TenantContext
{
    public string TenantId { get; }

    // İleride subdomain, region vs. eklenebilir
    public string? Region { get; }

    public TenantContext(string tenantId, string? region = null)
    {
        tenantId = (tenantId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("tenantId is required", nameof(tenantId));

        TenantId = tenantId;
        Region = region?.Trim();
    }

    public override string ToString() => Region is null ? TenantId : $"{TenantId}@{Region}";
}
