using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Infrastructure.Logging;
using Opas.Shared.Logging;
using Opas.Domain.Entities;
using System.Text.Json;

namespace Opas.Api.Endpoints;

/// <summary>
/// Ultimate Log Dashboard API Endpoints
/// En mükemmel log dashboard için backend API
/// </summary>
public static class LogEndpoints
{
    public static void MapLogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/logs")
            .WithTags("Logs")
            .WithOpenApi();

        // GET /api/logs/tenant/{tenantId} - Tenant-specific logs
        group.MapGet("/tenant/{tenantId}", async (
            string tenantId,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? level,
            [FromQuery] string? search,
            [FromServices] ControlPlaneDbContext db,
            [FromServices] IOpasLogger opasLogger,
            [FromServices] TenantLoggingService tenantLogging,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // Default values
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 50;
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    // Log access attempt
                    opasLogger.LogSystemEvent("LogAccess", $"Tenant logs accessed for {tenantId}", new {
                        TenantId = tenantId,
                        IP = clientIP,
                        Filters = new { startDate, endDate, level, search }
                    });

                    // Build query for real log entries
                    var query = db.LogEntries
                        .Where(l => l.TenantId == tenantId)
                        .AsQueryable();

                    // Apply filters
                    if (startDate.HasValue)
                        query = query.Where(l => l.Timestamp >= startDate.Value.ToUniversalTime());
                    
                    if (endDate.HasValue)
                        query = query.Where(l => l.Timestamp <= endDate.Value.ToUniversalTime());

                    if (!string.IsNullOrEmpty(level))
                        query = query.Where(l => l.Level == level);

                    if (!string.IsNullOrEmpty(search))
                        query = query.Where(l => l.Message.Contains(search) || 
                                                (l.UserId != null && l.UserId.Contains(search)));

                    // Get paginated results
                    var totalCount = await query.CountAsync();
                    var logs = await query
                        .OrderByDescending(l => l.Timestamp)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(l => new {
                            Id = l.Id,
                            Timestamp = l.Timestamp,
                            Level = l.Level,
                            Message = l.Message,
                            User = l.UserId,
                            IP = l.ClientIP,
                            TenantId = l.TenantId,
                            CorrelationId = l.CorrelationId,
                            RequestPath = l.RequestPath,
                            RequestMethod = l.RequestMethod,
                            StatusCode = l.StatusCode,
                            DurationMs = l.DurationMs,
                            Exception = l.Exception,
                            Properties = l.Properties
                        })
                        .ToListAsync();

                    // Log to tenant-specific logs
                    tenantLogging.LogTenantActivity(tenantId, "System", "LogAccess", new {
                        IP = clientIP,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount
                    });

                    return Results.Ok(new {
                        success = true,
                        data = logs,
                        pagination = new {
                            page,
                            pageSize,
                            totalCount,
                            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                        },
                        filters = new {
                            startDate,
                            endDate,
                            level,
                            search
                        }
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogError(ex, $"Failed to retrieve logs for tenant {tenantId}", new {
                        TenantId = tenantId,
                        IP = clientIP
                    });
                    
                    return Results.Problem("Log retrieval failed", statusCode: 500);
                }
            }
        })
        .WithName("GetTenantLogs")
        .WithSummary("Get tenant-specific logs")
        .WithDescription("Retrieve logs for a specific tenant with filtering and pagination");

