using Microsoft.EntityFrameworkCore;
using Opas.Shared.ControlPlane;
using Opas.Shared.Auth;
using Opas.Domain.Entities;
using Opas.Domain.ValueObjects;
using System.Text.Json;

namespace Opas.Infrastructure.Persistence;

/// <summary>
/// Tek bir Control Plane DB:
/// - GLN → TenantConnectionString eşleşmesi
/// - Kayıt/Provisioning meta verileri
/// </summary>
public sealed class ControlPlaneDbContext : DbContext
{
    public DbSet<TenantRecord> Tenants => Set<TenantRecord>();
    public DbSet<TokenStore> Tokens => Set<TokenStore>();
    public DbSet<User> Users => Set<User>(); // LEGACY - will be migrated to PharmacistAdmin
    public DbSet<PharmacistAdmin> PharmacistAdmins => Set<PharmacistAdmin>(); // NEW - main pharmacist accounts
    public DbSet<SubUser> SubUsers => Set<SubUser>(); // NEW - pharmacy employee accounts
    public DbSet<LogEntry> LogEntries => Set<LogEntry>(); // NEW - application logs
    public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>(); // NEW - system administrators


    public ControlPlaneDbContext(DbContextOptions<ControlPlaneDbContext> options)
        : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TenantRecord mapping moved below (new structure with TenantId as PK)


