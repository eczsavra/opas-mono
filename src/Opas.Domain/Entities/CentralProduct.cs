using Opas.Domain.Primitives;
using Opas.Domain.ValueObjects;
using System.Text.Json;

namespace Opas.Domain.Entities;

/// <summary>
/// Merkezi DB'deki ürün tablosu - ITS'den gelen ana ürün verisi
/// Her tenant bu tablodan beslenecek
/// </summary>
public class CentralProduct : BaseEntity
{
    /// <summary>
    /// GTIN - Global Trade Item Number (Unique)
    /// </summary>
    public string Gtin { get; set; } = string.Empty;

    /// <summary>
    /// İlaç adı - ITS'den gelen orijinal isim
    /// </summary>
    public string DrugName { get; set; } = string.Empty;

    /// <summary>
    /// Üretici GLN kodu
    /// </summary>
    public string? ManufacturerGln { get; set; }

    /// <summary>
    /// Üretici adı
    /// </summary>
    public string? ManufacturerName { get; set; }

    /// <summary>
    /// Merkezi fiyat (referans fiyat)
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
