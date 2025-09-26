using Microsoft.Extensions.Logging;
using Serilog;
using Opas.Shared.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Opas.Infrastructure.Logging;

/// <summary>
/// OPAS için özelleştirilmiş logger implementation
/// </summary>
public class OpasLogger : IOpasLogger
{
    private readonly ILogger<OpasLogger> _logger;
    private readonly TenantLoggingService _tenantLogging;
    private readonly ManagementLoggingService _managementLogging;

    public OpasLogger(
        ILogger<OpasLogger> logger,
        TenantLoggingService tenantLogging,
        ManagementLoggingService managementLogging)
    {
        _logger = logger;
        _tenantLogging = tenantLogging;
        _managementLogging = managementLogging;
    }

    #region Security Events

    public void LogUserLogin(string username, string ipAddress, bool success, string? reason = null)
    {
        using (OpasLogContext.EnrichSecurityContext("user_login", username))
        {
            if (success)
            {
                _logger.LogInformation("User {Username} successfully logged in from {IPAddress}", 
                    username, ipAddress);
            }
            else
            {
                _logger.LogWarning("Failed login attempt for user {Username} from {IPAddress}. Reason: {Reason}", 
                    username, ipAddress, reason ?? "Invalid credentials");
            }
        }
    }

    public void LogUserLogout(string username, string? reason = null)
    {
        using (OpasLogContext.EnrichSecurityContext("user_logout", username))
        {
            _logger.LogInformation("User {Username} logged out. Reason: {Reason}", 
                username, reason ?? "User initiated");
        }
    }

    public void LogPasswordChange(string username, string ipAddress, bool success, string? reason = null)
    {
        using (OpasLogContext.EnrichSecurityContext("password_change", username, sensitive: true))
        {
            if (success)
            {
                _logger.LogInformation("Password changed successfully for user {Username} from {IPAddress}. Reason: {Reason}", 
                    username, ipAddress, reason ?? "User initiated");
            }
            else
            {
                _logger.LogWarning("Password change failed for user {Username} from {IPAddress}. Reason: {Reason}", 
                    username, ipAddress, reason ?? "Unknown");
            }
        }
    }

    public void LogSuspiciousActivity(string username, string ipAddress, string activity, object? details = null)
    {
        using (OpasLogContext.EnrichSecurityContext("suspicious_activity", username, sensitive: true))
        {
            _logger.LogError("Suspicious activity detected: {Activity} by {Username} from {IPAddress}. Details: {@Details}", 
                activity, username, ipAddress, details);
        }
    }

    public void LogPermissionDenied(string username, string resource, string action)
    {
        using (OpasLogContext.EnrichSecurityContext("permission_denied", username))
        {
            _logger.LogWarning("Permission denied for user {Username} trying to {Action} on {Resource}", 
                username, action, resource);
        }
    }

    #endregion

    #region Business Events

    public void LogSaleTransaction(string pharmacistId, decimal amount, int itemCount, object saleDetails)
    {
        using (OpasLogContext.EnrichBusinessContext("sale_transaction", saleDetails))
        {
            _logger.LogInformation("Sale completed by {PharmacistId}: Amount={Amount:C}, Items={ItemCount}. Details: {@SaleDetails}", 
                pharmacistId, amount, itemCount, saleDetails);
        }
    }

    public void LogPrescriptionProcessed(string pharmacistId, string prescriptionId, object prescriptionDetails)
    {
        using (OpasLogContext.EnrichBusinessContext("prescription_processed", prescriptionDetails))
        {
            _logger.LogInformation("Prescription {PrescriptionId} processed by {PharmacistId}. Details: {@PrescriptionDetails}", 
                prescriptionId, pharmacistId, prescriptionDetails);
        }
    }

    public void LogInventoryChange(string pharmacistId, string productId, int quantity, string operation)
    {
        using (OpasLogContext.EnrichBusinessContext("inventory_change"))
        {
            _logger.LogInformation("Inventory {Operation}: Product={ProductId}, Quantity={Quantity}, By={PharmacistId}", 
                operation, productId, quantity, pharmacistId);
        }
    }

    public void LogCustomerInteraction(string pharmacistId, string customerId, string action, object? details = null)
    {
        using (OpasLogContext.EnrichBusinessContext("customer_interaction", details))
        {
            _logger.LogInformation("Customer interaction: {Action} on {CustomerId} by {PharmacistId}. Details: {@Details}", 
                action, customerId, pharmacistId, details);
        }
    }

    public void LogReportGenerated(string userId, string reportType, object parameters)
    {
        using (OpasLogContext.EnrichBusinessContext("report_generated", parameters))
        {
            _logger.LogInformation("Report generated: {ReportType} by {UserId}. Parameters: {@Parameters}", 
                reportType, userId, parameters);
        }
    }

    #endregion

    #region User Activity Events

    public void LogPageVisit(string userId, string page, TimeSpan duration)
    {
        _logger.LogDebug("Page visit: {Page} by {UserId}, Duration={Duration}ms", 
            page, userId, duration.TotalMilliseconds);
    }

    public void LogFeatureUsage(string userId, string feature, object? parameters = null)
    {
        _logger.LogInformation("Feature used: {Feature} by {UserId}. Parameters: {@Parameters}", 
            feature, userId, parameters);
    }

