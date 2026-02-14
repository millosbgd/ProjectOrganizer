using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;

namespace ProjectOrganizer.Api.Services;

public class NbsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NbsService> _logger;
    private readonly IConfiguration _configuration;
    private const string NbsServiceUrl = "https://webservices.nbs.rs/CommunicationOfficeService1_0/CompanyAccountService.asmx";

    public NbsService(HttpClient httpClient, ILogger<NbsService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<CompanyInfo?> GetCompanyInformationAsync(string pib)
    {
        if (string.IsNullOrWhiteSpace(pib))
            throw new ArgumentException("PIB je obavezan.", nameof(pib));

        // Get credentials from configuration
        var username = _configuration["Nbs:Username"];
        var password = _configuration["Nbs:Password"];
        var licenceId = _configuration["Nbs:LicenceId"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(licenceId))
        {
            throw new InvalidOperationException("NBS kredencijali nisu konfigurisani. Podesiti Nbs:Username, Nbs:Password i Nbs:LicenceId u appsettings.json ili Azure App Service Configuration.");
        }

        var authHeader = $@"
    <soap:Header>
        <com:AuthenticationHeader>
            <com:UserName>{username}</com:UserName>
            <com:Password>{password}</com:Password>
            <com:LicenceID>{licenceId}</com:LicenceID>
        </com:AuthenticationHeader>
    </soap:Header>";

        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:com=""http://communicationoffice.nbs.rs"">{authHeader}
    <soap:Body>
        <com:GetCompanyAccount>
            <com:nationalIdentificationNumber xsi:nil=""true"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""/>
            <com:taxIdentificationNumber>{pib}</com:taxIdentificationNumber>
            <com:bankCode xsi:nil=""true"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""/>
            <com:accountNumber xsi:nil=""true"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""/>
            <com:controlNumber xsi:nil=""true"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""/>
            <com:companyName xsi:nil=""true"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""/>
            <com:city xsi:nil=""true"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""/>
            <com:startItemNumber xsi:nil=""true"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""/>
            <com:endItemNumber xsi:nil=""true"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""/>
        </com:GetCompanyAccount>
    </soap:Body>
</soap:Envelope>";

        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "\"http://communicationoffice.nbs.rs/GetCompanyAccount\"");

        try
        {
            _logger.LogInformation("Pozivanje NBS servisa za PIB: {PIB}", pib);
            
            var response = await _httpClient.PostAsync(NbsServiceUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("NBS odgovor status: {StatusCode}", response.StatusCode);
            
            // Check if response is a SOAP fault
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NBS odgovor body: {Response}", responseContent);
                
                // Try to parse SOAP fault for better error message
                try
                {
                    var faultDoc = XDocument.Parse(responseContent);
                    XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
                    var fault = faultDoc.Descendants(soapNs + "Fault").FirstOrDefault();
                    
                    if (fault != null)
                    {
                        var errorType = fault.Descendants("ErrorType").FirstOrDefault()?.Value;
                        var errorCode = fault.Descendants("ErrorCode").FirstOrDefault()?.Value;
                        var errorMessage = fault.Descendants("ErrorMessage").FirstOrDefault()?.Value;
                        
                        if (errorType == "AuthenticationHeaderError")
                        {
                            throw new InvalidOperationException($"NBS autentifikacija neuspešna: {errorMessage} (Kod: {errorCode}). Proverite NBS kredencijale.");
                        }
                        
                        throw new InvalidOperationException($"NBS greška: {errorMessage} (Tip: {errorType}, Kod: {errorCode})");
                    }
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Nije moguće parsirati SOAP fault odgovor");
                }
                
                response.EnsureSuccessStatusCode();
            }

            var companyInfo = ParseCompanyInfoResponse(responseContent);
            
            return companyInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška pri pozivu NBS servisa za PIB: {PIB}", pib);
            throw new InvalidOperationException($"Greška pri pozivu NBS servisa: {ex.Message}", ex);
        }
    }

    private CompanyInfo? ParseCompanyInfoResponse(string xmlResponse)
    {
        try
        {
            _logger.LogInformation("Parsing NBS response, length: {Length}", xmlResponse?.Length ?? 0);
            _logger.LogInformation("Full XML Response: {Xml}", xmlResponse);
            
            var doc = XDocument.Parse(xmlResponse);
            
            // Try both namespaces - SOAP response and service namespace
            XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace ns = "http://communicationoffice.nbs.rs";

            // Navigate through SOAP envelope
            var body = doc.Descendants(soapNs + "Body").FirstOrDefault();
            if (body == null)
            {
                _logger.LogWarning("SOAP Body not found in response");
                return null;
            }

            var result = body.Descendants(ns + "GetCompanyAccountResponse")
                .FirstOrDefault()?
                .Element(ns + "GetCompanyAccountResult");
                
            if (result == null)
            {
                _logger.LogWarning("GetCompanyAccountResult not found in response");
                _logger.LogDebug("Response XML: {Xml}", xmlResponse);
                return null;
            }

            // NBS returns DataSet XML - need to parse diffgram
            XNamespace diffNs = "urn:schemas-microsoft-com:xml-diffgram-v1";
            var diffgram = result.Descendants(diffNs + "diffgram").FirstOrDefault();
            if (diffgram == null)
            {
                _logger.LogWarning("Diffgram not found in DataSet. Looking for any data...");
                _logger.LogDebug("Result content: {Result}", result.ToString());
                
                // Try alternative parsing - maybe data is directly in result
                var anyData = result.Descendants().FirstOrDefault(e => e.Name.LocalName == "CompanyAccount");
                if (anyData == null)
                {
                    _logger.LogWarning("No CompanyAccount found anywhere in response");
                    return null;
                }
                return ExtractCompanyData(anyData);
            }

            var dataRow = diffgram.Descendants().FirstOrDefault(e => e.Name.LocalName == "CompanyAccount");
            if (dataRow == null)
            {
                _logger.LogWarning("No company data found in diffgram");
                return null;
            }

            return ExtractCompanyData(dataRow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška pri parsiranju NBS odgovora: {Message}", ex.Message);
            throw new InvalidOperationException($"Greška pri parsiranju NBS odgovora: {ex.Message}", ex);
        }
    }

    private CompanyInfo? ExtractCompanyData(XElement dataRow)
    {
        var naziv = dataRow.Element("CompanyName")?.Value;
        var pib = dataRow.Element("TaxIdentificationNumber")?.Value?.Trim();
        var maticniBroj = dataRow.Element("NationalIdentificationNumber")?.Value;
        var adresa = dataRow.Element("Address")?.Value;
        var grad = dataRow.Element("City")?.Value;

        _logger.LogInformation("Parsed company: {Naziv}, PIB: {PIB}, Maticni: {Maticni}", naziv, pib, maticniBroj);

        if (string.IsNullOrWhiteSpace(naziv))
        {
            _logger.LogWarning("Company name is empty");
            return null;
        }

        return new CompanyInfo
        {
            Naziv = naziv,
            Pib = pib ?? string.Empty,
            MaticniBroj = maticniBroj ?? string.Empty,
            Adresa = adresa ?? string.Empty,
            Grad = grad ?? string.Empty,            Zemlja = "RS",            IsActive = true
        };
    }
}

public class CompanyInfo
{
    public string Naziv { get; set; } = string.Empty;
    public string Pib { get; set; } = string.Empty;
    public string MaticniBroj { get; set; } = string.Empty;
    public string Adresa { get; set; } = string.Empty;
    public string Grad { get; set; } = string.Empty;
    public string Zemlja { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
