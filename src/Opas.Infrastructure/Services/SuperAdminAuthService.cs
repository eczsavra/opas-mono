using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Opas.Infrastructure.Persistence;
using Opas.Shared.ControlPlane;
using Opas.Shared.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Opas.Infrastructure.Services;

/// <summary>
/// SuperAdmin authentication ve yetkilendirme servisi
/// </summary>
public class SuperAdminAuthService
{
    private readonly ILogger<SuperAdminAuthService> _logger;
    private readonly IOpasLogger _opasLogger;
    private readonly ControlPlaneDbContext _controlPlaneDb;

    public SuperAdminAuthService(
        ILogger<SuperAdminAuthService> logger,
        IOpasLogger opasLogger,
        ControlPlaneDbContext controlPlaneDb)
    {
        _logger = logger;
        _opasLogger = opasLogger;
        _controlPlaneDb = controlPlaneDb;
    }

    /// <summary>
    /// SuperAdmin authentication
    /// </summary>
    public async Task<SuperAdminAuthResult> AuthenticateAsync(string username, string password, string? clientIp = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("SuperAdmin authentication attempt for user: {Username}", username);

            // SuperAdmin'i bul
            var superAdmin = await _controlPlaneDb.SuperAdmins
                .FirstOrDefaultAsync(sa => sa.Username == username && sa.IsActive, ct);

            if (superAdmin == null)
            {
                _opasLogger.LogSystemEvent("SuperAdminAuth", "User not found", new 
                { 
                    Username = username, 
                    ClientIP = clientIp 
                });

                return new SuperAdminAuthResult
                {
                    Success = false,
                    Message = "Invalid credentials",
                    User = null
                };
            }

            // Password doğrula
            var hashedPassword = HashPassword(password, superAdmin.PasswordSalt);
            if (hashedPassword != superAdmin.PasswordHash)
            {
                _opasLogger.LogSystemEvent("SuperAdminAuth", "Invalid password", new 
                { 
                    Username = username, 
                    ClientIP = clientIp 
                });

                return new SuperAdminAuthResult
                {
                    Success = false,
                    Message = "Invalid credentials",
                    User = null
                };
            }

            // Son giriş bilgilerini güncelle
            superAdmin.LastLoginAt = DateTime.UtcNow;
            superAdmin.LastLoginIp = clientIp;
            await _controlPlaneDb.SaveChangesAsync(ct);

            _logger.LogInformation("SuperAdmin authentication successful for user: {Username}", username);
            _opasLogger.LogSystemEvent("SuperAdminAuth", "Authentication successful", new 
            { 
                Username = username, 
                UserId = superAdmin.Id,
                ClientIP = clientIp 
            });

            return new SuperAdminAuthResult
            {
                Success = true,
                Message = "Authentication successful",
                User = superAdmin
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SuperAdmin authentication for user: {Username}", username);
            _opasLogger.LogSystemEvent("SuperAdminAuth", "Authentication error", new 
            { 
                Username = username, 
                Error = ex.Message,
                ClientIP = clientIp 
            });

            return new SuperAdminAuthResult
            {
                Success = false,
                Message = "Authentication failed due to system error",
                User = null
            };
        }
    }

    /// <summary>
    /// SuperAdmin oluştur (initial setup için)
    /// </summary>
    public async Task<SuperAdminCreationResult> CreateSuperAdminAsync(CreateSuperAdminRequest request, CancellationToken ct = default)
    {
        try
        {
            // Username benzersizlik kontrolü
            var existingUser = await _controlPlaneDb.SuperAdmins
                .AnyAsync(sa => sa.Username == request.Username || sa.Email == request.Email, ct);

            if (existingUser)
            {
                return new SuperAdminCreationResult
                {
                    Success = false,
                    Message = "Username or email already exists"
                };
            }

            // Password hash oluştur
            var salt = GenerateSalt();
            var passwordHash = HashPassword(request.Password, salt);

            // Yetkileri JSON olarak serialize et
            var permissions = JsonSerializer.Serialize(request.Permissions ?? new List<string>
            {
                "GLOBAL_ACCESS",
                "TENANT_MANAGEMENT", 
                "SYSTEM_SETTINGS",
                "USER_MANAGEMENT"
            });

            // SuperAdmin oluştur
            var superAdmin = new SuperAdmin
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                PasswordSalt = salt,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                Permissions = permissions,
                IsActive = true,
                IsEmailVerified = request.IsEmailVerified,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy ?? "SYSTEM"
            };

            _controlPlaneDb.SuperAdmins.Add(superAdmin);
            await _controlPlaneDb.SaveChangesAsync(ct);

            _logger.LogInformation("SuperAdmin created successfully: {Username}", request.Username);
            _opasLogger.LogSystemEvent("SuperAdminCreation", "SuperAdmin created", new 
            { 
                Username = request.Username,
                Email = request.Email,
                CreatedBy = request.CreatedBy 
            });

            return new SuperAdminCreationResult
            {
                Success = true,
                Message = "SuperAdmin created successfully",
                SuperAdminId = superAdmin.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SuperAdmin: {Username}", request.Username);
            return new SuperAdminCreationResult
            {
                Success = false,
                Message = $"Failed to create SuperAdmin: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// SuperAdmin yetki kontrolü
    /// </summary>
    public async Task<bool> HasPermissionAsync(int superAdminId, string permission, CancellationToken ct = default)
    {
        try
        {
            var superAdmin = await _controlPlaneDb.SuperAdmins
                .FirstOrDefaultAsync(sa => sa.Id == superAdminId && sa.IsActive, ct);

            if (superAdmin == null) return false;

            var permissions = JsonSerializer.Deserialize<List<string>>(superAdmin.Permissions) ?? new List<string>();
            return permissions.Contains(permission) || permissions.Contains("GLOBAL_ACCESS");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SuperAdmin permission for user: {UserId}", superAdminId);
            return false;
        }
    }

    /// <summary>
    /// Salt oluştur
    /// </summary>
    private static string GenerateSalt()
    {
        var buffer = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }
        return Convert.ToBase64String(buffer);
    }

    /// <summary>
    /// Password hash oluştur
    /// </summary>
    private static string HashPassword(string password, string salt)
    {
        using (var sha512 = SHA512.Create())
        {
            var saltedPassword = password + salt;
            var bytes = Encoding.UTF8.GetBytes(saltedPassword);
            var hash = sha512.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}

/// <summary>
/// SuperAdmin authentication sonucu
/// </summary>
public sealed record SuperAdminAuthResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public SuperAdmin? User { get; init; }
}

/// <summary>
/// SuperAdmin oluşturma sonucu
/// </summary>
public sealed record SuperAdminCreationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? SuperAdminId { get; init; }
}

/// <summary>
/// SuperAdmin oluşturma talebi
/// </summary>
public sealed record CreateSuperAdminRequest
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public List<string>? Permissions { get; init; }
    public bool IsEmailVerified { get; init; } = false;
    public string? CreatedBy { get; init; }
}
