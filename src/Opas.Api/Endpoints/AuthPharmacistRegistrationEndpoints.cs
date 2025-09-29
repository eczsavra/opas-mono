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
            [FromServices] PublicDbContext publicDb,
            [FromServices] ControlPlaneDbContext controlDb,
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
                    var existingByGln = await controlDb.Tenants
                        .AsNoTracking()
                        .AnyAsync(x => x.Gln == dto.PersonalGln, ct);
                        
                    if (existingByGln)
                    {
                        opasLogger.LogSystemEvent("RegistrationGLNConflict", $"GLN already registered: {dto.PersonalGln}", new { GLN = dto.PersonalGln });
                        return Results.Conflict(new { success = false, error = "Bu GLN ile zaten kayıt yapılmış" });
                    }

                    // Check if username already taken
                    var existingByUsername = await controlDb.TenantsUsernames
                        .AsNoTracking()
                        .AnyAsync(x => x.Username == dto.Username.ToLowerInvariant(), ct);
                        
                    if (existingByUsername)
                    {
                        opasLogger.LogSystemEvent("RegistrationUsernameConflict", $"Username already taken: {dto.Username}", new { Username = dto.Username });
                        return Results.Conflict(new { success = false, error = "Bu kullanıcı adı zaten alınmış" });
                    }

                    // Check if email already used
                    var existingByEmail = await controlDb.Tenants
                        .AsNoTracking()
                        .AnyAsync(x => x.Email == dto.Email.ToLowerInvariant(), ct);
                        
                    if (existingByEmail)
                    {
                        opasLogger.LogSystemEvent("RegistrationEmailConflict", $"Email already used: {dto.Email}", new { Email = dto.Email });
                        return Results.Conflict(new { success = false, error = "Bu email zaten kullanılıyor" });
                    }

                    // Generate Smart IDs (unique across all types)
                    string tenantId;
                    do
                    {
                        tenantId = SmartIdGenerator.GenerateTenantId();
                    } while (await controlDb.Tenants.AnyAsync(x => x.TId == tenantId, ct));

                    // Log ID generation
                    opasLogger.LogSystemEvent("RegistrationIDGeneration", $"Generated IDs for new tenant", new { 
                        TenantId = tenantId 
                    });

                    // Hash password
                    var (hashedPassword, salt) = HashPassword(dto.Password);

                // Create Tenant record
                var tenant = new Tenant
                {
                    TId = tenantId,
                    Gln = dto.PersonalGln,
                    Type = "eczane",
                    EczaneAdi = null, // Will be filled from GLN registry if available
                    Ili = null, // Will be filled from GLN registry if available
                    Ilcesi = null, // Will be filled from GLN registry if available
                    IsActive = true,
                    Ad = dto.FirstName,
                    Soyad = dto.LastName,
                    TcNo = dto.TcNumber,
                    DogumYili = dto.BirthYear,
                    IsNviVerified = dto.IsNviVerified,
                    Email = dto.Email.ToLowerInvariant(),
                    IsEmailVerified = dto.IsEmailVerified,
                    CepTel = dto.Phone,
                    IsCepTelVerified = dto.IsPhoneVerified,
                    Username = dto.Username.ToLowerInvariant(),
                    Password = hashedPassword, // Store hashed password directly
                    IsCompleted = false,
                    KayitOlusturulmaZamani = DateTime.UtcNow,
                    KayitGuncellenmeZamani = null,
                    KayitSilinmeZamani = null
                };

                controlDb.Tenants.Add(tenant);
                
                // Add to tenants_usernames for unique username control
                var tenantUsername = new TenantUsername
                {
                    TId = tenantId,
                    Username = dto.Username.ToLowerInvariant()
                };
                
                controlDb.TenantsUsernames.Add(tenantUsername);
                await controlDb.SaveChangesAsync(ct);

                // Try to enrich from GLN registry if exists
                var glnInfo = await publicDb.GlnRegistry
                    .AsNoTracking()
                    .Where(x => x.Gln == dto.PersonalGln)
                    .Select(x => new { x.CompanyName, x.City, x.Town })
                    .SingleOrDefaultAsync(ct);

                // Update tenant with GLN registry info if available
                if (glnInfo != null)
                {
                    tenant.EczaneAdi = glnInfo.CompanyName;
                    tenant.Ili = glnInfo.City;
                    tenant.Ilcesi = glnInfo.Town;
                    await controlDb.SaveChangesAsync(ct);
                }

                    // Log successful registration
                    opasLogger.LogSystemEvent("RegistrationSuccess", $"Tenant registration completed successfully", new { 
                        TenantId = tenantId, 
                        GLN = dto.PersonalGln,
                        Username = dto.Username,
                        Email = dto.Email
                    });

                    // Log to management system
                    managementLogging.LogSystemEvent("NewTenantRegistered", $"New tenant registered: {dto.Username}", new {
                        TenantId = tenantId,
                        GLN = dto.PersonalGln,
                        IP = clientIP
                    });

                    // Log to tenant-specific logs (when tenant is active)
                    tenantLogging.LogTenantActivity(tenantId, tenantId, "RegistrationCompleted", new {
                        Username = dto.Username,
                        Email = dto.Email,
                        GLN = dto.PersonalGln
                    });

                    return Results.Created($"/api/auth/tenant/{tenantId}", new
                    {
                        success = true,
                        tenantId = tenantId,
                        username = tenant.Username,
                        message = "Tenant kaydı başarıyla oluşturuldu."
                    });
                }
                catch (Exception ex)
                {
                    // Log registration failure
                    opasLogger.LogError(ex, $"Tenant registration failed for {dto.Username}", new {
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
