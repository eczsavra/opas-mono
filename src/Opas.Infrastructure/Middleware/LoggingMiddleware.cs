using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Opas.Shared.Logging;
using Opas.Infrastructure.Logging;
using System.Diagnostics;

namespace Opas.Infrastructure.Middleware;

/// <summary>
/// Request/Response logging middleware
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        using (OpasLogContext.EnrichFromHttpContext(context))
        {
            // Request logging
            _logger.LogInformation("HTTP {Method} {Path} started", 
                context.Request.Method, context.Request.Path);

            try
            {
                await _next(context);
                
                stopwatch.Stop();
                
                // Success response logging
                _logger.LogInformation("HTTP {Method} {Path} completed with {StatusCode} in {Duration}ms", 
                    context.Request.Method, 
                    context.Request.Path, 
                    context.Response.StatusCode, 
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Error response logging
                _logger.LogError(ex, "HTTP {Method} {Path} failed with exception after {Duration}ms", 
                    context.Request.Method, 
                    context.Request.Path, 
                    stopwatch.ElapsedMilliseconds);
                
                throw; // Re-throw to maintain exception flow
            }
        }
    }
}

/// <summary>
/// Global exception handler for unhandled exceptions
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            using (OpasLogContext.EnrichFromHttpContext(context))
            {
                _logger.LogError(ex, "Unhandled exception occurred for {Method} {Path}", 
                    context.Request.Method, context.Request.Path);
            }

            // Detailed error response for development
            var env = context.RequestServices.GetService(typeof(IHostEnvironment)) as IHostEnvironment;
            if (env?.IsDevelopment() == true)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"Error: {ex.Message}\n\nStackTrace: {ex.StackTrace}");
            }
            else
            {
                // Production error response (no sensitive info)
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal server error");
            }
        }
    }
}
