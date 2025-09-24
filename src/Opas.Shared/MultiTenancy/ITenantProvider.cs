namespace Opas.Shared.MultiTenancy;

public interface ITenantProvider
{
    string TenantId { get; }
}
