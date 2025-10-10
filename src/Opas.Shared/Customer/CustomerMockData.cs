namespace Opas.Shared.Customer;

/// <summary>
/// Mock data generator for customers
/// 450 normal (TC'li), 40 yabancı (pasaport), 10 bebek (anne/baba TC'li)
/// </summary>
public static class CustomerMockData
{
    private static readonly Random _random = new();

    // Türk isimleri (erkek)
    private static readonly string[] TurkishMaleNames = new[]
    {
        "Ahmet", "Mehmet", "Mustafa", "Ali", "Hasan", "Hüseyin", "İbrahim", "Ömer", "Yusuf", "Murat",
        "Emre", "Can", "Cem", "Eren", "Bora", "Deniz", "Kaan", "Ege", "Barış", "Onur",
        "Serkan", "Okan", "Burak", "Kemal", "Fatih", "Selim", "Tolga", "Volkan", "Sinan", "Oğuz"
    };

    // Türk isimleri (kadın)
    private static readonly string[] TurkishFemaleNames = new[]
    {
        "Ayşe", "Fatma", "Zeynep", "Emine", "Elif", "Hatice", "Meryem", "Şeyma", "Nur", "Esra",
        "Selin", "Defne", "Ece", "İrem", "Yağmur", "Nehir", "Asya", "Derin", "Begüm", "Melis",
        "Sevgi", "Gül", "Pınar", "Aylin", "Burcu", "Özge", "Nisa", "Merve", "Betül", "Sinem"
    };

    // Yabancı isimleri
    private static readonly string[] ForeignNames = new[]
    {
        "John", "Michael", "David", "James", "Robert", "William", "Richard", "Thomas", "Anna", "Maria",
        "Emma", "Sophia", "Olivia", "Isabella", "Mia", "Charlotte", "Amelia", "Harper", "Evelyn", "Abigail",
        "Ali", "Mohammed", "Omar", "Fatima", "Aisha", "Hassan", "Hussein", "Mariam", "Zahra", "Yusuf"
    };

    // Türk soyadları
    private static readonly string[] TurkishSurnames = new[]
    {
        "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Yıldız", "Yıldırım", "Öztürk", "Aydın", "Özdemir",
        "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin", "Kara", "Koç", "Kurt", "Özkan", "Şimşek",
        "Polat", "Güneş", "Erdoğan", "Aksoy", "Avcı", "Türk", "Çakır", "Erdem", "Karaca", "Demirci",
        "Aktaş", "Bozkurt", "Akın", "Tekin", "Bulut", "Ateş", "Korkmaz", "Toprak", "Soylu", "Ekinci"
    };

