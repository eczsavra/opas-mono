using Opas.Shared.MultiTenancy;

namespace Opas.Infrastructure.MultiTenancy;

public sealed class HttpTenantProvider : ITenantProvider
{
    private readonly ITenantContextAccessor _accessor;

    public HttpTenantProvider(ITenantContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string TenantId => _accessor.Current.TenantId;
}
