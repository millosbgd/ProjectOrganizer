using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ProjectOrganizer.Api.Data;

/// <summary>
/// Ensures DateTime values are always stored and retrieved as UTC
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcDateTimeConverter()
        : base(
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : (DateTime?)null,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : (DateTime?)null)
    {
    }
}
