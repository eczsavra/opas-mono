namespace Opas.Shared.MultiTenancy;

public interface ITenantContextAccessor
{
    TenantContext Current { get; }
}
