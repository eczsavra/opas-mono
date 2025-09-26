using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Infrastructure.Logging;
using Opas.Shared.Auth;
using Opas.Shared.Common;
using Opas.Shared.ControlPlane;
using Opas.Shared.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Opas.Api.Endpoints;

public static class AuthPharmacistRegistrationEndpoints
{
    public static IEndpointRouteBuilder MapAuthPharmacistRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/auth/pharmacist/register
        app.MapPost("/api/auth/pharmacist/register", async (
            [FromBody] PharmacistRegistrationDto dto,
            [FromServices] ControlPlaneDbContext db,
            [FromServices] IOpasLogger opasLogger,
            [FromServices] TenantLoggingService tenantLogging,
            [FromServices] ManagementLoggingService managementLogging,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var correlationId = httpContext.TraceIdentifier;
            
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    // Log registration attempt
                    opasLogger.LogSystemEvent("RegistrationAttempt", $"New pharmacist registration attempt from {clientIP}", new { 
                        GLN = dto.PersonalGln, 
                        Username = dto.Username, 
                        Email = dto.Email 
                    });

                    // Validation
                    if (!IsValidRegistration(dto, out var validationError))
                    {
                        opasLogger.LogSystemEvent("RegistrationValidationFailed", $"Registration validation failed: {validationError}", new { 
                            GLN = dto.PersonalGln, 
                            Username = dto.Username 
                        });
                        return Results.BadRequest(new { success = false, error = validationError });
                    }

                    // Check if GLN already registered
                    var existingByGln = await db.PharmacistAdmins
                        .AsNoTracking()
                        .AnyAsync(x => x.PersonalGln == dto.PersonalGln, ct);
                        
                    if (existingByGln)
                    {
                        opasLogger.LogSystemEvent("RegistrationGLNConflict", $"GLN already registered: {dto.PersonalGln}", new { GLN = dto.PersonalGln });
                        return Results.Conflict(new { success = false, error = "Bu GLN ile zaten kayıt yapılmış" });
                    }

                    // Check if username already taken
                    var existingByUsername = await db.PharmacistAdmins
                        .AsNoTracking()
                        .AnyAsync(x => x.Username == dto.Username.ToLowerInvariant(), ct);
                        
                    if (existingByUsername)
                    {
                        opasLogger.LogSystemEvent("RegistrationUsernameConflict", $"Username already taken: {dto.Username}", new { Username = dto.Username });
                        return Results.Conflict(new { success = false, error = "Bu kullanıcı adı zaten alınmış" });
                    }

                    // Check if email already used
                    var existingByEmail = await db.PharmacistAdmins
                        .AsNoTracking()
                        .AnyAsync(x => x.Email == dto.Email.ToLowerInvariant(), ct);
                        
                    if (existingByEmail)
                    {
                        opasLogger.LogSystemEvent("RegistrationEmailConflict", $"Email already used: {dto.Email}", new { Email = dto.Email });
                        return Results.Conflict(new { success = false, error = "Bu email zaten kullanılıyor" });
                    }

                    // Generate Smart IDs (unique across all types)
                    string pharmacistId;
                    do
                    {
                        pharmacistId = SmartIdGenerator.GeneratePharmacistId();
                    } while (await db.PharmacistAdmins.AnyAsync(x => x.PharmacistId == pharmacistId, ct) ||
                             await db.SubUsers.AnyAsync(x => x.SubUserId == pharmacistId, ct));

                    string tenantId;
                    do
                    {
                        tenantId = SmartIdGenerator.GenerateTenantId();
                    } while (await db.PharmacistAdmins.AnyAsync(x => x.TenantId == tenantId, ct));

                    // Log ID generation
                    opasLogger.LogSystemEvent("RegistrationIDGeneration", $"Generated IDs for new pharmacist", new { 
                        PharmacistId = pharmacistId, 
                        TenantId = tenantId 
                    });

                    // Hash password
                    var (hashedPassword, salt) = HashPassword(dto.Password);

                // Create PharmacistAdmin record
                var pharmacist = new PharmacistAdmin
                {
                    PharmacistId = pharmacistId, // Smart ID
                    Username = dto.Username.ToLowerInvariant(),
                    PasswordHash = hashedPassword,
                    PasswordSalt = salt,
                    Email = dto.Email.ToLowerInvariant(),
                    Phone = dto.Phone,
                    PersonalGln = dto.PersonalGln,
                    TenantId = tenantId,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    TcNumber = dto.TcNumber,
                    BirthYear = dto.BirthYear,
                    PharmacyRegistrationNo = dto.PharmacyRegistrationNo,
                    IsEmailVerified = dto.IsEmailVerified,
                    IsPhoneVerified = dto.IsPhoneVerified,
                    IsNviVerified = dto.IsNviVerified,
                    TenantStatus = "Pending", // C Approach: Staged Provisioning
                    Role = "PharmacyAdmin"
                };

                db.PharmacistAdmins.Add(pharmacist);
                await db.SaveChangesAsync(ct);

                // Create TenantRecord immediately (staged provisioning)
                // Try to enrich from GLN registry if exists
                var glnInfo = await db.GlnRegistry
                    .AsNoTracking()
                    .Where(x => x.Gln == dto.PersonalGln)
                    .Select(x => new { x.CompanyName, x.City, x.Town })
                    .SingleOrDefaultAsync(ct);

                var databaseName = $"tenant_{tenantId.ToLowerInvariant()}";
                var tenantConnectionString =
                    $"Host=127.0.0.1;Port=5432;Database={databaseName};Username=postgres;Password=postgres";

                var tenantRecord = new TenantRecord
                {
                    TenantId = tenantId,
                    PharmacistGln = dto.PersonalGln,
                    PharmacyName = glnInfo?.CompanyName ?? "Unknown Pharmacy",
                    PharmacyRegistrationNo = dto.PharmacyRegistrationNo,
                    City = glnInfo?.City,
                    District = glnInfo?.Town,
                    TenantConnectionString = tenantConnectionString,
                    Status = "Provisioning",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                    db.Tenants.Add(tenantRecord);
                    await db.SaveChangesAsync(ct);

                    // Log successful registration
                    opasLogger.LogSystemEvent("RegistrationSuccess", $"Pharmacist registration completed successfully", new { 
                        PharmacistId = pharmacist.PharmacistId, 
                        TenantId = tenantId, 
                        GLN = dto.PersonalGln,
                        Username = dto.Username,
                        Email = dto.Email
                    });

                    // Log to management system
                    managementLogging.LogSystemEvent("NewPharmacistRegistered", $"New pharmacist registered: {dto.Username}", new {
                        PharmacistId = pharmacist.PharmacistId,
                        TenantId = tenantId,
                        GLN = dto.PersonalGln,
                        IP = clientIP
                    });

                    // Log to tenant-specific logs (when tenant is active)
                    tenantLogging.LogTenantActivity(tenantId, pharmacist.PharmacistId, "RegistrationCompleted", new {
                        Username = dto.Username,
                        Email = dto.Email,
                        GLN = dto.PersonalGln
                    });

                    return Results.Created($"/api/auth/pharmacist/{pharmacist.Id}", new
                    {
                        success = true,
                        pharmacistId = pharmacist.PharmacistId, // Smart ID
                        tenantId = tenantId,
                        username = pharmacist.Username,
                        message = "Eczacı kaydı başarıyla oluşturuldu. Tenant provisioning işlemi başlatıldı."
                    });
                }
                catch (Exception ex)
                {
                    // Log registration failure
                    opasLogger.LogError(ex, $"Pharmacist registration failed for {dto.Username}", new {
                        GLN = dto.PersonalGln,
                        Username = dto.Username,
                        Email = dto.Email,
                        IP = clientIP
                    });
                    
                    return Results.Problem("Kayıt işlemi sırasında bir hata oluştu", statusCode: 500);
                }
            }
        })
        .WithName("RegisterPharmacistAdmin")
        .WithOpenApi();

        return app;
    }

    // Tenant ID generator moved to SmartIdGenerator

    // Password hashing with salt
    private static (string hashedPassword, string salt) HashPassword(string password)
    {
        // Generate a random salt
        byte[] saltBytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        
        string salt = Convert.ToBase64String(saltBytes);
        
        // Hash password with salt
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256))
        {
            byte[] hashBytes = pbkdf2.GetBytes(32);
            string hashedPassword = Convert.ToBase64String(hashBytes);
            return (hashedPassword, salt);
        }
    }

    // Validation helper
    private static bool IsValidRegistration(PharmacistRegistrationDto dto, out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Length < 3 || dto.Username.Length > 50)
        {
            error = "Kullanıcı adı 3-50 karakter olmalı";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
        {
            error = "Parola en az 8 karakter olmalı";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains("@"))
        {
            error = "Geçerli bir email adresi giriniz";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.PersonalGln) || dto.PersonalGln.Length != 13)
        {
            error = "GLN 13 haneli olmalı";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
        {
            error = "Ad ve soyad boş olamaz";
            return false;
        }

        return true;
    }
}

// DTO for pharmacist registration
public class PharmacistRegistrationDto
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public string PersonalGln { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? TcNumber { get; set; }
    public int? BirthYear { get; set; }
    public string? PharmacyRegistrationNo { get; set; }
    public bool IsEmailVerified { get; set; } = false;
    public bool IsPhoneVerified { get; set; } = false;
    public bool IsNviVerified { get; set; } = false;
}
