using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Opas.Infrastructure.Persistence.Ref;

namespace Opas.Infrastructure.Persistence.Seed;

public static class PublicSeeder
{
    public static async Task EnsureCreatedAndSeedAsync(PublicDbContext db, IHostEnvironment env)
    {
        if (!env.IsDevelopment()) return;         // sadece Development
        // DB bağlantısı varsa çalışır; yoksa DI'ya eklenmemiştir zaten.
        await db.Database.EnsureCreatedAsync();

        if (await db.Products.AnyAsync()) return;

        var items = new[]
        {
            new ProductRef { Gtin = "8690000000001", Name = "Paracetamol",  Form = "Tablet", Strength = "500 mg",  Manufacturer = "OPAS Ref", IsActive = true },
            new ProductRef { Gtin = "8690000000002", Name = "Ibuprofen",    Form = "Tablet", Strength = "400 mg",  Manufacturer = "OPAS Ref", IsActive = true },
            new ProductRef { Gtin = "8690000000003", Name = "Aspirin",      Form = "Tablet", Strength = "100 mg",  Manufacturer = "OPAS Ref", IsActive = true },
            new ProductRef { Gtin = "8690000000004", Name = "Klorheksidin", Form = "Solüsyon", Strength = "0.2%", Manufacturer = "OPAS Ref", IsActive = true },
        };

        await db.Products.AddRangeAsync(items);
        await db.SaveChangesAsync();
    }
}
