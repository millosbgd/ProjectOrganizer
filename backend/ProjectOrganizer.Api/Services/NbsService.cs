using System.Text;
using System.Xml.Linq;

namespace ProjectOrganizer.Api.Services;

public class NbsService
{
    private readonly HttpClient _httpClient;
    private const string NbsServiceUrl = "https://webservices.nbs.rs/CommunicationOfficeService1_0/CompanyAccountService.asmx";

    public NbsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CompanyInfo?> GetCompanyInformationAsync(string pib)
    {
        if (string.IsNullOrWhiteSpace(pib))
            throw new ArgumentException("PIB je obavezan.", nameof(pib));

        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:com=""http://communicationoffice.nbs.rs"">
    <soap:Body>
        <com:GetCompanyInformation>
            <com:CompanyRegistryIdentifier>{pib}</com:CompanyRegistryIdentifier>
        </com:GetCompanyInformation>
    </soap:Body>
</soap:Envelope>";

        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "\"http://communicationoffice.nbs.rs/GetCompanyInformation\"");

        try
        {
            var response = await _httpClient.PostAsync(NbsServiceUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return ParseCompanyInfoResponse(responseContent);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Greška pri pozivu NBS servisa: {ex.Message}", ex);
        }
    }

    private CompanyInfo? ParseCompanyInfoResponse(string xmlResponse)
    {
        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://communicationoffice.nbs.rs";

            var result = doc.Descendants(ns + "GetCompanyInformationResult").FirstOrDefault();
            if (result == null)
                return null;

            var naziv = result.Element(ns + "CompanyName")?.Value;
            var pib = result.Element(ns + "CompanyRegistryIdentifier")?.Value;
            var maticniBroj = result.Element(ns + "CompanyRegistrationNumber")?.Value;
            var adresa = result.Element(ns + "CompanyAddress")?.Value;
            var grad = result.Element(ns + "CompanyCity")?.Value;
            var isActive = result.Element(ns + "IsActive")?.Value;

            if (string.IsNullOrWhiteSpace(naziv))
                return null;

            return new CompanyInfo
            {
                Naziv = naziv,
                Pib = pib ?? string.Empty,
                MaticniBroj = maticniBroj ?? string.Empty,
                Adresa = adresa ?? string.Empty,
                Grad = grad ?? string.Empty,
                IsActive = bool.TryParse(isActive, out var active) && active
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Greška pri parsiranju NBS odgovora: {ex.Message}", ex);
        }
    }
}

public class CompanyInfo
{
    public string Naziv { get; set; } = string.Empty;
    public string Pib { get; set; } = string.Empty;
    public string MaticniBroj { get; set; } = string.Empty;
    public string Adresa { get; set; } = string.Empty;
    public string Grad { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
