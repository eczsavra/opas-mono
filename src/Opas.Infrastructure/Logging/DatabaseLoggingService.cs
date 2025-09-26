using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Opas.Infrastructure.Persistence;
using Opas.Domain.Entities;
using Opas.Shared.Logging;

namespace Opas.Infrastructure.Logging;

/// <summary>
/// Database'e log yazan service
/// </summary>
public class DatabaseLoggingService
{
    private readonly ControlPlaneDbContext _dbContext;
    private readonly ILogger<DatabaseLoggingService> _logger;

    public DatabaseLoggingService(ControlPlaneDbContext dbContext, ILogger<DatabaseLoggingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Log entry'yi database'e kaydet
    /// </summary>
    public async Task LogAsync(LogEntry logEntry)
    {
        try
        {
            _dbContext.LogEntries.Add(logEntry);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save log entry to database");
        }
    }

    /// <summary>
    /// Kullanıcı giriş log'u
    /// </summary>
    public async Task LogUserLoginAsync(string userId, string tenantId, string clientIP, string userAgent, bool success, string? reason = null)
    {
        var logEntry = new LogEntry
        {
            Level = success ? "INFO" : "WARN",
            Message = success ? $"User {userId} logged in successfully" : $"User {userId} login failed: {reason}",
            UserId = userId,
            TenantId = tenantId,
            ClientIP = clientIP,
            UserAgent = userAgent,
            RequestPath = "/api/auth/login",
            RequestMethod = "POST",
            StatusCode = success ? 200 : 401,
            Timestamp = DateTime.UtcNow
        };

        await LogAsync(logEntry);
    }

    /// <summary>
    /// Kullanıcı çıkış log'u
    /// </summary>
    public async Task LogUserLogoutAsync(string userId, string tenantId, string clientIP)
    {
        var logEntry = new LogEntry
        {
            Level = "INFO",
            Message = $"User {userId} logged out",
            UserId = userId,
            TenantId = tenantId,
            ClientIP = clientIP,
            RequestPath = "/logout",
            RequestMethod = "POST",
            StatusCode = 200,
            Timestamp = DateTime.UtcNow
        };

        await LogAsync(logEntry);
    }

    /// <summary>
    /// Şifre sıfırlama log'u
    /// </summary>
    public async Task LogPasswordResetAsync(string userId, string tenantId, string clientIP, bool success, string? reason = null)
    {
        var logEntry = new LogEntry
        {
            Level = success ? "INFO" : "WARN",
            Message = success ? $"User {userId} password reset successfully" : $"User {userId} password reset failed: {reason}",
            UserId = userId,
            TenantId = tenantId,
            ClientIP = clientIP,
            RequestPath = "/api/auth/password-reset",
            RequestMethod = "POST",
            StatusCode = success ? 200 : 400,
            Timestamp = DateTime.UtcNow
        };

        await LogAsync(logEntry);
    }

    /// <summary>
    /// Kayıt log'u
    /// </summary>
    public async Task LogRegistrationAsync(string userId, string tenantId, string clientIP, bool success, string? reason = null)
    {
        var logEntry = new LogEntry
        {
            Level = success ? "INFO" : "WARN",
            Message = success ? $"User {userId} registered successfully" : $"User {userId} registration failed: {reason}",
            UserId = userId,
            TenantId = tenantId,
            ClientIP = clientIP,
            RequestPath = "/api/auth/pharmacist/register",
            RequestMethod = "POST",
            StatusCode = success ? 200 : 400,
            Timestamp = DateTime.UtcNow
        };

        await LogAsync(logEntry);
    }
}
