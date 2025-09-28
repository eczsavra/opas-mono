namespace Opas.Shared.ControlPlane;

/// <summary>
/// OPAS sistem yöneticisi - tüm tenantlara erişim yetkisi
/// Control Plane DB'de saklanır
/// </summary>
public class SuperAdmin
{
    public int Id { get; set; }

    /// <summary>
    /// Benzersiz kullanıcı adı
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email (ikincil kimlik doğrulama)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Şifreli password hash
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Password salt
    /// </summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// Ad
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Soyad
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Telefon
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Yetkiler (JSON formatında)
    /// ["GLOBAL_ACCESS", "TENANT_MANAGEMENT", "SYSTEM_SETTINGS"]
    /// </summary>
    public string Permissions { get; set; } = "[]";

    /// <summary>
    /// Aktif durumu
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Email doğrulandı mı?
    /// </summary>
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>
    /// Son giriş zamanı
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Son giriş IP adresi
    /// </summary>
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// Hesap oluşturulma zamanı
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Son güncellenme zamanı
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Oluşturan kullanıcı
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Güncelleyen kullanıcı
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Tam ad
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Yetkiler listesi (JSON'dan parse edilmiş)
    /// </summary>
    public List<string> PermissionList => 
        System.Text.Json.JsonSerializer.Deserialize<List<string>>(Permissions) ?? new List<string>();
}
