using Microsoft.AspNetCore.Http;

namespace Opas.Shared.MultiTenancy;

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private readonly IHttpContextAccessor _http;

    public TenantContextAccessor(IHttpContextAccessor http)
    {
        _http = http;
    }

    public TenantContext Current
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx == null) throw new InvalidOperationException("No HttpContext");

            if (ctx.Items.TryGetValue("TenantContext", out var v) && v is TenantContext t)
                return t;

            // Middleware çalışmasa bile emniyet: default tenant
            return new TenantContext("default");
        }
    }
}