    public void LogSearchQuery(string userId, string query, int resultCount)
    {
        _logger.LogInformation("Search performed by {UserId}: '{Query}' returned {ResultCount} results", 
            userId, query, resultCount);
    }

    public void LogFormSubmission(string userId, string formName, bool success, object? data = null)
    {
        _logger.LogInformation("Form submission: {FormName} by {UserId}, Success={Success}. Data: {@Data}", 
            formName, userId, success, data);
    }

    #endregion

    #region System Events

    public void LogSystemEvent(string eventType, string message, object? details = null)
    {
        _logger.LogInformation("System event: {EventType} - {Message}. Details: {@Details}", 
            eventType, message, details);
    }

    public void LogPerformanceMetric(string operation, TimeSpan duration, bool success)
    {
        if (duration.TotalSeconds > 5) // Slow operation threshold
        {
            _logger.LogWarning("Slow operation detected: {Operation} took {Duration}ms, Success={Success}", 
                operation, duration.TotalMilliseconds, success);
        }
        else
        {
            _logger.LogDebug("Operation: {Operation} completed in {Duration}ms, Success={Success}", 
                operation, duration.TotalMilliseconds, success);
        }
    }

    public void LogExternalApiCall(string apiName, string endpoint, TimeSpan duration, bool success, string? error = null)
    {
        using (OpasLogContext.EnrichExternalApiContext(apiName, endpoint))
        {
            if (success)
            {
                _logger.LogInformation("External API call successful: {ApiName} -> {Endpoint} in {Duration}ms", 
                    apiName, endpoint, duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogError("External API call failed: {ApiName} -> {Endpoint} after {Duration}ms. Error: {Error}", 
                    apiName, endpoint, duration.TotalMilliseconds, error);
            }
        }
    }

    public void LogBackgroundJob(string jobName, TimeSpan duration, bool success, string? error = null)
    {
        if (success)
        {
            _logger.LogInformation("Background job completed: {JobName} in {Duration}ms", 
                jobName, duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogError("Background job failed: {JobName} after {Duration}ms. Error: {Error}", 
                jobName, duration.TotalMilliseconds, error);
        }
    }

    #endregion

    #region External Integration Events

    public void LogMedulaActivity(string pharmacistId, string action, bool success, object? details = null)
    {
        using (OpasLogContext.EnrichExternalApiContext("Medula", action))
        {
            if (success)
            {
                _logger.LogInformation("Medula operation successful: {Action} by {PharmacistId}. Details: {@Details}", 
                    action, pharmacistId, details);
            }
            else
            {
                _logger.LogError("Medula operation failed: {Action} by {PharmacistId}. Details: {@Details}", 
                    action, pharmacistId, details);
            }
        }
    }

    public void LogNviVerification(string tcNumber, bool success, string? error = null)
    {
        using (OpasLogContext.EnrichExternalApiContext("NVI", "verify_identity"))
        {
            // TC numarasını log'a yazmıyoruz (privacy)
            var maskedTc = string.IsNullOrEmpty(tcNumber) ? "unknown" : $"{tcNumber[..3]}****{tcNumber[^2..]}";
            
            if (success)
            {
                _logger.LogInformation("NVI verification successful for TC: {MaskedTc}", maskedTc);
            }
            else
            {
                _logger.LogWarning("NVI verification failed for TC: {MaskedTc}. Error: {Error}", maskedTc, error);
            }
        }
    }

    public void LogGlnValidation(string gln, bool success, object? details = null)
    {
        using (OpasLogContext.EnrichExternalApiContext("GLN_Registry", "validate_gln"))
        {
            _logger.LogInformation("GLN validation: {Gln}, Success={Success}. Details: {@Details}", 
                gln, success, details);
        }
    }

    #endregion

    #region Tenant Events

    public void LogTenantActivity(string tenantId, string action, object? details = null)
    {
        _logger.LogInformation("Tenant activity: {Action} for {TenantId}. Details: {@Details}", 
            action, tenantId, details);
    }

    public void LogTenantDatabaseOperation(string tenantId, string operation, bool success)
    {
        if (success)
        {
            _logger.LogInformation("Tenant database operation successful: {Operation} for {TenantId}", 
                operation, tenantId);
        }
        else
        {
            _logger.LogError("Tenant database operation failed: {Operation} for {TenantId}", 
                operation, tenantId);
        }
    }

    #endregion

    #region Audit Events

    public void LogDataAccess(string userId, string dataType, string operation, object? details = null)
    {
        _logger.LogInformation("Data access: {Operation} on {DataType} by {UserId}. Details: {@Details}", 
            operation, dataType, userId, details);
    }

    public void LogSensitiveDataAccess(string userId, string dataType, string purpose, object? details = null)
    {
        using (OpasLogContext.EnrichSecurityContext("sensitive_data_access", userId, sensitive: true))
        {
            _logger.LogWarning("Sensitive data accessed: {DataType} by {UserId} for {Purpose}. Details: {@Details}", 
                dataType, userId, purpose, details);
        }
    }

    public void LogComplianceEvent(string eventType, string description, object? details = null)
    {
        _logger.LogWarning("Compliance event: {EventType} - {Description}. Details: {@Details}", 
            eventType, description, details);
    }

    public void LogError(Exception? exception, string message, object? details = null)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "Error: {Message}. Details: {@Details}", message, details);
        }
        else
        {
            _logger.LogError("Error: {Message}. Details: {@Details}", message, details);
        }
    }

    #endregion
}
