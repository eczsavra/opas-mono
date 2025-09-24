using Opas.Domain.Primitives;

namespace Opas.Domain.Entities;

/// <summary>
/// Saf domain entity (Infrastructure/EF'e dokunmaz).
/// PublicRef tarafındaki alanlarla uyumlu tutuldu.
/// </summary>
public sealed class Product : BaseEntity
{
    // Zorunlu alanlar
    public string Gtin { get; private set; } = default!;   // 869… barkod
    public string Name { get; private set; } = default!;   // ürün adı (TR)

    // İsteğe bağlı alanlar
    public string? Form { get; private set; }              // Tablet/Şurup
    public string? Strength { get; private set; }          // 500 mg vb.
    public string? Manufacturer { get; private set; }      // Firma

    public bool IsActive { get; private set; } = true;

    private Product() { } // EF/serializer için (domain dışı kullanmayacağız)

    private Product(string gtin, string name, string? form, string? strength, string? manufacturer)
    {
        Gtin = gtin;
        Name = name;
        Form = form;
        Strength = strength;
        Manufacturer = manufacturer;
    }

    // Factory
    public static Product Create(string gtin, string name, string? form = null, string? strength = null, string? manufacturer = null)
    {
        gtin = (gtin ?? string.Empty).Trim();
        name = (name ?? string.Empty).Trim();

        if (gtin.Length is < 8 or > 20) // GTIN/Barcode aralığı (esnek)
            throw new ArgumentException("gtin length invalid (8..20)", nameof(gtin));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name is required", nameof(name));

        return new Product(gtin, name, form?.Trim(), strength?.Trim(), manufacturer?.Trim());
    }

    // Davranışlar (basit, side-effect: UpdatedAtUtc güncellenir)
    public void Rename(string newName)
    {
        newName = (newName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("name is required", nameof(newName));

        if (newName == Name) return;

        Name = newName;
        MarkUpdated();
    }

    public void SetActive(bool active)
    {
        if (IsActive == active) return;
        IsActive = active;
        MarkUpdated();
    }

    public void UpdateDetails(string? form, string? strength, string? manufacturer)
    {
        var f = form?.Trim();
        var s = strength?.Trim();
        var m = manufacturer?.Trim();

        if (f == Form && s == Strength && m == Manufacturer) return;

        Form = f;
        Strength = s;
        Manufacturer = m;
        MarkUpdated();
    }
}
