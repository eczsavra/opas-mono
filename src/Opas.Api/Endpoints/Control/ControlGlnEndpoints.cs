using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Api.Control;
using Opas.Infrastructure.Persistence;
using Opas.Shared.ControlPlane;
using Opas.Shared.Validation;
using System.Text.Json;
using Opas.Shared.MultiTenancy;
using Opas.Infrastructure.MultiTenancy;

namespace Opas.Api.Endpoints.Control;

public static class ControlGlnEndpoints
{
    public static void MapControlGlnEndpoints(this WebApplication app)
    {
        // 1) GLN doğrula (format)
        app.MapGet("/control/gln/validate", (string? value) =>
        {
            if (string.IsNullOrWhiteSpace(value))
                return Results.BadRequest(new { ok = false, error = "gln required" });

            return Gln.IsValid(value)
                ? Results.Ok(new { ok = true, gln = value })
                : Results.BadRequest(new { ok = false, gln = value, error = "gln must start with 868 in TR and be 13 digits" });
        });

        // GET /control/gln/exists?value=868...
        app.MapGet("/control/gln/exists", async (
            [FromQuery] string? value,
            ControlPlaneDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(value) || !Gln.IsValid(value))
                return Results.BadRequest(new { ok = false, error = "invalid gln format" });

            var v = value.Trim();
            // unique index zaten var (Gln), bu çok hızlı çalışacak
            var exists = await db.GlnRegistry.AsNoTracking()
                            .AnyAsync(x => x.Gln == v);

            return Results.Ok(new { ok = true, exists });
        })
        .WithName("ControlGlnExists");

        
        // 2) GLN ara (q, page, size)
        app.MapGet("/control/gln/search", async (
            ControlPlaneDbContext db,
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20
            ) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new { ok = false, error = "q required" });

            page = Math.Max(1, page);
            size = Math.Clamp(size, 1, 100);

            // tablo garanti: EnsureCreated ilk kullanımda
            await db.Database.EnsureCreatedAsync();

            var baseQuery = db.GlnRegistry.AsNoTracking()
                .Where(x =>
                    EF.Functions.ILike(x.CompanyName ?? "", $"%{q}%") ||
                    EF.Functions.ILike(x.City ?? "", $"%{q}%") ||
                    EF.Functions.ILike(x.Town ?? "", $"%{q}%") ||
                    x.Gln.Contains(q));

            var total = await baseQuery.CountAsync();
            var items = await baseQuery
                .OrderBy(x => x.CompanyName).ThenBy(x => x.Gln)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(x => new
                {
                    x.Gln,
                    x.CompanyName,
                    x.City,
                    x.Town,
                    x.ImportedAtUtc
                })
                .ToListAsync();

