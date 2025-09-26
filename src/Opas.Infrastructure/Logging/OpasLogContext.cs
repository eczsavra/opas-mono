using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Security.Claims;
using Opas.Shared.MultiTenancy;

namespace Opas.Infrastructure.Logging;

/// <summary>
/// OPAS için log context enrichment
/// </summary>
public static class OpasLogContext
{
    /// <summary>
    /// HTTP request için tenant, user, IP context ekler
    /// </summary>
    public static IDisposable EnrichFromHttpContext(HttpContext httpContext)
    {
        var disposables = new List<IDisposable>();

        // User Information
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst("pharmacist_id")?.Value;
            var username = httpContext.User.FindFirst("username")?.Value;
            var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(userId))
                disposables.Add(LogContext.PushProperty("UserId", userId));
                
            if (!string.IsNullOrEmpty(username))
                disposables.Add(LogContext.PushProperty("Username", username));
                
            if (!string.IsNullOrEmpty(tenantId))
                disposables.Add(LogContext.PushProperty("TenantId", tenantId));
                
            if (!string.IsNullOrEmpty(role))
                disposables.Add(LogContext.PushProperty("UserRole", role));
        }

        // Request Information
        disposables.Add(LogContext.PushProperty("RequestMethod", httpContext.Request.Method));
        disposables.Add(LogContext.PushProperty("RequestPath", httpContext.Request.Path));
        
        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
        disposables.Add(LogContext.PushProperty("UserAgent", userAgent));
        
        // IP Address (önemli - Medula için)
        var clientIP = GetClientIPAddress(httpContext);
        disposables.Add(LogContext.PushProperty("ClientIP", clientIP));

        // Correlation ID (if available)
        var correlationId = httpContext.TraceIdentifier;
        disposables.Add(LogContext.PushProperty("CorrelationId", correlationId));

        // Tenant Context (if available)
        var tenantContext = httpContext.Items["TenantContext"] as TenantContext;
        if (tenantContext != null)
        {
            disposables.Add(LogContext.PushProperty("TenantId", tenantContext.TenantId));
            disposables.Add(LogContext.PushProperty("TenantRegion", tenantContext.Region ?? "default"));
        }

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Business işlemler için context
    /// </summary>
    public static IDisposable EnrichBusinessContext(string operation, object? data = null)
    {
        var disposables = new List<IDisposable>
        {
            LogContext.PushProperty("BusinessOperation", operation),
            LogContext.PushProperty("Timestamp", DateTimeOffset.UtcNow)
        };

        if (data != null)
        {
            disposables.Add(LogContext.PushProperty("BusinessData", data, true));
        }

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Security işlemler için context
    /// </summary>
    public static IDisposable EnrichSecurityContext(string action, string? target = null, bool sensitive = false)
    {
        var disposables = new List<IDisposable>
        {
            LogContext.PushProperty("SecurityAction", action),
            LogContext.PushProperty("SecurityLevel", sensitive ? "HIGH" : "NORMAL"),
            LogContext.PushProperty("Timestamp", DateTimeOffset.UtcNow)
        };

        if (!string.IsNullOrEmpty(target))
        {
            disposables.Add(LogContext.PushProperty("SecurityTarget", target));
        }

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// External API çağrıları için context
    /// </summary>
    public static IDisposable EnrichExternalApiContext(string apiName, string endpoint, string method = "GET")
    {
        var disposables = new List<IDisposable>
        {
            LogContext.PushProperty("ExternalAPI", apiName),
            LogContext.PushProperty("APIEndpoint", endpoint),
            LogContext.PushProperty("APIMethod", method),
            LogContext.PushProperty("APICallTimestamp", DateTimeOffset.UtcNow)
        };

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Client IP adresini güvenli şekilde alır
    /// </summary>
    private static string GetClientIPAddress(HttpContext httpContext)
    {
        // X-Forwarded-For header (load balancer/proxy arkasında)
        var xForwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        // X-Real-IP header
        var xRealIP = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIP))
        {
            return xRealIP;
        }

        // Direct connection
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Multiple disposable'ları birleştiren helper class
/// </summary>
public class CompositeDisposable : IDisposable
{
    private readonly List<IDisposable> _disposables;

    public CompositeDisposable(List<IDisposable> disposables)
    {
        _disposables = disposables;
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable?.Dispose();
        }
        _disposables.Clear();
    }
}
