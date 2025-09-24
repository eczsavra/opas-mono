namespace Opas.Shared.ControlPlane;

public interface ITokenProvider
{
    Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken = default);
    Task<bool> IsTokenValidAsync(string tokenName, CancellationToken cancellationToken = default);
}