    // Yabancı soyadları
    private static readonly string[] ForeignSurnames = new[]
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
        "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
        "Lee", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker"
    };

    // Türkiye şehirleri
    private static readonly string[] TurkishCities = new[]
    {
        "İstanbul", "Ankara", "İzmir", "Bursa", "Antalya", "Adana", "Konya", "Gaziantep", "Mersin", "Diyarbakır",
        "Kayseri", "Eskişehir", "Şanlıurfa", "Samsun", "Denizli", "Adapazarı", "Malatya", "Kahramanmaraş", "Erzurum", "Van"
    };

    // İlçe örnekleri
    private static readonly string[] Districts = new[]
    {
        "Merkez", "Çankaya", "Keçiören", "Karşıyaka", "Konak", "Kadıköy", "Beşiktaş", "Üsküdar", "Şişli", "Beyoğlu",
        "Nilüfer", "Osmangazi", "Kepez", "Muratpaşa", "Seyhan", "Çukurova", "Meram", "Selçuklu", "Yenişehir", "Pamukkale"
    };

    // Mahalle örnekleri
    private static readonly string[] Neighborhoods = new[]
    {
        "Cumhuriyet", "Bahçelievler", "Yenimahalle", "Güzeltepe", "Zafer", "Hürriyet", "Atatürk", "Fatih", "Yeşilyurt", "Karacaahmet",
        "Mustafakemalpaşa", "İnönü", "Şehit", "Yıldırım", "Çamlık", "Çiçek", "Gül", "Lale", "Yıldız", "Güneş"
    };

    // Sokak örnekleri
    private static readonly string[] Streets = new[]
    {
        "Atatürk Caddesi", "İnönü Caddesi", "Cumhuriyet Caddesi", "Hürriyet Caddesi", "Millet Caddesi",
        "Gazi Caddesi", "Şehit Sokak", "Bahar Sokak", "Çiçek Sokak", "Yıldız Sokak", "Gül Sokak", "Lale Sokak"
    };

    /// <summary>
    /// 600 mock customer oluşturur (450 normal, 40 yabancı, 10 bebek, 100 çocuk)
    /// </summary>
    public static List<CreateCustomerRequest> Generate500Customers(string tenantId)
    {
        var customers = new List<CreateCustomerRequest>();
        var usedTcs = new HashSet<string>();

        // 450 normal TC'li müşteri
        for (int i = 0; i < 450; i++)
        {
            var isMale = _random.Next(2) == 0;
            var tc = GenerateUniqueTc(usedTcs);
            usedTcs.Add(tc);

            customers.Add(new CreateCustomerRequest
            {
                CustomerType = "INDIVIDUAL",
                TcNo = tc,
                FirstName = isMale ? TurkishMaleNames[_random.Next(TurkishMaleNames.Length)] : TurkishFemaleNames[_random.Next(TurkishFemaleNames.Length)],
                LastName = TurkishSurnames[_random.Next(TurkishSurnames.Length)],
                Phone = GeneratePhone(),
                BirthDate = GenerateBirthDate(18, 80),
                Gender = isMale ? "M" : "F",
                City = TurkishCities[_random.Next(TurkishCities.Length)],
                District = Districts[_random.Next(Districts.Length)],
                Neighborhood = Neighborhoods[_random.Next(Neighborhoods.Length)],
                Street = Streets[_random.Next(Streets.Length)],
                BuildingNo = _random.Next(1, 200).ToString(),
                ApartmentNo = _random.Next(1, 50).ToString(),
                EmergencyContactName = TurkishFemaleNames[_random.Next(TurkishFemaleNames.Length)] + " " + TurkishSurnames[_random.Next(TurkishSurnames.Length)],
                EmergencyContactPhone = GeneratePhone(),
                EmergencyContactRelation = new[] { "Eş", "Anne", "Baba", "Kardeş", "Çocuk" }[_random.Next(5)],
                Notes = _random.Next(10) < 3 ? "Kronik rahatsızlığı var" : null,
                KvkkConsent = _random.Next(10) > 2 // %70 KVKK onayı var
            });
        }

        // 40 yabancı müşteri
        for (int i = 0; i < 40; i++)
        {
            var isMale = _random.Next(2) == 0;
            var passport = GeneratePassport();

            customers.Add(new CreateCustomerRequest
            {
                CustomerType = "FOREIGN",
                PassportNo = passport,
                FirstName = ForeignNames[_random.Next(ForeignNames.Length)],
                LastName = ForeignSurnames[_random.Next(ForeignSurnames.Length)],
                Phone = GeneratePhone(),
                BirthDate = GenerateBirthDate(20, 70),
                Gender = isMale ? "M" : "F",
                City = TurkishCities[_random.Next(TurkishCities.Length)],
                District = Districts[_random.Next(Districts.Length)],
                EmergencyContactName = ForeignNames[_random.Next(ForeignNames.Length)] + " " + ForeignSurnames[_random.Next(ForeignSurnames.Length)],
                EmergencyContactPhone = GeneratePhone(),
                EmergencyContactRelation = "Family",
                Notes = "Yabancı uyruklu",
                KvkkConsent = _random.Next(10) > 3 // %60 KVKK onayı var
            });
        }

        // 10 bebek müşteri (anne/baba TC'li)
        for (int i = 0; i < 10; i++)
        {
            var isMale = _random.Next(2) == 0;
            var motherTc = GenerateUniqueTc(usedTcs);
            var fatherTc = GenerateUniqueTc(usedTcs);
            usedTcs.Add(motherTc);
            usedTcs.Add(fatherTc);

            customers.Add(new CreateCustomerRequest
            {
                CustomerType = "INFANT",
                MotherTc = motherTc,
                FatherTc = fatherTc,
                FirstName = isMale ? TurkishMaleNames[_random.Next(TurkishMaleNames.Length)] : TurkishFemaleNames[_random.Next(TurkishFemaleNames.Length)],
                LastName = TurkishSurnames[_random.Next(TurkishSurnames.Length)],
                Phone = GeneratePhone(), // Anne/baba telefonu
                BirthDate = GenerateBirthDate(0, 2), // 0-2 yaş
                Gender = isMale ? "M" : "F",
                City = TurkishCities[_random.Next(TurkishCities.Length)],
                District = Districts[_random.Next(Districts.Length)],
                GuardianName = TurkishFemaleNames[_random.Next(TurkishFemaleNames.Length)] + " " + TurkishSurnames[_random.Next(TurkishSurnames.Length)],
                GuardianPhone = GeneratePhone(),
                Notes = "Bebek - TC henüz alınmadı",
                KvkkConsent = true // Veli adına onay
            });
        }

        // 100 çocuk müşteri (12-18: veli bilgisi yok, 0-12: veli bilgisi var)
        for (int i = 0; i < 100; i++)
        {
            var isMale = _random.Next(2) == 0;
            var tc = GenerateUniqueTc(usedTcs);
            usedTcs.Add(tc);

            // Rastgele 0-18 yaş arası
            var age = _random.Next(0, 19);
            var birthDate = GenerateBirthDate(age, age); // Tam yaş

            // 12 yaş altı için veli bilgisi lazım
            var needsGuardian = age < 12;

            string? guardianTc = null;
            string? guardianName = null;
            string? guardianPhone = null;
            string? guardianRelation = null;

            if (needsGuardian)
            {
                guardianTc = GenerateUniqueTc(usedTcs);
                usedTcs.Add(guardianTc);
                
                // %60 anne, %30 baba, %10 diğer (amca, dayı, komşu vs.)
                var relationType = _random.Next(10);
                if (relationType < 6)
                {
                    guardianRelation = "Anne";
                    guardianName = TurkishFemaleNames[_random.Next(TurkishFemaleNames.Length)] + " " + TurkishSurnames[_random.Next(TurkishSurnames.Length)];
                }
                else if (relationType < 9)
                {
                    guardianRelation = "Baba";
                    guardianName = TurkishMaleNames[_random.Next(TurkishMaleNames.Length)] + " " + TurkishSurnames[_random.Next(TurkishSurnames.Length)];
                }
                else
                {
                    // Diğer: Amca, Dayı, Teyze, Hala, Komşu
                    var otherRelations = new[] { "Amca", "Dayı", "Teyze", "Hala", "Komşu" };
                    guardianRelation = otherRelations[_random.Next(otherRelations.Length)];
                    guardianName = (guardianRelation == "Teyze" || guardianRelation == "Hala" 
                        ? TurkishFemaleNames[_random.Next(TurkishFemaleNames.Length)]
                        : TurkishMaleNames[_random.Next(TurkishMaleNames.Length)]) + " " + TurkishSurnames[_random.Next(TurkishSurnames.Length)];
                }
                
                guardianPhone = GeneratePhone();
            }

            var notes = age < 12 
                ? $"{age} yaşında - Veli: {guardianName} ({guardianRelation})"
                : $"{age} yaşında - Reşit değil ama veli bilgisi gerekmiyor";

            customers.Add(new CreateCustomerRequest
            {
                CustomerType = "INDIVIDUAL", // TC'si var, INDIVIDUAL
                TcNo = tc,
                FirstName = isMale ? TurkishMaleNames[_random.Next(TurkishMaleNames.Length)] : TurkishFemaleNames[_random.Next(TurkishFemaleNames.Length)],
                LastName = TurkishSurnames[_random.Next(TurkishSurnames.Length)],
                Phone = GeneratePhone(),
                BirthDate = birthDate,
                Gender = isMale ? "M" : "F",
                City = TurkishCities[_random.Next(TurkishCities.Length)],
                District = Districts[_random.Next(Districts.Length)],
                Neighborhood = Neighborhoods[_random.Next(Neighborhoods.Length)],
                Street = Streets[_random.Next(Streets.Length)],
                BuildingNo = _random.Next(1, 200).ToString(),
                ApartmentNo = _random.Next(1, 50).ToString(),
                GuardianTc = guardianTc,
                GuardianName = guardianName,
                GuardianPhone = guardianPhone,
                GuardianRelation = guardianRelation,
                EmergencyContactName = TurkishFemaleNames[_random.Next(TurkishFemaleNames.Length)] + " " + TurkishSurnames[_random.Next(TurkishSurnames.Length)],
                EmergencyContactPhone = GeneratePhone(),
                EmergencyContactRelation = new[] { "Anne", "Baba", "Kardeş", "Amca", "Dayı", "Teyze", "Hala" }[_random.Next(7)],
                Notes = notes,
                KvkkConsent = true // Veli adına onay
            });
        }

        return customers;
    }

    /// <summary>
    /// Unique TC numarası üretir (11 haneli, basit validation)
    /// </summary>
    private static string GenerateUniqueTc(HashSet<string> usedTcs)
    {
        string tc;
        do
        {
            // İlk hane 1-9, diğerleri 0-9
            tc = _random.Next(1, 10).ToString();
            for (int i = 0; i < 10; i++)
            {
                tc += _random.Next(0, 10).ToString();
            }
        } while (usedTcs.Contains(tc));

        return tc;
    }

    /// <summary>
    /// Pasaport numarası üretir (örn: AB1234567)
    /// </summary>
    private static string GeneratePassport()
    {
        var prefix = new[] { "AB", "CD", "EF", "GH", "IJ", "KL", "MN", "OP", "QR", "ST", "UV", "WX", "YZ" }[_random.Next(13)];
        var number = _random.Next(1000000, 9999999);
        return $"{prefix}{number}";
    }

    /// <summary>
    /// Telefon numarası üretir (05XX XXX XX XX formatında)
    /// </summary>
    private static string GeneratePhone()
    {
        var operator1 = new[] { "50", "51", "52", "53", "54", "55" }[_random.Next(6)];
        var part1 = _random.Next(100, 1000);
        var part2 = _random.Next(10, 100);
        var part3 = _random.Next(10, 100);
        return $"05{operator1} {part1} {part2} {part3}";
    }

    /// <summary>
    /// Doğum tarihi üretir
    /// </summary>
    private static DateTime GenerateBirthDate(int minAge, int maxAge)
    {
        var today = DateTime.Today;
        var years = _random.Next(minAge, maxAge + 1);
        var months = _random.Next(0, 12);
        var days = _random.Next(1, 29); // Basit - 28 güne kadar
        
        return today.AddYears(-years).AddMonths(-months).AddDays(-days);
    }
}

