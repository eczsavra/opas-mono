namespace Opas.Domain.Entities;

/// <summary>
/// Log entry entity for storing application logs
/// </summary>
public class LogEntry
{
    public int Id { get; set; }

    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public string? CorrelationId { get; set; }
    public string? ClientIP { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? StatusCode { get; set; }
    public long? DurationMs { get; set; }
    public string? Exception { get; set; }
    public string? Properties { get; set; } // JSON string for additional properties
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
