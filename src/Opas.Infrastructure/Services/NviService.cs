using System.Text;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Opas.Infrastructure.Services;

/// <summary>
/// NVI (Nüfus ve Vatandaşlık İşleri) Kimlik Doğrulama Servisi
/// TC Kimlik No, Ad, Soyad ve Doğum Yılı ile kimlik doğrulama yapar
/// </summary>
public class NviService
{
    private readonly ILogger<NviService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    // NVI Kimlik Doğrulama Servisi WSDL URL
    private const string NVI_SERVICE_URL = "https://tckimlik.nvi.gov.tr/Service/KPSPublic.asmx";

    public NviService(ILogger<NviService> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("nvi");
    }

    /// <summary>
    /// NVI servisi ile kimlik doğrulama yapar
    /// </summary>
    /// <param name="tcNumber">TC Kimlik Numarası (11 haneli)</param>
    /// <param name="firstName">Ad</param>
    /// <param name="lastName">Soyad</param>
    /// <param name="birthYear">Doğum Yılı</param>
    /// <returns>Kimlik eşleşme durumu</returns>
    public async Task<NviValidationResult> ValidateIdentityAsync(
        string tcNumber, 
        string firstName, 
        string lastName, 
        int birthYear)
    {
        try
        {
            // TEMPORARY BYPASS: NVI servisi kapalı olduğu için Development'ta bypass
            if (_configuration.GetValue<bool>("Integrations:Nvi:BypassInDevelopment", false))
            {
                _logger.LogWarning("NVI bypass enabled in Development mode - returning valid result");
                return new NviValidationResult
                {
                    IsValid = true,
                    TcNumber = tcNumber,
                    FirstName = firstName,
                    LastName = lastName,
                    BirthYear = birthYear,
                    ResponseTime = DateTime.UtcNow,
                    ErrorMessage = null
                };
            }

            _logger.LogInformation("NVI kimlik doğrulama başlatıldı: TC={TC}", tcNumber);

            // SOAP envelope oluştur
            var soapEnvelope = CreateSoapEnvelope(tcNumber, firstName, lastName, birthYear);
            
            // HTTP POST request
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://tckimlik.nvi.gov.tr/WS/TCKimlikNoDogrula");

            var response = await _httpClient.PostAsync(NVI_SERVICE_URL, content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("NVI servisi HTTP hatası: {StatusCode}", response.StatusCode);
                return new NviValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = $"NVI servisi erişim hatası: {response.StatusCode}",
                    ResponseTime = DateTime.UtcNow
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var isValid = ParseSoapResponse(responseContent);

            _logger.LogInformation("NVI kimlik doğrulama tamamlandı: TC={TC}, Valid={Valid}", tcNumber, isValid);

            return new NviValidationResult
            {
                IsValid = isValid,
                TcNumber = tcNumber,
                FirstName = firstName,
                LastName = lastName,
                BirthYear = birthYear,
                ResponseTime = DateTime.UtcNow,
                ErrorMessage = isValid ? null : "Kimlik bilgileri eşleşmiyor"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "NVI servisi ağ hatası: TC={TC}", tcNumber);
            return new NviValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "NVI servisi ağ hatası",
                ResponseTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NVI kimlik doğrulama genel hatası: TC={TC}", tcNumber);
            return new NviValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "Kimlik doğrulama sırasında hata oluştu",
                ResponseTime = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// SOAP envelope oluşturur
    /// </summary>
    private string CreateSoapEnvelope(string tcNumber, string firstName, string lastName, int birthYear)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <TCKimlikNoDogrula xmlns=""http://tckimlik.nvi.gov.tr/WS"">
      <TCKimlikNo>{tcNumber}</TCKimlikNo>
      <Ad>{SecurityHelper.XmlEncode(firstName.ToUpperInvariant())}</Ad>
      <Soyad>{SecurityHelper.XmlEncode(lastName.ToUpperInvariant())}</Soyad>
      <DogumYili>{birthYear}</DogumYili>
    </TCKimlikNoDogrula>
  </soap:Body>
</soap:Envelope>";
    }

    /// <summary>
    /// SOAP response'unu parse eder
    /// </summary>
    private bool ParseSoapResponse(string soapResponse)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(soapResponse);

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsmgr.AddNamespace("tckimlik", "http://tckimlik.nvi.gov.tr/WS");

            var resultNode = doc.SelectSingleNode("//tckimlik:TCKimlikNoDogrulaResponse/tckimlik:TCKimlikNoDogrulaResult", nsmgr);
            
            if (resultNode?.InnerText == "true")
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SOAP response parse hatası");
            return false;
        }
    }
}

/// <summary>
/// NVI doğrulama sonuç modeli
/// </summary>
public class NviValidationResult
{
    public bool IsValid { get; set; }
    public string? TcNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? BirthYear { get; set; }
    public DateTime ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Güvenlik yardımcı sınıfı
/// </summary>
public static class SecurityHelper
{
    public static string XmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