        // TokenStore mapping
        modelBuilder.Entity<TokenStore>(e =>
        {
            e.ToTable("token_store");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            e.Property(x => x.Token).HasColumnName("token").IsRequired();
            e.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // User mapping (LEGACY - keeping for backward compatibility)
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users_legacy");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Username).HasColumnName("username").IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Username).IsUnique(); // Username must be unique
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
            e.Property(x => x.PasswordSalt).HasColumnName("password_salt").IsRequired().HasMaxLength(128);
            e.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Email).IsUnique(); // Email must be unique
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(15);
            e.Property(x => x.PharmacyGln).HasColumnName("pharmacy_gln").IsRequired().HasMaxLength(13);
            e.Property(x => x.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
            e.Property(x => x.TcNumber).HasColumnName("tc_number").HasMaxLength(11);
            e.Property(x => x.BirthYear).HasColumnName("birth_year");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.IsEmailVerified).HasColumnName("is_email_verified");
            e.Property(x => x.IsPhoneVerified).HasColumnName("is_phone_verified");
            e.Property(x => x.IsNviVerified).HasColumnName("is_nvi_verified");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            e.Property(x => x.Role).HasColumnName("role").HasMaxLength(50);
        });

        // PharmacistAdmin mapping (NEW - main pharmacist accounts)
        modelBuilder.Entity<PharmacistAdmin>(e =>
        {
            e.ToTable("pharmacist_admins");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PharmacistId).HasColumnName("pharmacist_id").IsRequired().HasMaxLength(10);
            e.HasIndex(x => x.PharmacistId).IsUnique(); // Smart ID must be unique globally
            e.Property(x => x.Username).HasColumnName("username").IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Username).IsUnique(); // Username must be unique globally
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
            e.Property(x => x.PasswordSalt).HasColumnName("password_salt").IsRequired().HasMaxLength(128);
            e.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Email).IsUnique(); // Email must be unique globally
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(15);
            e.Property(x => x.PersonalGln).HasColumnName("personal_gln").IsRequired().HasMaxLength(13);
            e.HasIndex(x => x.PersonalGln).IsUnique(); // One pharmacist per GLN
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.TenantId).IsUnique(); // One pharmacist admin per tenant
            e.Property(x => x.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
            e.Property(x => x.TcNumber).HasColumnName("tc_number").HasMaxLength(11);
            e.Property(x => x.BirthYear).HasColumnName("birth_year");
            e.Property(x => x.PharmacyRegistrationNo).HasColumnName("pharmacy_registration_no").HasMaxLength(20);
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.IsEmailVerified).HasColumnName("is_email_verified");
            e.Property(x => x.IsPhoneVerified).HasColumnName("is_phone_verified");
            e.Property(x => x.IsNviVerified).HasColumnName("is_nvi_verified");
            e.Property(x => x.TenantStatus).HasColumnName("tenant_status").HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            e.Property(x => x.Role).HasColumnName("role").HasMaxLength(50);
        });

        // SubUser mapping (NEW - pharmacy employee accounts)
        modelBuilder.Entity<SubUser>(e =>
        {
            e.ToTable("sub_users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SubUserId).HasColumnName("sub_user_id").IsRequired().HasMaxLength(10);
            e.HasIndex(x => x.SubUserId).IsUnique(); // Smart ID must be unique globally
            e.Property(x => x.Username).HasColumnName("username").IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Username).IsUnique(); // Username must be unique globally
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
            e.Property(x => x.PasswordSalt).HasColumnName("password_salt").IsRequired().HasMaxLength(128);
            e.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Email).IsUnique(); // Email must be unique globally
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(15);
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired().HasMaxLength(50);
            e.Property(x => x.CreatedByPharmacistAdminId).HasColumnName("created_by_pharmacist_admin_id");
            e.Property(x => x.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
            e.Property(x => x.TcNumber).HasColumnName("tc_number").HasMaxLength(11);
            e.Property(x => x.BirthYear).HasColumnName("birth_year");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            e.Property(x => x.Role).HasColumnName("role").HasMaxLength(50);
        });

        // TenantRecord mapping (UPDATED for new TenantId structure)
        modelBuilder.Entity<TenantRecord>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(x => x.TenantId); // PRIMARY KEY is now TenantId, not GLN
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired().HasMaxLength(50);
            e.Property(x => x.PharmacistGln).HasColumnName("pharmacist_gln").IsRequired().HasMaxLength(13);
            e.HasIndex(x => x.PharmacistGln); // Index but not unique (GLN can change)
            e.Property(x => x.PharmacyName).HasColumnName("pharmacy_name").IsRequired().HasMaxLength(200);
            e.Property(x => x.PharmacyRegistrationNo).HasColumnName("pharmacy_registration_no").HasMaxLength(20);
            e.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
            e.Property(x => x.District).HasColumnName("district").HasMaxLength(100);
            e.Property(x => x.TenantConnectionString).HasColumnName("tenant_connection_string").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(50);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        // LogEntry configuration
        modelBuilder.Entity<LogEntry>(e =>
        {
            e.ToTable("log_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Level).HasColumnName("level").HasMaxLength(50);
            e.Property(x => x.Message).HasColumnName("message").HasMaxLength(2000);
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(50);
            e.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(50);
            e.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
            e.Property(x => x.ClientIP).HasColumnName("client_ip").HasMaxLength(45);
            e.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
            e.Property(x => x.RequestPath).HasColumnName("request_path").HasMaxLength(500);
            e.Property(x => x.RequestMethod).HasColumnName("request_method").HasMaxLength(10);
            e.Property(x => x.StatusCode).HasColumnName("status_code");
            e.Property(x => x.DurationMs).HasColumnName("duration_ms");
            e.Property(x => x.Exception).HasColumnName("exception").HasMaxLength(4000);
            e.Property(x => x.Properties).HasColumnName("properties").HasMaxLength(4000);
            e.Property(x => x.Timestamp).HasColumnName("timestamp");
            
            // Indexes for performance
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.Level);
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CorrelationId);
        });


        // SuperAdmin configuration
        modelBuilder.Entity<SuperAdmin>(e =>
        {
            e.ToTable("super_admins");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");

            // Unique constraints
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();

            // Basic info
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
            e.Property(x => x.PasswordSalt).HasColumnName("password_salt").HasMaxLength(500).IsRequired();

            // Personal info
            e.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);

            // Permissions & status
            e.Property(x => x.Permissions).HasColumnName("permissions").HasColumnType("text").IsRequired();
            e.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
            e.Property(x => x.IsEmailVerified).HasColumnName("is_email_verified").IsRequired();

            // Audit fields
            e.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            e.Property(x => x.LastLoginIp).HasColumnName("last_login_ip").HasMaxLength(50);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

            // Indexes for performance
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.LastLoginAt);
        });
    }

}
