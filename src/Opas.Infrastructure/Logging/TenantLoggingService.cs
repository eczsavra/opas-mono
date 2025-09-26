using Microsoft.Extensions.Logging;
using Serilog;
using Opas.Shared.Logging;
using Opas.Shared.MultiTenancy;
using SerilogILogger = Serilog.ILogger;

namespace Opas.Infrastructure.Logging;

/// <summary>
/// Tenant-specific logging service
/// Her tenant'ın kendi log dosyalarına yazma
/// </summary>
public class TenantLoggingService
{
    private readonly ILogger<TenantLoggingService> _logger;
    private readonly Dictionary<string, SerilogILogger> _tenantLoggers = new();

    public TenantLoggingService(ILogger<TenantLoggingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Tenant-specific logger al veya oluştur
    /// </summary>
    public SerilogILogger GetTenantLogger(string tenantId)
    {
        if (_tenantLoggers.TryGetValue(tenantId, out var existingLogger))
        {
            return existingLogger;
        }

        // Tenant-specific logger oluştur
        var tenantLogger = new LoggerConfiguration()
            .WriteTo.File(
                path: $"logs/tenants/{tenantId}/activity-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {CorrelationId} {UserId} {Message:lj} {Properties:j}{NewLine}{Exception}"
            )
            .WriteTo.File(
                path: $"logs/tenants/{tenantId}/security-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {CorrelationId} {UserId} {ClientIP} {SecurityAction} {Message:lj} {Properties:j}{NewLine}{Exception}",
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning
            )
            .WriteTo.File(
                path: $"logs/tenants/{tenantId}/business-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {CorrelationId} {UserId} {BusinessOperation} {Message:lj} {Properties:j}{NewLine}{Exception}"
            )
            .Enrich.WithProperty("TenantId", tenantId)
            .CreateLogger();

        _tenantLoggers[tenantId] = tenantLogger;
        return tenantLogger;
    }

    /// <summary>
    /// Tenant activity log
    /// </summary>
    public void LogTenantActivity(string tenantId, string userId, string activity, object? data = null)
    {
        var tenantLogger = GetTenantLogger(tenantId);
        tenantLogger.Information("Tenant activity: {Activity} by {UserId}. Data: {@Data}", 
            activity, userId, data);
    }

    /// <summary>
    /// Tenant security event
    /// </summary>
    public void LogTenantSecurity(string tenantId, string userId, string action, string ipAddress, object? details = null)
    {
        var tenantLogger = GetTenantLogger(tenantId);
        tenantLogger.Warning("Tenant security: {Action} by {UserId} from {IPAddress}. Details: {@Details}", 
            action, userId, ipAddress, details);
    }

    /// <summary>
    /// Tenant business event
    /// </summary>
    public void LogTenantBusiness(string tenantId, string userId, string operation, object? data = null)
    {
        var tenantLogger = GetTenantLogger(tenantId);
        tenantLogger.Information("Tenant business: {Operation} by {UserId}. Data: {@Data}", 
            operation, userId, data);
    }
}

/// <summary>
/// OPAS Management logging service
/// Global system logs for OPAS administrators
/// </summary>
public class ManagementLoggingService
{
    private readonly ILogger<ManagementLoggingService> _logger;

    public ManagementLoggingService(ILogger<ManagementLoggingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// System-wide event
    /// </summary>
    public void LogSystemEvent(string eventType, string message, object? details = null)
    {
        _logger.LogInformation("System event: {EventType} - {Message}. Details: {@Details}", 
            eventType, message, details);
    }

    /// <summary>
    /// Global security event
    /// </summary>
    public void LogGlobalSecurity(string eventType, string description, object? details = null)
    {
        _logger.LogWarning("Global security: {EventType} - {Description}. Details: {@Details}", 
            eventType, description, details);
    }

    /// <summary>
    /// Cross-tenant audit event
    /// </summary>
    public void LogCrossTenantAudit(string operation, string fromTenant, string toTenant, object? details = null)
    {
        _logger.LogInformation("Cross-tenant audit: {Operation} from {FromTenant} to {ToTenant}. Details: {@Details}", 
            operation, fromTenant, toTenant, details);
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public void LogPerformanceMetric(string metric, double value, string unit, object? context = null)
    {
        _logger.LogInformation("Performance: {Metric} = {Value} {Unit}. Context: {@Context}", 
            metric, value, unit, context);
    }
}
