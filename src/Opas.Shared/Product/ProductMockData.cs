namespace Opas.Shared.Product;

/// <summary>
/// Mock data generator for products
/// 100 mock ilaç - kolayca tanınabilir ve silinebilir
/// </summary>
public static class ProductMockData
{
    private static readonly Random _random = new();

    // İlaç tipleri ve dozajları
    private static readonly string[] DrugPrefixes = new[]
    {
        "ABC", "DJN", "RTK", "PLX", "MKR", "ZYN", "FLX", "QRS", "VNX", "KPL",
        "BRT", "GLN", "HXR", "JMK", "LPT", "NVR", "PQR", "STX", "TRZ", "WXY"
    };

    private static readonly string[] DosageForms = new[]
    {
        "TABLET", "KAPSÜL", "ŞURUP", "AMPUL", "FLAKON", "KREM", "MERHEM", "DAMLA", "SPREY", "SUPOZITUAR"
    };

    private static readonly int[] Strengths = new[]
    {
        5, 10, 20, 25, 50, 100, 200, 250, 500, 1000
    };

    private static readonly string[] Units = new[]
    {
        "MG", "ML", "G", "MCG"
    };

    // Üretici firmaları
    private static readonly string[] Manufacturers = new[]
    {
        "MOCK İLAÇ SANAYİ A.Ş.",
        "TEST PHARMA ENDÜSTRİSİ",
        "DEMO ECZACIBAŞI",
        "SAMPLE ECZACILIK",
        "DENEME SAĞLIK ÜRÜNLERİ"
    };

    public static List<CreateProductRequest> Generate100Products()
    {
        var products = new List<CreateProductRequest>();
        var usedBarcodes = new HashSet<string>();

        for (int i = 0; i < 100; i++)
        {
            var prefix = DrugPrefixes[_random.Next(DrugPrefixes.Length)];
            var strength = Strengths[_random.Next(Strengths.Length)];
            var unit = Units[_random.Next(Units.Length)];
            var form = DosageForms[_random.Next(DosageForms.Length)];
            var manufacturer = Manufacturers[_random.Next(Manufacturers.Length)];

            // Unique barcode (9999 ile başlar)
            string barcode;
            do
            {
                barcode = $"9999{_random.Next(10000000, 99999999)}"; // 9999XXXXXXXX (12 digit)
            } while (usedBarcodes.Contains(barcode));
            usedBarcodes.Add(barcode);

            // İsim oluştur
            var drugName = $"{prefix} {strength} {unit} {form}";
            
            // Ambalaj bilgisi ekle (bazılarında)
            if (_random.Next(100) < 60) // %60 ihtimalle
            {
                var packageSizes = new[] { 10, 20, 30, 50, 100 };
                var packageSize = packageSizes[_random.Next(packageSizes.Length)];
                drugName += $" ({packageSize} ADET)";
            }

            // Fiyat (10 TL - 500 TL arası)
            var price = Math.Round(_random.Next(1000, 50000) / 100.0m, 2);

            // Kategori
            var category = _random.Next(100) < 70 ? "DRUG" : "OTC"; // %70 DRUG, %30 OTC

            products.Add(new CreateProductRequest
            {
                Gtin = barcode,
                DrugName = drugName,
                ManufacturerName = manufacturer,
                ManufacturerGln = $"999900000{_random.Next(1000, 9999)}", // Mock GLN
                Price = price,
                Category = category,
                HasDatamatrix = _random.Next(100) < 80, // %80 karekodlu
                RequiresExpiryTracking = category == "DRUG", // İlaçlar için zorunlu
                IsControlled = _random.Next(100) < 10, // %10 yeşil reçete
                IsActive = true
            });
        }

        return products;
    }

    // Price history oluştur (3-6 ay geriye)
    private static string GeneratePriceHistory(decimal currentPrice)
    {
        var history = new List<string>();
        var monthsBack = _random.Next(3, 7);
        var price = currentPrice;

        for (int i = monthsBack; i > 0; i--)
        {
            var date = DateTime.UtcNow.AddMonths(-i);
            var priceChange = _random.Next(-20, 30); // ±%20-30 değişim
            price = Math.Max(5, price + (price * priceChange / 100));
            price = Math.Round(price, 2);
            
            history.Add($"{date:yyyy-MM-dd}:{price}");
        }

        // En son fiyat
        history.Add($"{DateTime.UtcNow:yyyy-MM-dd}:{currentPrice}");

        return string.Join(",", history);
    }
}

public record CreateProductRequest
{
    public required string Gtin { get; init; }
    public required string DrugName { get; init; }
    public required string ManufacturerName { get; init; }
    public required string ManufacturerGln { get; init; }
    public required decimal Price { get; init; }
    public required string Category { get; init; }
    public required bool HasDatamatrix { get; init; }
    public required bool RequiresExpiryTracking { get; init; }
    public required bool IsControlled { get; init; }
    public required bool IsActive { get; init; }
}

