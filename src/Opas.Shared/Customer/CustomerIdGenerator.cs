namespace Opas.Shared.Customer;

/// <summary>
/// Global Patient ID ve Tenant-specific Customer ID generator
/// </summary>
public static class CustomerIdGenerator
{
    /// <summary>
    /// Global Patient ID oluşturur (cross-tenant tracking için)
    /// Format:
    /// - TC varsa: HAS-{TC_NO} (örn: HAS-12345678901)
    /// - Yabancı: HAS-F-{PASSPORT} (örn: HAS-F-AB1234567)
    /// - Bebek: HAS-P-{PARENT_TC}-{YYYYMMDD} (örn: HAS-P-98765432109-20251009)
    /// </summary>
    public static string GenerateGlobalPatientId(
        string? tcNo,
        string? passportNo,
        string? motherTc,
        string? fatherTc,
        DateTime? birthDate)
    {
        // TC varsa
        if (!string.IsNullOrWhiteSpace(tcNo))
        {
            return $"HAS-{tcNo}";
        }

        // Yabancı (pasaport varsa)
        if (!string.IsNullOrWhiteSpace(passportNo))
        {
            return $"HAS-F-{passportNo}";
        }

        // Bebek (anne veya baba TC'si + doğum tarihi)
        var parentTc = !string.IsNullOrWhiteSpace(motherTc) ? motherTc : fatherTc;
        if (!string.IsNullOrWhiteSpace(parentTc) && birthDate.HasValue)
        {
            var birthDateStr = birthDate.Value.ToString("yyyyMMdd");
            return $"HAS-P-{parentTc}-{birthDateStr}";
        }

        throw new InvalidOperationException(
            "Global Patient ID oluşturulamadı. TC, pasaport veya ebeveyn bilgisi gerekli.");
    }

    /// <summary>
    /// Tenant-specific Customer ID oluşturur (human-readable)
    /// Format: HAS-{initials}-{GLN7}-{sequence}
    /// Örnek: HAS-AY-1530144-0000024
    /// </summary>
    /// <param name="firstName">İsim</param>
    /// <param name="lastName">Soyisim</param>
    /// <param name="gln">Tenant GLN (TNT_8680001530144 formatında)</param>
    /// <param name="sequence">Sıra numarası</param>
    public static string GenerateTenantCustomerId(
        string firstName,
        string lastName,
        string gln,
        int sequence)
    {
        // GLN'den son 7 haneyi al (TNT_8680001530144 → 1530144)
        var glnLast7 = gln.Length >= 7 ? gln[^7..] : gln;

        // İsim/soyisim baş harfleri (Türkçe karakter desteği)
        var initials = GenerateInitials(firstName, lastName);

        // Sequence 7 haneye pad (0000001)
        var seqStr = sequence.ToString("D7");

        return $"HAS-{initials}-{glnLast7}-{seqStr}";
    }

    /// <summary>
    /// İsim/soyisim baş harflerini oluşturur
    /// Örnek: "Ahmet Yılmaz" → "AY"
    /// Örnek: "Mehmet Emin Arslan" → "MEA"
    /// Örnek: "Şeyma Nur" → "ŞN"
    /// </summary>
    private static string GenerateInitials(string firstName, string lastName)
    {
        var firstInitials = string.Join("", firstName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w[0].ToString().ToUpperInvariant()));

        var lastInitials = string.Join("", lastName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w[0].ToString().ToUpperInvariant()));

        return firstInitials + lastInitials;
    }

    /// <summary>
    /// Yaş hesaplar (doğum tarihinden)
    /// </summary>
    public static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;

        // Doğum günü henüz geçmediyse 1 yaş eksilt
        if (birthDate.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}

