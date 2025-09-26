namespace Opas.Shared.Logging;

/// <summary>
/// OPAS için özelleştirilmiş logging interface
/// </summary>
public interface IOpasLogger
{
    // Security Events
    void LogUserLogin(string username, string ipAddress, bool success, string? reason = null);
    void LogUserLogout(string username, string? reason = null);
    void LogPasswordChange(string username, string ipAddress, bool success, string? reason = null);
    void LogSuspiciousActivity(string username, string ipAddress, string activity, object? details = null);
    void LogPermissionDenied(string username, string resource, string action);

    // Business Events
    void LogSaleTransaction(string pharmacistId, decimal amount, int itemCount, object saleDetails);
    void LogPrescriptionProcessed(string pharmacistId, string prescriptionId, object prescriptionDetails);
    void LogInventoryChange(string pharmacistId, string productId, int quantity, string operation);
    void LogCustomerInteraction(string pharmacistId, string customerId, string action, object? details = null);
    void LogReportGenerated(string userId, string reportType, object parameters);

    // User Activity Events  
    void LogPageVisit(string userId, string page, TimeSpan duration);
    void LogFeatureUsage(string userId, string feature, object? parameters = null);
    void LogSearchQuery(string userId, string query, int resultCount);
    void LogFormSubmission(string userId, string formName, bool success, object? data = null);

    // System Events
    void LogSystemEvent(string eventType, string message, object? details = null);
    void LogError(Exception? exception, string message, object? details = null);
    void LogPerformanceMetric(string operation, TimeSpan duration, bool success);
    void LogExternalApiCall(string apiName, string endpoint, TimeSpan duration, bool success, string? error = null);
    void LogBackgroundJob(string jobName, TimeSpan duration, bool success, string? error = null);

    // External Integration Events
    void LogMedulaActivity(string pharmacistId, string action, bool success, object? details = null);
    void LogNviVerification(string tcNumber, bool success, string? error = null);
    void LogGlnValidation(string gln, bool success, object? details = null);

    // Tenant Events
    void LogTenantActivity(string tenantId, string action, object? details = null);
    void LogTenantDatabaseOperation(string tenantId, string operation, bool success);

    // Audit Events
    void LogDataAccess(string userId, string dataType, string operation, object? details = null);
    void LogSensitiveDataAccess(string userId, string dataType, string purpose, object? details = null);
    void LogComplianceEvent(string eventType, string description, object? details = null);
}

/// <summary>
/// OPAS Logger extension methods
/// </summary>
public static class OpasLoggerExtensions
{
    /// <summary>
    /// Critical security event loglama
    /// </summary>
    public static void LogCriticalSecurityEvent(this IOpasLogger logger, string username, string ipAddress, string event_, object? details = null)
    {
        logger.LogSuspiciousActivity(username, ipAddress, event_, details);
    }

    /// <summary>
    /// Business transaction loglama
    /// </summary>
    public static void LogBusinessTransaction(this IOpasLogger logger, string pharmacistId, string transactionType, object transactionData)
    {
        logger.LogSaleTransaction(pharmacistId, 0, 0, transactionData);
    }
}
