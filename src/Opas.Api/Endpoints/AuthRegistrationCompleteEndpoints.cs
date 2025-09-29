using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Infrastructure.Services;
using Opas.Shared.ControlPlane;
using Opas.Shared.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace Opas.Api.Endpoints;

public static class AuthRegistrationCompleteEndpoints
{
    public sealed record RegisterCompleteRequest(
        [Required] string Token,
        [Required] string Username,
        [Required] string Password,
        [Required] string Email,
        [Required] string Phone,
        [Required] string FirstName,
        [Required] string LastName,
        [Required] string TcNo,
        [Required] int BirthYear,
        string? PharmacyName,
        string? PharmacyRegistrationNo,
        string? City,
        string? District
    );

    public sealed record RegisterCompleteResponse(
        bool Success,
        string? Message,
        string? TenantId,
        string? Username
    );

    public static IEndpointRouteBuilder MapAuthRegistrationCompleteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register/complete", async (
            [FromBody] RegisterCompleteRequest request,
            [FromServices] PublicDbContext publicDb,
            [FromServices] ControlPlaneDbContext controlDb,
            [FromServices] TenantProvisioningService provisioningService,
            [FromServices] IOpasLogger opasLogger,
            HttpContext httpContext) =>
        {
            var clientIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            try
            {
                // 1. Token'dan GLN'i bul (şimdilik basit validation)
                // TODO: Token cache/db'den kontrol edilecek
                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    return Results.BadRequest(new RegisterCompleteResponse(false, "Invalid token", null, null));
                }

                // 2. Username benzersizliği kontrolü
                var existingUser = await controlDb.PharmacistAdmins
                    .FirstOrDefaultAsync(p => p.Username == request.Username);
                    
                if (existingUser != null)
                {
                    return Results.BadRequest(new RegisterCompleteResponse(false, "Username already exists", null, null));
                }

                // 3. TC No benzersizliği kontrolü
                var existingTc = await controlDb.PharmacistAdmins
                    .FirstOrDefaultAsync(p => p.TcNumber == request.TcNo);
                    
                if (existingTc != null)
                {
                    return Results.BadRequest(new RegisterCompleteResponse(false, "TC number already registered", null, null));
                }

                // 4. GLN'i token'dan al (şimdilik hardcoded test için)
                // TODO: Gerçek token->GLN mapping
                var pharmacistGln = "8680001000000"; // Test için sabit GLN

                // GLN registry'den bilgileri al - Public DB'den
                var glnInfo = await publicDb.GlnRegistry
                    .FirstOrDefaultAsync(g => g.Gln == pharmacistGln);

                var pharmacyName = request.PharmacyName ?? glnInfo?.CompanyName ?? "Unknown Pharmacy";
                var city = request.City ?? glnInfo?.City ?? "";
                var district = request.District ?? glnInfo?.Town ?? "";

                // 5. Tenant ID oluştur
                var tenantId = $"TNT_{new Random().Next(100000, 999999)}";
                var pharmacistId = $"PHM_{new Random().Next(100000, 999999)}";

                // 6. Password hash
                var salt = GenerateSalt();
                var passwordHash = HashPassword(request.Password, salt);

                // 7. Transaction başlat
                using var transaction = await controlDb.Database.BeginTransactionAsync();

                try
                {
                    // 8. Tenant kaydı oluştur
                    var tenantRecord = new TenantRecord
                    {
                        TenantId = tenantId,
                        PharmacistGln = pharmacistGln,
                        PharmacyName = pharmacyName,
                        PharmacyRegistrationNo = request.PharmacyRegistrationNo,
                        City = city,
                        District = district,
                        TenantConnectionString = "", // Provisioning sonrası güncellenecek
                        Status = "Provisioning",
                        CreatedAt = DateTime.UtcNow
                    };

                    controlDb.TenantRecords.Add(tenantRecord);

                    // 9. PharmacistAdmin kaydı oluştur
                    var pharmacistAdmin = new
                    {
                        Id = 0,
                        PharmacistId = pharmacistId,
                        Username = request.Username,
                        PasswordHash = passwordHash,
                        PasswordSalt = salt,
                        Email = request.Email,
                        Phone = request.Phone,
                        PersonalGln = pharmacistGln,
                        TenantId = tenantId,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        TcNumber = request.TcNo,
                        BirthYear = request.BirthYear,
                        PharmacyRegistrationNo = request.PharmacyRegistrationNo,
                        IsActive = true,
                        IsEmailVerified = false,
                        IsPhoneVerified = false,
                        IsNviVerified = false,
                        TenantStatus = "Active",
                        CreatedAt = DateTime.UtcNow,
                        Role = "PharmacyAdmin"
                    };

                    // SQL ile insert (EF Core entity olmadığı için)
                    var insertSql = @"
                        INSERT INTO pharmacist_admins (
                            pharmacist_id, username, password_hash, password_salt,
                            email, phone, personal_gln, tenant_id,
                            first_name, last_name, tc_number, birth_year,
                            pharmacy_registration_no, is_active, is_email_verified,
                            is_phone_verified, is_nvi_verified, tenant_status,
                            created_at, role
                        ) VALUES (
                            @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7,
                            @p8, @p9, @p10, @p11, @p12, @p13, @p14,
                            @p15, @p16, @p17, @p18, @p19
                        )";

                    await controlDb.Database.ExecuteSqlRawAsync(insertSql,
                        pharmacistAdmin.PharmacistId,
                        pharmacistAdmin.Username,
                        pharmacistAdmin.PasswordHash,
                        pharmacistAdmin.PasswordSalt,
                        pharmacistAdmin.Email,
                        pharmacistAdmin.Phone,
                        pharmacistAdmin.PersonalGln,
                        pharmacistAdmin.TenantId,
                        pharmacistAdmin.FirstName,
                        pharmacistAdmin.LastName,
                        pharmacistAdmin.TcNumber,
                        pharmacistAdmin.BirthYear,
                        pharmacistAdmin.PharmacyRegistrationNo ?? (object)DBNull.Value,
                        pharmacistAdmin.IsActive,
                        pharmacistAdmin.IsEmailVerified,
                        pharmacistAdmin.IsPhoneVerified,
                        pharmacistAdmin.IsNviVerified,
                        pharmacistAdmin.TenantStatus,
                        pharmacistAdmin.CreatedAt,
                        pharmacistAdmin.Role);

                    await controlDb.SaveChangesAsync();

                    // 10. Tenant DB'yi provision et
                    var (success, connectionString, message) = await provisioningService.ProvisionTenantDatabaseAsync(
                        tenantId,
                        pharmacistGln,
                        pharmacyName);

                    if (!success)
                    {
                        await transaction.RollbackAsync();
                        return Results.Problem($"Tenant provisioning failed: {message}", statusCode: 500);
                    }

                    // 11. Tenant connection string'i güncelle
                    tenantRecord.TenantConnectionString = connectionString;
                    tenantRecord.Status = "Active";
                    await controlDb.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Log successful registration
                    opasLogger.LogSystemEvent("UserRegistration", "New pharmacy registered", new
                    {
                        TenantId = tenantId,
                        Username = request.Username,
                        PharmacyName = pharmacyName,
                        Gln = pharmacistGln,
                        ClientIP = clientIP
                    });

                    return Results.Ok(new RegisterCompleteResponse(
                        true,
                        "Registration completed successfully",
                        tenantId,
                        request.Username));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                opasLogger.LogSystemEvent("RegistrationError", ex.Message, new
                {
                    Username = request.Username,
                    ClientIP = clientIP,
                    Error = ex.ToString()
                });

                return Results.Problem("Registration failed. Please try again.", statusCode: 500);
            }
        })
        .WithName("AuthRegisterComplete")
        .WithTags("Authentication")
        .WithDescription("Complete pharmacy registration with tenant provisioning");

        return app;
    }

    private static string GenerateSalt()
    {
        var buffer = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }
        return Convert.ToBase64String(buffer);
    }

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
