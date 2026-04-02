using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class Mail
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [MaxLength(500)]
    public string MessageId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string From { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Cc { get; set; }

    [MaxLength(998)]
    public string Subject { get; set; } = string.Empty;

    public string? BodyText { get; set; }

    public string? BodyHtml { get; set; }

    public DateTime ReceivedDateTime { get; set; }

    public DateTime DatumUcitavanja { get; set; } = DateTime.UtcNow;
}
