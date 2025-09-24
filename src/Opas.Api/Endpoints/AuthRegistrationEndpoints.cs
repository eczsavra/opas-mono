using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Shared.Validation;
using System.ComponentModel.DataAnnotations;

namespace Opas.Api.Endpoints;

public static class AuthRegistrationEndpoints
{
    
    public sealed record RegisterStartRequest([Required] string Gln);
    public sealed record RegisterStartResponse(bool ok, string? error, string? gln, string? token);
    
    
    public static IEndpointRouteBuilder MapAuthRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/register/validate-gln", async (
            [FromQuery] string? value,
            ControlPlaneDbContext db) =>
        {
            // 1) boş / trim
            var gln = value?.Trim();
            if (string.IsNullOrWhiteSpace(gln))
                return Results.BadRequest(new { ok = false, error = "gln required" });

            // 2) format: GLN validation
            if (!Gln.IsValid(gln))
                return Results.BadRequest(new { ok = false, gln, error = "invalid gln format (must start with 868 and be 13 digits)" });

            // 3) veritabanında arama (birebir eşleşme)
            var rec = await db.GlnRegistry
                .AsNoTracking()
                .Where(x => x.Gln == gln)
                .Select(x => new { x.Gln, x.CompanyName, x.City, x.Town })
                .SingleOrDefaultAsync();

            if (rec is null)
                return Results.Ok(new { ok = true, found = false, gln, message = "gln not found in registry" });

            return Results.Ok(new { ok = true, found = true, rec.Gln, rec.CompanyName, rec.City, rec.Town });
        });

        // GET /auth/register/validate-username?value=...
        app.MapGet("/auth/register/validate-username", ([FromQuery] string? value) =>
        {
            // Kurallar:
            // - 4..32 karakter
            // - Harfle başlamalı
            // - Sadece harf, rakam, nokta, altçizgi
            // - '..' ve '__' gibi art arda nokta/altçizgi yok
            // - Nokta/altçizgi ile bitemez
            // - Bazı rezerve kelimeler yasak

            var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "admin","root","owner","system","support","help","opas","superadmin"
            };

            if (string.IsNullOrWhiteSpace(value))
                return Results.BadRequest(new { ok = false, error = "username required" });

            var u = value.Trim();

            if (u.Length < 4 || u.Length > 32)
                return Results.BadRequest(new { ok = false, username = u, error = "length must be 4-32" });

            if (!char.IsLetter(u[0]))
                return Results.BadRequest(new { ok = false, username = u, error = "must start with a letter" });

            // Geçerli karakterler
            foreach (var ch in u)
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '.' || ch == '_'))
                    return Results.BadRequest(new { ok = false, username = u, error = "invalid characters" });
            }

            // Son karakter kontrolü
            if (u.EndsWith('.') || u.EndsWith('_'))
                return Results.BadRequest(new { ok = false, username = u, error = "cannot end with '.' or '_'" });

            // Art arda nokta / altçizgi
            if (u.Contains("..") || u.Contains("__") || u.Contains("._") || u.Contains("_."))
                return Results.BadRequest(new { ok = false, username = u, error = "no consecutive separators" });

            if (reserved.Contains(u))
                return Results.BadRequest(new { ok = false, username = u, error = "reserved username" });

            // (İLERİDE) tekillik kontrolü ControlPlane’de yapılacak – TODO
            return Results.Ok(new { ok = true, username = u });
        });



        // GET /auth/register/validate-phone?value=...  (TR E164 uyumlu)
        app.MapGet("/auth/register/validate-phone", (string? value) =>
        {
            if (string.IsNullOrWhiteSpace(value))
                return Results.BadRequest(new { ok = false, error = "phone required" });

            var raw = value.Trim();

            // kabul ettiklerimiz:
            // - başında + ve rakamlar (E.164)  -> ör: +905321234567
            // - TR için yerel yazım: 05XXXXXXXXX veya 5XXXXXXXXX -> normalize edip +90 ile döndürüyoruz
            string normalized;

            if (System.Text.RegularExpressions.Regex.IsMatch(raw, @"^\+?[1-9]\d{7,14}$"))
            {
                // + ile başlıyorsa direkt kullan; yoksa + ekle
                normalized = raw.StartsWith("+") ? raw : "+" + raw;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(raw, @"^0?5\d{9}$"))
            {
                // TR mobil: 05XXXXXXXXX ya da 5XXXXXXXXX -> +90 ile normalize
                var digits = raw.StartsWith("0") ? raw[1..] : raw;
                normalized = $"+90{digits}";
            }
            else
            {
                return Results.BadRequest(new
                {
                    ok = false,
                    phone = raw,
                    error = "invalid phone format (use +905xxxxxxxxx or 05xxxxxxxxx)"
                });
            }

            return Results.Ok(new { ok = true, phone = normalized });
        });

        app.MapPost("/auth/register/start", async (
            [FromBody] RegisterStartRequest body,
            [FromServices] ControlPlaneDbContext db) =>
        {
            // 1) GLN basit kontrol: 13 hane & TR için 868 prefix
            if (string.IsNullOrWhiteSpace(body.Gln) || body.Gln.Length != 13 || !body.Gln.StartsWith("868"))
                return Results.BadRequest(new RegisterStartResponse(false, "invalid gln format (868 + 13 digits)", body.Gln, null));

            // 2) Bizde var mı? (gln_registry)
            var exists = await db.GlnRegistry.AsNoTracking().AnyAsync(x => x.Gln == body.Gln);
            if (!exists)
                return Results.NotFound(new RegisterStartResponse(false, "gln not found in registry", body.Gln, null));

            // 3) Token üret (şimdilik sadece geri döndürüyoruz; ileride cache/DB’ye koyacağız)
            var token = Guid.NewGuid().ToString("N");

            return Results.Ok(new RegisterStartResponse(true, null, body.Gln, token));
        })
        .WithName("AuthRegisterStart");


        return app;
    }
}
