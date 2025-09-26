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

        // Test veriler kaldırıldı - ITS'den gerçek ürün verisi gelecek
        // Gelecekte: ITS Product Sync burada çalışacak
    }
}
