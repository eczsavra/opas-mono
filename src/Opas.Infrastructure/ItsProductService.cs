// ItsProductService.cs
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Shared.ControlPlane;
using Opas.Shared.Logging;
using Opas.Infrastructure.Logging;

namespace Opas.Infrastructure;

/// <summary>
/// ITS'den ürün listesi çekme servisi
/// </summary>
public class ItsProductService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ItsProductService> _logger;
    private readonly IConfiguration _cfg;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOpasLogger _opasLogger;

    public ItsProductService(
        IHttpClientFactory httpFactory, 
        ILogger<ItsProductService> logger, 
        IConfiguration cfg, 
        IServiceProvider serviceProvider,
        IOpasLogger opasLogger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _cfg = cfg;
        _serviceProvider = serviceProvider;
        _opasLogger = opasLogger;
    }

    /// <summary>
    /// ITS'den ürün listesi modeli
    /// </summary>
    public record ItsProductResponse(
        [property: JsonPropertyName("drugList")] ItsProduct[] DrugList
    );

    public record ItsProduct(
        [property: JsonPropertyName("gtin")] string? Gtin,
        [property: JsonPropertyName("drugname")] string? DrugName,
        [property: JsonPropertyName("manufacturerGln")] string? ManufacturerGln,
        [property: JsonPropertyName("manufacturerName")] string? ManufacturerName,
        [property: JsonPropertyName("active")] bool Active,
        [property: JsonPropertyName("imported")] bool Imported
    );

    /// <summary>
    /// ITS'den ürün listesini çek
    /// </summary>
    public async Task<ItsProduct[]?> GetProductListAsync(CancellationToken ct = default)
    {
        var drugListUrl = _cfg["Integrations:Its:DrugListUrl"] ?? "https://its2.saglik.gov.tr/reference/app/drug/";

        try
        {
            // Önce token al
            var token = await GetValidTokenAsync(ct);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No valid ITS token available for product list fetch");
                _opasLogger.LogSystemEvent("ItsProductFetch", "Failed: No valid token", new { Url = drugListUrl });
                return null;
            }

            var client = _httpFactory.CreateClient("its");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // POST body - tüm ilaçları getir (aktif + pasif)
            var requestBody = new { getAll = true };
            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Fetching drug list from ITS: {Url}", drugListUrl);
            _opasLogger.LogSystemEvent("ItsProductFetch", "Started drug list fetch", new { Url = drugListUrl, GetAll = true });

            var response = await client.PostAsync(drugListUrl, content, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ITS product list fetch failed with status: {StatusCode}", response.StatusCode);
                _opasLogger.LogSystemEvent("ItsProductFetch", "Failed with HTTP error", new { 
                    StatusCode = (int)response.StatusCode,
                    Url = drugListUrl 
                });
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var productResponse = JsonSerializer.Deserialize<ItsProductResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (productResponse?.DrugList != null)
            {
                _logger.LogInformation("Successfully fetched {Count} drugs from ITS", productResponse.DrugList.Length);
                _opasLogger.LogSystemEvent("ItsProductFetch", "Successful", new { 
                    DrugCount = productResponse.DrugList.Length,
                    Url = drugListUrl 
                });
                return productResponse.DrugList;
            }

            _logger.LogWarning("ITS product list response format unexpected: {Response}", responseContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during ITS product list fetch");
            _opasLogger.LogSystemEvent("ItsProductFetch", "Exception occurred: " + ex.Message, new { Url = drugListUrl });
            return null;
        }
    }

    /// <summary>
    /// Geçerli token al (database'den veya yeni fetch)
    /// </summary>
    private async Task<string?> GetValidTokenAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            // Database'den mevcut token kontrol et
            var existingToken = await db.Tokens
                .Where(x => x.Name == "ITS-Access" && x.ExpiresAtUtc > DateTime.UtcNow)
                .FirstOrDefaultAsync(ct);

            if (existingToken != null)
            {
                _logger.LogDebug("Using existing ITS token from database");
                return existingToken.Token;
            }

            // Token yok ya da expired, yeni al
            _logger.LogInformation("No valid token in database, fetching new one...");
            var itsTokenService = scope.ServiceProvider.GetRequiredService<ItsTokenService>();
            return await itsTokenService.GetAndStoreTokenAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get valid ITS token");
            return null;
        }
    }
}
