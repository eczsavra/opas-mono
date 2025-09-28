using Microsoft.AspNetCore.Mvc;
using Opas.Infrastructure.Services;
using Opas.Shared.Logging;
using System.ComponentModel.DataAnnotations;

namespace Opas.Api.Endpoints;

public static class SuperAdminEndpoints
{
    public sealed record SuperAdminLoginRequest(
        [Required] string Username,
        [Required] string Password
    );

    public sealed record SuperAdminLoginResponse(
        bool Success,
        string Message,
        SuperAdminUserInfo? User,
        string? Token  // Future: JWT token
    );

    public sealed record SuperAdminUserInfo(
        int Id,
        string Username,
        string Email,
        string FullName,
        List<string> Permissions,
        DateTime? LastLoginAt
    );

    public sealed record CreateSuperAdminRequest(
        [Required] string Username,
        [Required] string Email,
        [Required] string Password,
        [Required] string FirstName,
        [Required] string LastName,
        string? Phone,
        List<string>? Permissions
    );

    public sealed record CreateSuperAdminResponse(
        bool Success,
        string Message,
        int? SuperAdminId
    );

    public static IEndpointRouteBuilder MapSuperAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/superadmin")
            .WithTags("SuperAdmin Authentication")
            .WithDescription("SuperAdmin authentication and management");

        // SuperAdmin Login
        group.MapPost("/login", async (
            [FromBody] SuperAdminLoginRequest request,
            [FromServices] SuperAdminAuthService authService,
            [FromServices] IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            try
            {
                var result = await authService.AuthenticateAsync(
                    request.Username, 
                    request.Password, 
                    clientIP, 
                    httpContext.RequestAborted
                );

                if (!result.Success)
                {
                    return Results.Unauthorized();
                }

                var user = result.User!;
                var userInfo = new SuperAdminUserInfo(
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.PermissionList,
                    user.LastLoginAt
                );

                return Results.Ok(new SuperAdminLoginResponse(
                    true,
                    "Login successful",
                    userInfo,
                    null  // Future: Generate JWT token here
                ));
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("SuperAdminLogin", "Login error", new
                {
                    Username = request.Username,
                    Error = ex.Message,
                    ClientIP = clientIP
                });

                return Results.Problem(
                    title: "Login Error",
                    detail: "An error occurred during login",
                    statusCode: 500
                );
            }
        })
        .WithName("SuperAdminLogin")
        .WithSummary("SuperAdmin login")
        .WithDescription("Authenticate SuperAdmin user and return user info");

        // Create SuperAdmin (Initialization endpoint)
        group.MapPost("/create", async (
            [FromBody] CreateSuperAdminRequest request,
            [FromServices] SuperAdminAuthService authService,
            [FromServices] IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            try
            {
                var createRequest = new Infrastructure.Services.CreateSuperAdminRequest
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = request.Password,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    Permissions = request.Permissions,
                    IsEmailVerified = true,  // For initial setup
                    CreatedBy = "INITIAL_SETUP"
                };

                var result = await authService.CreateSuperAdminAsync(createRequest, httpContext.RequestAborted);

                if (!result.Success)
                {
                    return Results.BadRequest(new CreateSuperAdminResponse(
                        false,
                        result.Message,
                        null
                    ));
                }

                opasLogger.LogSystemEvent("SuperAdminCreation", "SuperAdmin created via API", new
                {
                    Username = request.Username,
                    Email = request.Email,
                    CreatedBy = "INITIAL_SETUP",
                    ClientIP = clientIP
                });

                return Results.Ok(new CreateSuperAdminResponse(
                    true,
                    result.Message,
                    result.SuperAdminId
                ));
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("SuperAdminCreation", "Creation error", new
                {
                    Username = request.Username,
                    Error = ex.Message,
                    ClientIP = clientIP
                });

                return Results.Problem(
                    title: "SuperAdmin Creation Error",
                    detail: "An error occurred during SuperAdmin creation",
                    statusCode: 500
                );
            }
        })
        .WithName("CreateSuperAdmin")
        .WithSummary("Create SuperAdmin user")
        .WithDescription("Create a new SuperAdmin user (for initial system setup)");

        // SuperAdmin Status
        group.MapGet("/status", (
            [FromServices] SuperAdminAuthService authService,
            HttpContext httpContext) =>
        {
            try
            {
                // Bu endpoint daha sonra JWT token validation ile korunacak
                return Results.Ok(new
                {
                    Status = "SuperAdmin service active",
                    Timestamp = DateTime.UtcNow,
                    Message = "Use /login to authenticate"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "SuperAdmin Status Error",
                    detail: $"Error retrieving SuperAdmin status: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("SuperAdminStatus")
        .WithSummary("Get SuperAdmin service status")
        .WithDescription("Check if SuperAdmin service is running");

        return app;
    }
}
