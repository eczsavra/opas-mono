using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Opas.Infrastructure.Persistence;
using Opas.Shared.ControlPlane;


namespace Opas.Infrastructure;

public class TokenProvider : ITokenProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenProvider> _logger;

    public TokenProvider(IServiceProvider serviceProvider, ILogger<TokenProvider> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            var token = await db.Tokens
                .FirstOrDefaultAsync(x => x.Name == tokenName, cancellationToken);

            if (token == null)
            {
                _logger.LogWarning("Token not found: {TokenName}. Attempting to fetch new token.", tokenName);
                return await FetchNewTokenAsync(tokenName, cancellationToken);
            }

            // Check if token is expired
            if (token.ExpiresAtUtc.HasValue && token.ExpiresAtUtc.Value <= DateTime.UtcNow)
            {
                _logger.LogWarning("Token expired: {TokenName}, ExpiresAt: {ExpiresAt}. Fetching new token.", 
                    tokenName, token.ExpiresAtUtc);
                return await FetchNewTokenAsync(tokenName, cancellationToken);
            }

            return token.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token: {TokenName}", tokenName);
            return null;
        }
    }

    public async Task<bool> IsTokenValidAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(tokenName, cancellationToken);
        return !string.IsNullOrWhiteSpace(token);
    }

    private async Task<string?> FetchNewTokenAsync(string tokenName, CancellationToken cancellationToken)
    {
        if (tokenName == "ITS-Access")
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var itsTokenService = scope.ServiceProvider.GetRequiredService<ItsTokenService>();
                
                var newToken = await itsTokenService.GetAndStoreTokenAsync(cancellationToken);
                if (newToken != null)
                {
                    _logger.LogInformation("Successfully fetched new token for: {TokenName}", tokenName);
                    return newToken;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch new token for: {TokenName}", tokenName);
            }
        }
        
        _logger.LogError("Cannot fetch new token for: {TokenName}. Only ITS-Access tokens are supported.", tokenName);
        return null;
    }
}
