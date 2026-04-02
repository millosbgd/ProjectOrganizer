using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;
using ProjectOrganizer.Api.Services;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/mail")]
[Authorize]
public class MailController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MailController> _logger;

    public MailController(
        ApplicationDbContext context,
        UserService userService,
        IHttpClientFactory httpClientFactory,
        ILogger<MailController> logger)
    {
        _context = context;
        _userService = userService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // GET: api/mail
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Mail>>> GetAll()
    {
        var user = await _userService.EnsureUserExistsAsync(User);
        var mails = await _context.Mailovi
            .Where(m => m.UserId == user.Id)
            .OrderByDescending(m => m.ReceivedDateTime)
            .ToListAsync();
        return Ok(mails);
    }

    // POST: api/mail/fetch
    [HttpPost("fetch")]
    public async Task<ActionResult<FetchMailsResult>> FetchMails([FromBody] FetchMailsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
            return BadRequest("accessToken je obavezan");

        if (request.Days < 1 || request.Days > 90)
            return BadRequest("days mora biti između 1 i 90");

        var user = await _userService.EnsureUserExistsAsync(User);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", request.AccessToken);

        var fromDate = DateTime.UtcNow.AddDays(-request.Days).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"https://graph.microsoft.com/v1.0/me/messages" +
                  $"?$filter=receivedDateTime ge {fromDate}" +
                  $"&$select=id,from,ccRecipients,subject,body,receivedDateTime" +
                  $"&$top=100" +
                  $"&$orderby=receivedDateTime desc";

        var totalFetched = 0;
        var newCount = 0;

        while (!string.IsNullOrEmpty(url))
        {
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Graph API poziv nije uspeo");
                return StatusCode(502, "Greška pri komunikaciji sa Microsoft Graph API-jem");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Graph API greška {Status}: {Body}", response.StatusCode, errorBody);
                return StatusCode((int)response.StatusCode,
                    "Microsoft Graph API greška. Moguće da je token istekao ili da nemate dozvolu.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var graphResponse = JsonSerializer.Deserialize<GraphMailResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (graphResponse?.Value == null) break;

            totalFetched += graphResponse.Value.Count;

            foreach (var msg in graphResponse.Value)
            {
                var messageId = msg.Id ?? string.Empty;
                var alreadyExists = await _context.Mailovi
                    .AnyAsync(m => m.UserId == user.Id && m.MessageId == messageId);

                if (alreadyExists) continue;

                var from = msg.From?.EmailAddress?.Address ?? string.Empty;
                var ccAddresses = msg.CcRecipients?
                    .Where(r => r.EmailAddress?.Address != null)
                    .Select(r => r.EmailAddress!.Address!)
                    .ToList() ?? new List<string>();

                var mail = new Mail
                {
                    UserId = user.Id,
                    MessageId = messageId,
                    From = from,
                    Cc = ccAddresses.Count > 0 ? string.Join("; ", ccAddresses) : null,
                    Subject = msg.Subject ?? string.Empty,
                    BodyText = msg.Body?.ContentType == "text" ? msg.Body.Content : null,
                    BodyHtml = msg.Body?.ContentType == "html" ? msg.Body.Content : null,
                    ReceivedDateTime = msg.ReceivedDateTime ?? DateTime.UtcNow,
                    DatumUcitavanja = DateTime.UtcNow
                };

                _context.Mailovi.Add(mail);
                newCount++;
            }

            await _context.SaveChangesAsync();
            url = graphResponse.OdataNextLink;
        }

        return Ok(new FetchMailsResult
        {
            NewCount = newCount,
            TotalFetched = totalFetched,
            Message = $"Učitano {newCount} novih mailova"
        });
    }

    // DELETE: api/mail/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userService.EnsureUserExistsAsync(User);
        var mail = await _context.Mailovi
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

        if (mail == null) return NotFound();

        _context.Mailovi.Remove(mail);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

// DTOs
public class FetchMailsRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public int Days { get; set; } = 7;
}

public class FetchMailsResult
{
    public int NewCount { get; set; }
    public int TotalFetched { get; set; }
    public string Message { get; set; } = string.Empty;
}

// Graph API response models
public class GraphMailResponse
{
    public List<GraphMessage>? Value { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
    public string? OdataNextLink { get; set; }
}

public class GraphMessage
{
    public string? Id { get; set; }
    public string? Subject { get; set; }
    public DateTime? ReceivedDateTime { get; set; }
    public GraphEmailWrapper? From { get; set; }
    public List<GraphEmailWrapper>? CcRecipients { get; set; }
    public GraphBody? Body { get; set; }
}

public class GraphEmailWrapper
{
    public GraphEmailAddress? EmailAddress { get; set; }
}

public class GraphEmailAddress
{
    public string? Name { get; set; }
    public string? Address { get; set; }
}

public class GraphBody
{
    public string? ContentType { get; set; }
    public string? Content { get; set; }
}
