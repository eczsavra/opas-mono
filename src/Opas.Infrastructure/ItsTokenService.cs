// ItsTokenService.cs
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Opas.Infrastructure.Persistence;
using Opas.Shared.ControlPlane;
using Microsoft.EntityFrameworkCore;

public class ItsTokenService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ItsTokenService> _logger;
    private readonly IConfiguration _cfg;
    private readonly IServiceProvider _serviceProvider;

    public ItsTokenService(IHttpClientFactory httpFactory, ILogger<ItsTokenService> logger, IConfiguration cfg, IServiceProvider serviceProvider)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _cfg = cfg;
        _serviceProvider = serviceProvider;
    }

    public record TokenResponse([property: JsonPropertyName("token")] string Token);

    public async Task<string?> GetAndStoreTokenAsync(CancellationToken ct = default)
    {
        var username = _cfg["Integrations:Its:Username"];
        var password = _cfg["Integrations:Its:Password"];
        var url = _cfg["Integrations:Its:TokenUrl"] ?? "https://its2.saglik.gov.tr/token/app/token/";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("ITS credentials missing in configuration. Skipping token fetch.");
            return null;
        }

        var client = _httpFactory.CreateClient("its");
        var body = new { username, password };

        try
        {
            var resp = await client.PostAsJsonAsync(url, body, ct);
            resp.EnsureSuccessStatusCode();
            var tr = await resp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
            if (tr?.Token != null)
            {
                _logger.LogInformation("Got ITS token length={Len}", tr.Token.Length);

                // Save to database
                await SaveTokenToDatabase(tr.Token, ct);
                return tr.Token;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch ITS token");
            return null;
        }
    }

    private async Task SaveTokenToDatabase(string token, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            // Find existing ITS token or create new
            var existing = await db.Tokens.FirstOrDefaultAsync(x => x.Name == "ITS-Access", ct);
            
            if (existing != null)
            {
                // Update existing token
                existing.Token = token;
                existing.ExpiresAtUtc = DateTime.UtcNow.AddHours(23); // Assume 24h expiry, refresh 1h early
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new token
                db.Tokens.Add(new TokenStore
                {
                    Name = "ITS-Access",
                    Token = token,
                    ExpiresAtUtc = DateTime.UtcNow.AddHours(23),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("ITS token saved to database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save ITS token to database");
        }
    }
}
