namespace Opas.Infrastructure.Persistence.Ref;

public sealed class ProductRef
{
    public int Id { get; set; }
    public string Gtin { get; set; } = null!;        // 869… (13 hane olabilir)
    public string Name { get; set; } = null!;        // Ürün adı (TR)
    public string? Form { get; set; }                // Tablet/Şurup vb.
    public string? Strength { get; set; }            // 500 mg vb.
    public string? Manufacturer { get; set; }        // Firma
    public bool IsActive { get; set; } = true;
}
