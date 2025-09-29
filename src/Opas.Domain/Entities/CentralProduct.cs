using Opas.Domain.Primitives;

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
    /// ITS'den gelen aktif/pasif durumu
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// ITS'den import edilip edilmediği
    /// </summary>
    public bool Imported { get; set; } = false;
}
