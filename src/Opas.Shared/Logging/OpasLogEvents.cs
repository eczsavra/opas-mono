namespace Opas.Shared.Logging;

/// <summary>
/// OPAS sistem logları için event kategorileri ve kodları
/// </summary>
public static class OpasLogEvents
{
    // 1000-1999: Authentication & Security Events
    public const int LOGIN_SUCCESS = 1001;
    public const int LOGIN_FAILED = 1002;
    public const int LOGOUT = 1003;
    public const int PASSWORD_CHANGED = 1004;
    public const int PASSWORD_RESET_REQUESTED = 1005;
    public const int PASSWORD_RESET_COMPLETED = 1006;
    public const int ACCOUNT_LOCKED = 1007;
    public const int SUSPICIOUS_IP_ACCESS = 1008;
    public const int PERMISSION_DENIED = 1009;
    public const int SESSION_EXPIRED = 1010;
    public const int MFA_REQUIRED = 1011;
    public const int MFA_SUCCESS = 1012;
    public const int MFA_FAILED = 1013;

    // 2000-2999: Business Events (Eczane İşlemleri)
    public const int SALE_TRANSACTION = 2001;
    public const int SALE_COMPLETED = 2002;
    public const int SALE_CANCELLED = 2003;
    public const int PRESCRIPTION_PROCESSED = 2004;
    public const int PRESCRIPTION_CANCELLED = 2005;
    public const int INVENTORY_UPDATED = 2006;
    public const int STOCK_LOW_WARNING = 2007;
    public const int CUSTOMER_CREATED = 2008;
    public const int CUSTOMER_UPDATED = 2009;
    public const int REPORT_GENERATED = 2010;
    public const int DATA_EXPORTED = 2011;
    public const int INVOICE_CREATED = 2012;
    public const int PAYMENT_PROCESSED = 2013;

    // 3000-3999: User Activity Events
    public const int PAGE_VISITED = 3001;
    public const int FEATURE_USED = 3002;
    public const int SEARCH_PERFORMED = 3003;
    public const int FORM_SUBMITTED = 3004;
    public const int SETTINGS_CHANGED = 3005;
    public const int PROFILE_UPDATED = 3006;
    public const int FILE_UPLOADED = 3007;
    public const int FILE_DOWNLOADED = 3008;
    public const int API_CALLED = 3009;
    public const int DASHBOARD_VIEWED = 3010;

    // 4000-4999: System Events
    public const int SYSTEM_STARTUP = 4001;
    public const int SYSTEM_SHUTDOWN = 4002;
    public const int DATABASE_CONNECTION_FAILED = 4003;
    public const int DATABASE_CONNECTION_RESTORED = 4004;
    public const int EXTERNAL_API_CALLED = 4005;
    public const int EXTERNAL_API_FAILED = 4006;
    public const int BACKGROUND_JOB_STARTED = 4007;
    public const int BACKGROUND_JOB_COMPLETED = 4008;
    public const int BACKGROUND_JOB_FAILED = 4009;
    public const int PERFORMANCE_WARNING = 4010;
    public const int MEMORY_USAGE_HIGH = 4011;
    public const int DISK_SPACE_LOW = 4012;

    // 5000-5999: External Integration Events (Medula, NVI, etc.)
    public const int MEDULA_CONNECTION_STARTED = 5001;
    public const int MEDULA_CONNECTION_SUCCESS = 5002;
    public const int MEDULA_CONNECTION_FAILED = 5003;
    public const int MEDULA_SESSION_EXPIRED = 5004;
    public const int MEDULA_FORM_SUBMITTED = 5005;
    public const int MEDULA_DATA_EXTRACTED = 5006;
    public const int NVI_VERIFICATION_STARTED = 5007;
    public const int NVI_VERIFICATION_SUCCESS = 5008;
    public const int NVI_VERIFICATION_FAILED = 5009;
    public const int GLN_VALIDATION_PERFORMED = 5010;
    public const int ITS_TOKEN_REQUESTED = 5011;
    public const int ITS_TOKEN_RECEIVED = 5012;

    // 6000-6999: Tenant & Multi-Tenancy Events
    public const int TENANT_CREATED = 6001;
    public const int TENANT_ACTIVATED = 6002;
    public const int TENANT_DEACTIVATED = 6003;
    public const int TENANT_DATABASE_CREATED = 6004;
    public const int TENANT_DATABASE_MIGRATED = 6005;
    public const int TENANT_CONTEXT_SWITCHED = 6006;
    public const int PHARMACIST_REGISTERED = 6007;
    public const int SUB_USER_CREATED = 6008;
    public const int SUB_USER_PERMISSION_CHANGED = 6009;

    // 7000-7999: Audit & Compliance Events  
    public const int DATA_ACCESS = 7001;
    public const int DATA_MODIFIED = 7002;
    public const int DATA_DELETED = 7003;
    public const int SENSITIVE_DATA_ACCESSED = 7004;
    public const int COMPLIANCE_VIOLATION = 7005;
    public const int AUDIT_LOG_VIEWED = 7006;
    public const int GDPR_REQUEST = 7007;
    public const int DATA_RETENTION_POLICY_APPLIED = 7008;
}

/// <summary>
/// Log kategorileri için string constants
/// </summary>
public static class OpasLogCategories
{
    public const string SECURITY = "OPAS.Security";
    public const string BUSINESS = "OPAS.Business";
    public const string USER_ACTIVITY = "OPAS.UserActivity";
    public const string SYSTEM = "OPAS.System";
    public const string EXTERNAL_INTEGRATION = "OPAS.External";
    public const string TENANT = "OPAS.Tenant";
    public const string AUDIT = "OPAS.Audit";
}

/// <summary>
/// Log seviyeleri için OPAS özel tanımları
/// </summary>
public enum OpasLogLevel
{
    Trace = 0,      // En detaylı - development only
    Debug = 1,      // Debug bilgileri
    Information = 2, // Normal işlem logları
    Warning = 3,    // Dikkat gerektiren durumlar
    Error = 4,      // Hatalar
    Fatal = 5       // Kritik sistem hataları
}