            return Results.Ok(new { ok = true, page, size, total, items });
        });

        // 3) GLN getir (tek kayıt) – regex: 13 hane
        app.MapGet("/control/gln/{gln:regex(^\\d{{13}}$)}", async (string gln, ControlPlaneDbContext db) =>
        {
            await db.Database.EnsureCreatedAsync();

            var rec = await db.GlnRegistry.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Gln == gln);

            return rec is null
                ? Results.NotFound(new { ok = false, error = "not found", gln })
                : Results.Ok(new
                {
                    rec.Gln,
                    rec.CompanyName,
                    rec.City,
                    rec.Town,
                    rec.ImportedAtUtc
                });
        });

        // 4) GLN upsert
        app.MapPost("/control/gln/upsert", async ([FromBody] GlnUpsertDto body, ControlPlaneDbContext db) =>
        {
            if (!Gln.IsValid(body.Gln))
                return Results.BadRequest(new { ok = false, gln = body.Gln, error = "invalid gln format" });

            await db.Database.EnsureCreatedAsync();

            var existing = await db.GlnRegistry.SingleOrDefaultAsync(x => x.Gln == body.Gln);
            if (existing is null)
            {
                db.GlnRegistry.Add(new GlnRecord
                {
                    Gln = body.Gln,
                    CompanyName = body.CompanyName,
                    City = body.City,
                    Town = body.Town,
                    Source = body.Source,
                    ImportedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                return Results.Created($"/control/gln/{body.Gln}", new { ok = true, created = true, gln = body.Gln });
            }
            else
            {
                existing.CompanyName = body.CompanyName;
                existing.City = body.City;
                existing.Town = body.Town;
                existing.Source = body.Source;
                existing.ImportedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.Ok(new { ok = true, updated = true, gln = body.Gln });
            }
        });

        // POST /control/gln/import/its
        app.MapPost("/control/gln/import/its", async (
            [FromServices] ControlPlaneDbContext db,
            [FromServices] ITokenProvider tokenProvider,
            [FromServices] ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var token = await tokenProvider.GetTokenAsync("ITS-Access", ct);
                if (string.IsNullOrWhiteSpace(token))
                {
                    return Results.Problem("ITS token alınamadı", statusCode: 500);
                }

                var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var body = new { stakeholderType = "eczane", getAll = true };
                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var resp = await http.PostAsync("https://its2.saglik.gov.tr/reference/app/stakeholder/", content, ct);
                resp.EnsureSuccessStatusCode();

                var jsonResp = await resp.Content.ReadAsStringAsync(ct);
                logger.LogInformation("ITS Response: {Response}", jsonResp.Substring(0, Math.Min(500, jsonResp.Length)));
                
                // Parse response and extract array
                JsonElement[] list;
                var responseObj = JsonSerializer.Deserialize<JsonElement>(jsonResp);
                
                // Try different possible array properties
                if (responseObj.ValueKind == JsonValueKind.Array)
                {
                    list = responseObj.EnumerateArray().ToArray();
                }
                else if (responseObj.TryGetProperty("companyList", out var companyListProp) && companyListProp.ValueKind == JsonValueKind.Array)
                {
                    list = companyListProp.EnumerateArray().ToArray();
                }
                else if (responseObj.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
                {
                    list = dataProp.EnumerateArray().ToArray();
                }
                else if (responseObj.TryGetProperty("stakeholders", out var stakeholdersProp) && stakeholdersProp.ValueKind == JsonValueKind.Array)
                {
                    list = stakeholdersProp.EnumerateArray().ToArray();
                }
                else if (responseObj.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Array)
                {
                    list = resultProp.EnumerateArray().ToArray();
                }
                else
                {
                    logger.LogError("ITS response format beklenmeyen. Response: {Response}", jsonResp);
                    return Results.Problem("ITS response format beklenmeyen", statusCode: 500);
                }
                
                if (list == null || list.Length == 0)
                {
                    return Results.Ok(new { imported = 0, message = "Liste boş" });
                }

                var upserted = 0;
                foreach (var item in list)
                {
                    if (!item.TryGetProperty("gln", out var glnEl) || glnEl.ValueKind != JsonValueKind.String)
                        continue;
                    var gln = glnEl.GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(gln))
                        continue;

                    var rec = await db.GlnRegistry.FirstOrDefaultAsync(x => x.Gln == gln, ct);
                    if (rec == null)
                    {
                        rec = new GlnRecord { Gln = gln };
                        db.GlnRegistry.Add(rec);
                    }

                    rec.CompanyName = item.TryGetProperty("companyName", out var cn) ? cn.GetString() : null;
                    rec.Authorized = item.TryGetProperty("authorized", out var auth) ? auth.GetString() : null;
                    rec.Email = item.TryGetProperty("email", out var em) ? em.GetString() : null;
                    rec.Phone = item.TryGetProperty("phone", out var ph) ? ph.GetString() : null;
                    rec.City = item.TryGetProperty("city", out var ci) ? ci.GetString() : null;
                    rec.Town = item.TryGetProperty("town", out var to) ? to.GetString() : null;
                    rec.Address = item.TryGetProperty("address", out var ad) ? ad.GetString() : null;
                    rec.Active = item.TryGetProperty("active", out var ac) ? ac.GetBoolean() : null;
                    rec.ImportedAtUtc = DateTime.UtcNow;

                    upserted++;
                }

                await db.SaveChangesAsync(ct);
                logger.LogInformation("ITS GLN import completed. Upserted: {Count}", upserted);

                return Results.Ok(new { imported = upserted, message = "Başarılı" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ITS GLN import failed");
                return Results.Problem("Import başarısız", statusCode: 500);
            }
        })
        .WithName("ImportItsGln")
        .WithOpenApi();

        // DEBUG: Test ITS response format
        app.MapPost("/control/gln/debug-its", async (
            [FromServices] ITokenProvider tokenProvider,
            [FromServices] ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var token = await tokenProvider.GetTokenAsync("ITS-Access", ct);
                if (string.IsNullOrWhiteSpace(token))
                {
                    return Results.Problem("ITS token alınamadı", statusCode: 500);
                }

                var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var body = new { stakeholderType = "eczane", getAll = true };
                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var resp = await http.PostAsync("https://its2.saglik.gov.tr/reference/app/stakeholder/", content, ct);
                resp.EnsureSuccessStatusCode();

                var jsonResp = await resp.Content.ReadAsStringAsync(ct);
                
                return Results.Ok(new { 
                    statusCode = (int)resp.StatusCode,
                    responseLength = jsonResp.Length,
                    responsePreview = jsonResp.Substring(0, Math.Min(1000, jsonResp.Length)),
                    isArray = jsonResp.TrimStart().StartsWith("["),
                    isObject = jsonResp.TrimStart().StartsWith("{")
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ITS debug failed");
                return Results.Problem("Debug başarısız", statusCode: 500);
            }
        })
        .WithName("DebugItsResponse")
        .WithOpenApi();
    }
}
