using System.Security.Cryptography;

namespace Opas.Shared.Common;

/// <summary>
/// Akıllı ID generator - tüm kullanıcı türleri için unique ID'ler
/// Format: PREFIX_XXXXXX (6 haneli random)
/// </summary>
public static class SmartIdGenerator
{
    private static readonly Random _random = new();
    private static readonly object _lock = new();

    /// <summary>
    /// SuperAdmin ID - FIXED
    /// Format: <superadmin_id> (no generation needed)
    /// </summary>
    public const string SUPERADMIN_ID = "";

    /// <summary>
    /// Pharmacist ID generator  
    /// Format: PHM_XXXXXX
    /// </summary>
    public static string GeneratePharmacistId()
    {
        return GenerateId("PHM");
    }

    /// <summary>
    /// SubUser ID generator
    /// Format: SUB_XXXXXX
    /// </summary>
    public static string GenerateSubUserId()
    {
        return GenerateId("SUB");
    }

    /// <summary>
    /// Tenant ID generator
    /// Format: TNT_XXXXXX
    /// </summary>
    public static string GenerateTenantId()
    {
        return GenerateId("TNT");
    }

    /// <summary>
    /// Generic ID generator with prefix
    /// </summary>
    public static string GenerateId(string prefix)
    {
        lock (_lock)
        {
            // 6 haneli random sayı (000001 - 999999)
            var number = _random.Next(1, 1000000);
            return $"{prefix}_{number:D6}";
        }
    }

    /// <summary>
    /// Generate multiple unique IDs for batch operations
    /// </summary>
    public static List<string> GenerateMultipleIds(string prefix, int count)
    {
        var ids = new HashSet<string>();
        
        while (ids.Count < count)
        {
            ids.Add(GenerateId(prefix));
        }
        
        return ids.ToList();
    }

    /// <summary>
    /// Validate ID format
    /// </summary>
    public static bool IsValidId(string id, string expectedPrefix)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        var parts = id.Split('_');
        if (parts.Length != 2)
            return false;

        if (parts[0] != expectedPrefix)
            return false;

        if (parts[1].Length != 6 || !parts[1].All(char.IsDigit))
            return false;

        return true;
    }

    /// <summary>
    /// Extract prefix from ID
    /// </summary>
    public static string? GetPrefix(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var parts = id.Split('_');
        return parts.Length == 2 ? parts[0] : null;
    }

    /// <summary>
    /// Extract number from ID
    /// </summary>
    public static int? GetNumber(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var parts = id.Split('_');
        if (parts.Length != 2 || parts[1].Length != 6)
            return null;

        return int.TryParse(parts[1], out var number) ? number : null;
    }
}
