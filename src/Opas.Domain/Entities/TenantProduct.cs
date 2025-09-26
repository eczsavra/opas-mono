using Opas.Domain.Primitives;
using Opas.Domain.ValueObjects;

namespace Opas.Domain.Entities;

/// <summary>
/// Tenant-specific product entity - her eczanenin kendi ürün listesi
/// CentralProduct ile aynı yapıda (tenant customization ile)
/// </summary>
public class TenantProduct : BaseEntity
{
    /// <summary>
    /// GTIN - Küresel Ticari Ürün Numarası (ITS'den gelen)
    /// </summary>
    public string Gtin { get; set; } = string.Empty;

    /// <summary>
    /// İlaç adı (değiştirebilir)
    /// </summary>
    public string DrugName { get; set; } = string.Empty;

    /// <summary>
    /// Üretici GLN (ITS'den gelen)
    /// </summary>
    public string? ManufacturerGln { get; set; }

    /// <summary>
    /// Üretici adı (ITS'den gelen)
    /// </summary>
    public string? ManufacturerName { get; set; }

    /// <summary>
    /// Satış fiyatı (değiştirebilir)
    /// </summary>
    public decimal Price { get; set; } = 0m;

    /// <summary>
    /// Fiyat değişiklik geçmişi (JSON formatında)
    /// </summary>
    public List<PriceHistoryEntry> PriceHistory { get; set; } = new();

    /// <summary>
    /// ITS'den gelen aktif/pasif durumu
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// ITS'den import edilip edilmediği
    /// </summary>
    public bool IsImported { get; set; } = false;

    /// <summary>
    /// Son ITS sync zamanı
    /// </summary>
    public DateTime? LastItsSyncAt { get; set; }

    /// <summary>
    /// ITS'den gelen raw data (JSON)
    /// </summary>
    public string? ItsRawData { get; set; }

    /// <summary>
    /// Kaydı oluşturan kullanıcı
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Kaydı güncelleyen kullanıcı
    /// </summary>
    public string? UpdatedBy { get; set; }
}
