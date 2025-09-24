using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Opas.Infrastructure.Persistence;
using Opas.Shared.ControlPlane;
using Opas.Api.Control;

namespace Opas.Api.Endpoints.Control;

public static class ControlTenantEndpoints
{
    // TenantId generator - unique ID independent of GLN
    private static string GenerateTenantId()
    {
        // Format: OPAS_TNT_XXXXXX (6 random digits)
        var random = new Random();
        var number = random.Next(100000, 999999);
        return $"OPAS_TNT_{number:D6}";
    }

    public static IEndpointRouteBuilder MapControlTenantEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /control/tenant
        app.MapPost("/control/tenant", async (TenantUpsertDto body, IServiceProvider sp) =>
        {
            var scopeFactory = sp.GetService<IServiceScopeFactory>();
            if (scopeFactory is null)
                return Results.Problem("ControlPlaneDbContext not registered", statusCode: 503);

            if (string.IsNullOrWhiteSpace(body.PharmacistGln) ||
                string.IsNullOrWhiteSpace(body.PharmacyName) ||
                string.IsNullOrWhiteSpace(body.TenantConnectionString))
                return Results.BadRequest(new { error = "pharmacistGln / pharmacyName / tenantConnectionString required" });

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            await db.Database.EnsureCreatedAsync();

            TenantRecord? existing = null;
            bool isUpdate = false;

            // Check if this is an update (TenantId provided) or create (no TenantId)
            if (!string.IsNullOrWhiteSpace(body.TenantId))
            {
                existing = await db.Tenants.AsTracking().FirstOrDefaultAsync(x => x.TenantId == body.TenantId);
                isUpdate = existing is not null;
            }

            if (!isUpdate)
            {
                // CREATE: Generate new TenantId and ensure it's unique
                string newTenantId;
                do
                {
                    newTenantId = GenerateTenantId();
                } while (await db.Tenants.AnyAsync(x => x.TenantId == newTenantId));

                var newTenant = new TenantRecord
                {
                    TenantId = newTenantId,
                    PharmacistGln = body.PharmacistGln.Trim(),
                    PharmacyName = body.PharmacyName.Trim(),
                    PharmacyRegistrationNo = body.PharmacyRegistrationNo?.Trim(),
                    City = body.City?.Trim(),
                    District = body.District?.Trim(),
                    TenantConnectionString = body.TenantConnectionString.Trim(),
                    Status = "Active"
                };

                db.Tenants.Add(newTenant);
                await db.SaveChangesAsync();
                return Results.Created($"/control/tenant/{newTenantId}", new { ok = true, created = true, tenantId = newTenantId, pharmacistGln = body.PharmacistGln });
            }
            else
            {
                // UPDATE: Update existing tenant
                existing!.PharmacistGln = body.PharmacistGln.Trim();
                existing.PharmacyName = body.PharmacyName.Trim();
                existing.PharmacyRegistrationNo = body.PharmacyRegistrationNo?.Trim();
                existing.City = body.City?.Trim();
                existing.District = body.District?.Trim();
                existing.TenantConnectionString = body.TenantConnectionString.Trim();
                existing.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                return Results.Ok(new { ok = true, updated = true, tenantId = existing.TenantId, pharmacistGln = body.PharmacistGln });
            }
        })
        .WithName("ControlPlaneUpsertTenant");

        return app;
    }
}
