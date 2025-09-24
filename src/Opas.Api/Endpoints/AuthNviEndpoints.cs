using Microsoft.AspNetCore.Mvc;
using Opas.Infrastructure.Services;

namespace Opas.Api.Endpoints;

public static class AuthNviEndpoints
{
    public static IEndpointRouteBuilder MapAuthNviEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /auth/nvi/validate - NVI kimlik doğrulama
        app.MapPost("/auth/nvi/validate", async (
            [FromBody] NviValidationRequest request,
            [FromServices] NviService nviService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(request.TcNumber) || request.TcNumber.Length != 11)
                {
                    return Results.BadRequest(new { ok = false, error = "TC kimlik numarası 11 haneli olmalıdır" });
                }

                if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Length < 2)
                {
                    return Results.BadRequest(new { ok = false, error = "Ad en az 2 karakter olmalıdır" });
                }

                if (string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Length < 2)
                {
                    return Results.BadRequest(new { ok = false, error = "Soyad en az 2 karakter olmalıdır" });
                }

                if (request.BirthYear < 1940 || request.BirthYear > DateTime.Now.Year - 18)
                {
                    return Results.BadRequest(new { ok = false, error = "Geçerli bir doğum yılı giriniz" });
                }

                // NVI doğrulama
                var result = await nviService.ValidateIdentityAsync(
                    request.TcNumber.Trim(),
                    request.FirstName.Trim(),
                    request.LastName.Trim(),
                    request.BirthYear
                );

                if (result.IsValid)
                {
                    logger.LogInformation("NVI kimlik doğrulama başarılı: TC={TC}", request.TcNumber);
                    
                    return Results.Ok(new
                    {
                        ok = true,
                        message = "Kimlik bilgileri NVI tarafından doğrulandı",
                        data = new
                        {
                            tcNumber = result.TcNumber,
                            firstName = result.FirstName,
                            lastName = result.LastName,
                            birthYear = result.BirthYear,
                            validatedAt = result.ResponseTime
                        }
                    });
                }
                else
                {
                    logger.LogWarning("NVI kimlik doğrulama başarısız: TC={TC}, Error={Error}", 
                        request.TcNumber, result.ErrorMessage);
                    
                    return Results.BadRequest(new
                    {
                        ok = false,
                        error = result.ErrorMessage ?? "Kimlik bilgileri doğrulanamadı",
                        details = "NVI kayıtları ile eşleşmiyor"
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "NVI kimlik doğrulama API hatası");
                
                return Results.Problem(
                    title: "Kimlik Doğrulama Hatası",
                    detail: "Kimlik doğrulama servisinde bir hata oluştu",
                    statusCode: 500);
            }
        })
        .WithName("ValidateNviIdentity")
        .WithOpenApi()
        .WithTags("Authentication", "NVI")
        .WithSummary("NVI Kimlik Doğrulama")
        .WithDescription("T.C. Kimlik No, ad, soyad ve doğum yılı ile NVI kimlik doğrulama yapar");

        return app;
    }
}

/// <summary>
/// NVI kimlik doğrulama request modeli
/// </summary>
public record NviValidationRequest
{
    /// <summary>TC Kimlik Numarası (11 haneli)</summary>
    public string TcNumber { get; init; } = default!;
    
    /// <summary>Ad</summary>
    public string FirstName { get; init; } = default!;
    
    /// <summary>Soyad</summary>
    public string LastName { get; init; } = default!;
    
    /// <summary>Doğum Yılı</summary>
    public int BirthYear { get; init; }
}