        // GET /api/logs/management - Global management logs
        group.MapGet("/management", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? level,
            [FromQuery] string? tenantId,
            [FromQuery] string? search,
            [FromServices] ControlPlaneDbContext db,
            [FromServices] IOpasLogger opasLogger,
            [FromServices] ManagementLoggingService managementLogging,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // Default values
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 50;
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    // Log management access
                    opasLogger.LogSystemEvent("ManagementLogAccess", "Management logs accessed", new {
                        IP = clientIP,
                        Filters = new { startDate, endDate, level, tenantId, search }
                    });

                    // Build query for all tenants
                    var query = db.PharmacistAdmins.AsQueryable();

                    // Apply filters
                    if (startDate.HasValue)
                        query = query.Where(p => p.CreatedAt >= startDate.Value.ToUniversalTime());
                    
                    if (endDate.HasValue)
                        query = query.Where(p => p.CreatedAt <= endDate.Value.ToUniversalTime());

                    if (!string.IsNullOrEmpty(tenantId))
                        query = query.Where(p => p.TenantId == tenantId);

                    // Get paginated results
                    var totalCount = await query.CountAsync();
                    var logs = await query
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(p => new {
                            Id = p.Id,
                            Timestamp = p.CreatedAt,
                            Level = "INFO",
                            Message = $"Pharmacist {p.Username} activity",
                            User = p.Username,
                            IP = "127.0.0.1",
                            TenantId = p.TenantId,
                            TenantName = "Eczane " + p.TenantId,
                            Details = new {
                                FirstName = p.FirstName,
                                LastName = p.LastName,
                                Email = p.Email,
                                IsActive = p.IsActive
                            }
                        })
                        .ToListAsync();

                    // Log to management system
                    managementLogging.LogSystemEvent("ManagementLogAccess", "Management logs retrieved", new {
                        IP = clientIP,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount
                    });

                    return Results.Ok(new {
                        success = true,
                        data = logs,
                        pagination = new {
                            page,
                            pageSize,
                            totalCount,
                            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                        },
                        filters = new {
                            startDate,
                            endDate,
                            level,
                            tenantId,
                            search
                        }
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogError(ex, "Failed to retrieve management logs", new {
                        IP = clientIP
                    });
                    
                    return Results.Problem("Management log retrieval failed", statusCode: 500);
                }
            }
        })
        .WithName("GetManagementLogs")
        .WithSummary("Get global management logs")
        .WithDescription("Retrieve system-wide logs for OPAS management");

        // GET /api/logs/analytics - Log analytics
        group.MapGet("/analytics", async (
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? tenantId,
            [FromServices] ControlPlaneDbContext db,
            [FromServices] IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    // Log analytics access
                    opasLogger.LogSystemEvent("LogAnalyticsAccess", "Log analytics accessed", new {
                        IP = clientIP,
                        TenantId = tenantId,
                        DateRange = new { startDate, endDate }
                    });

                    // Build date range
                    var start = startDate?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(-30);
                    var end = endDate?.ToUniversalTime() ?? DateTime.UtcNow;

                    // Get analytics data
                    var query = db.PharmacistAdmins
                        .Where(p => p.CreatedAt >= start && p.CreatedAt <= end);

                    if (!string.IsNullOrEmpty(tenantId))
                        query = query.Where(p => p.TenantId == tenantId);

                    var analytics = new {
                        totalLogs = await query.CountAsync(),
                        activeUsers = await query.Select(p => p.Username).Distinct().CountAsync(),
                        totalTenants = await query.Select(p => p.TenantId).Distinct().CountAsync(),
                        dailyActivity = await query
                            .GroupBy(p => p.CreatedAt.Date)
                            .Select(g => new {
                                date = g.Key,
                                count = g.Count()
                            })
                            .OrderBy(x => x.date)
                            .ToListAsync(),
                        levelDistribution = new {
                            info = await query.CountAsync(),
                            warning = 0,
                            error = 0,
                            success = 0
                        },
                        topUsers = await query
                            .GroupBy(p => p.Username)
                            .Select(g => new {
                                username = g.Key,
                                count = g.Count()
                            })
                            .OrderByDescending(x => x.count)
                            .Take(10)
                            .ToListAsync()
                    };

                    return Results.Ok(new {
                        success = true,
                        data = analytics,
                        dateRange = new { start, end },
                        tenantId
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogError(ex, "Failed to retrieve log analytics", new {
                        IP = clientIP,
                        TenantId = tenantId
                    });
                    
                    return Results.Problem("Log analytics retrieval failed", statusCode: 500);
                }
            }
        })
        .WithName("GetLogAnalytics")
        .WithSummary("Get log analytics")
        .WithDescription("Retrieve analytics and statistics for logs");

        // GET /api/logs/export - Export logs
        group.MapGet("/export", async (
            [FromQuery] string format,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? level,
            [FromQuery] string? tenantId,
            [FromServices] ControlPlaneDbContext db,
            [FromServices] IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // Default format
            if (string.IsNullOrEmpty(format)) format = "json";
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    // Log export attempt
                    opasLogger.LogSystemEvent("LogExport", "Log export requested", new {
                        IP = clientIP,
                        Format = format,
                        Filters = new { startDate, endDate, level, tenantId }
                    });

                    // Build query
                    var query = db.PharmacistAdmins.AsQueryable();

                    if (startDate.HasValue)
                        query = query.Where(p => p.CreatedAt >= startDate.Value.ToUniversalTime());
                    
                    if (endDate.HasValue)
                        query = query.Where(p => p.CreatedAt <= endDate.Value.ToUniversalTime());

                    if (!string.IsNullOrEmpty(tenantId))
                        query = query.Where(p => p.TenantId == tenantId);

                    var logs = await query
                        .OrderByDescending(p => p.CreatedAt)
                        .Select(p => new {
                            Timestamp = p.CreatedAt,
                            Level = "INFO",
                            Message = $"Pharmacist {p.Username} activity",
                            User = p.Username,
                            IP = "127.0.0.1",
                            TenantId = p.TenantId,
                            Details = new {
                                FirstName = p.FirstName,
                                LastName = p.LastName,
                                Email = p.Email
                            }
                        })
                        .ToListAsync();

                    if (format.ToLower() == "csv")
                    {
                        var csv = "Timestamp,Level,Message,User,IP,TenantId\n" +
                                string.Join("\n", logs.Select(l => 
                                    $"{l.Timestamp:yyyy-MM-dd HH:mm:ss},{l.Level},{l.Message},{l.User},{l.IP},{l.TenantId}"));
                        
                        return Results.File(
                            System.Text.Encoding.UTF8.GetBytes(csv),
                            "text/csv",
                            $"logs_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
                    }
                    else
                    {
                        return Results.Ok(new {
                            success = true,
                            data = logs,
                            exportedAt = DateTime.UtcNow,
                            format = "json",
                            count = logs.Count
                        });
                    }
                }
                catch (Exception ex)
                {
                    opasLogger.LogError(ex, "Failed to export logs", new {
                        IP = clientIP,
                        Format = format
                    });
                    
                    return Results.Problem("Log export failed", statusCode: 500);
                }
            }
        })
        .WithName("ExportLogs")
        .WithSummary("Export logs")
        .WithDescription("Export logs in various formats (JSON, CSV)");
    }
}
