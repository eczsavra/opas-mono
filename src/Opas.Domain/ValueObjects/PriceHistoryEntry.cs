namespace Opas.Domain.ValueObjects;

/// <summary>
/// Fiyat tarihçesi entry modeli
/// </summary>
public record PriceHistoryEntry
{
    /// <summary>
    /// Fiyat değeri
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Değişiklik tarihi
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// Değişikliği yapan kullanıcı
    /// </summary>
    public string ChangedBy { get; init; } = string.Empty;

    /// <summary>
    /// Değişiklik sebebi
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Değişiklik tipi (ITS_SYNC, MANUAL_UPDATE, BULK_UPDATE)
    /// </summary>
    public string ChangeType { get; init; } = string.Empty;
}
