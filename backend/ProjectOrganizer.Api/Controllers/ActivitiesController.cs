using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;
using ProjectOrganizer.Api.Services;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivitiesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivitiesController> _logger;
    private readonly UserService _userService;

    public ActivitiesController(
        ApplicationDbContext context,
        ILogger<ActivitiesController> logger,
        UserService userService)
    {
        _context = context;
        _logger = logger;
        _userService = userService;
    }

    /// <summary>
    /// Get activities for calendar view filtered by date range
    /// </summary>
    /// <param name="from">Start date in ISO format (UTC)</param>
    /// <param name="to">End date in ISO format (UTC)</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalendarActivityDto>>> GetActivities(
        [FromQuery] string from,
        [FromQuery] string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
        {
            return BadRequest("Parameters 'from' and 'to' are required.");
        }

        if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
        {
            return BadRequest("Invalid date format. Use ISO format (UTC).");
        }

        // Get current user
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        // Query activities that overlap with the requested range and belong to the current user
        // (StartUtc < to && EndUtc > from && CreatedBy == currentUserId)
        var activities = await _context.Aktivnosti
            .Include(a => a.Projekat)
            .Where(a => a.StartUtc.HasValue && a.EndUtc.HasValue 
                && a.StartUtc < toDate && a.EndUtc > fromDate
                && a.CreatedBy == currentUser.Id)
            .OrderBy(a => a.StartUtc)
            .Select(a => new CalendarActivityDto
            {
                Id = a.Id,
                Title = a.Opis,
                Start = a.StartUtc!.Value,
                End = a.EndUtc!.Value,
                Type = a.Vrsta,
                ProjectName = a.Projekat != null ? a.Projekat.Naziv : "BAU",
                ProjectId = a.ProjekatId,
                Bau = a.Bau,
                BauTipAktivnosti = a.BauTipAktivnosti
            })
            .ToListAsync();

        return Ok(activities);
    }

    /// <summary>
    /// Update activity start and end time (for drag & drop and resize)
    /// </summary>
    /// <param name="id">Activity ID</param>
    /// <param name="dto">Update DTO with new times</param>
    [HttpPatch("{id}/time")]
    public async Task<IActionResult> UpdateActivityTime(int id, [FromBody] UpdateActivityTimeDto dto)
    {
        // Get current user
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        var activity = await _context.Aktivnosti.FindAsync(id);
        
        if (activity == null)
        {
            return NotFound(new { message = "Aktivnost nije pronađena." });
        }

        // Check if user owns this activity
        if (activity.CreatedBy != currentUser.Id)
        {
            return Forbid();
        }

        // Validate that end is after start
        if (dto.EndUtc <= dto.StartUtc)
        {
            return BadRequest(new { message = "Vreme završetka mora biti posle vremena početka." });
        }

        // Update times and date field
        activity.StartUtc = dto.StartUtc;
        activity.EndUtc = dto.EndUtc;
        activity.Datum = dto.StartUtc; // Update Datum field based on activity start time
        activity.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Aktivnosti.AnyAsync(e => e.Id == id))
            {
                return NotFound(new { message = "Aktivnost nije pronađena." });
            }
            throw;
        }
    }

    /// <summary>
    /// Schedule BAU activities for a selected day into the 08:00-16:00 work window.
    /// Non-BAU activities are treated as fixed intervals and are never moved.
    /// </summary>
    [HttpPost("schedule-bau-day")]
    public async Task<ActionResult<ScheduleBauDayResultDto>> ScheduleBauDay([FromBody] ScheduleBauDayRequestDto dto)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);
        var timeZone = ResolveBelgradeTimeZone();
        var localDay = dto.Datum.Date;

        var workStartLocal = localDay.AddHours(8);
        var workEndLocal = localDay.AddHours(16);
        var localNextDay = localDay.AddDays(1);
        var workStartUtc = ConvertLocalToUtc(workStartLocal, timeZone);
        var workEndUtc = ConvertLocalToUtc(workEndLocal, timeZone);

        var fixedActivities = await _context.Aktivnosti
            .Where(a => a.CreatedBy == currentUser.Id
                && !a.Bau
                && a.StartUtc.HasValue
                && a.EndUtc.HasValue
                && a.StartUtc < workEndUtc
                && a.EndUtc > workStartUtc)
            .OrderBy(a => a.StartUtc)
            .ToListAsync();

        var busyIntervals = fixedActivities
            .Select(a => (
                Start: a.StartUtc!.Value < workStartUtc ? workStartUtc : a.StartUtc.Value,
                End: a.EndUtc!.Value > workEndUtc ? workEndUtc : a.EndUtc.Value
            ))
            .Where(i => i.End > i.Start)
            .OrderBy(i => i.Start)
            .ToList();

        var freeSlots = BuildFreeSlots(workStartUtc, workEndUtc, busyIntervals);

        var bauActivities = await _context.Aktivnosti
            .Where(a => a.CreatedBy == currentUser.Id
                && a.Bau
                && a.Datum >= localDay
                && a.Datum < localNextDay)
            .ToListAsync();

        if (bauActivities.Count == 0)
            return BadRequest(new { message = "Nema BAU aktivnosti za izabrani dan." });

        var invalidDuration = bauActivities.FirstOrDefault(a => !a.BauTrajanjeMinuta.HasValue || a.BauTrajanjeMinuta <= 0);
        if (invalidDuration != null)
            return BadRequest(new { message = $"BAU aktivnost #{invalidDuration.Id} nema validno trajanje." });

        var totalBauMinutes = bauActivities.Sum(a => a.BauTrajanjeMinuta!.Value);
        var totalFreeMinutes = freeSlots.Sum(s => (int)(s.End - s.Start).TotalMinutes);

        if (totalFreeMinutes < bauActivities.Count)
        {
            return BadRequest(new
            {
                message = $"Nema dovoljno slobodnog vremena za sve BAU aktivnosti. Slobodno: {totalFreeMinutes} min, aktivnosti: {bauActivities.Count}."
            });
        }

        var scheduled = new List<ScheduleBauActivityDto>();
        var schedulePlan = BuildBauSchedulePlan(bauActivities, freeSlots);

        foreach (var item in schedulePlan)
        {
            var activity = item.Activity;
            var start = item.StartUtc;
            var end = item.EndUtc;

            activity.StartUtc = start;
            activity.EndUtc = end;
            activity.UpdatedAt = DateTime.UtcNow;

            scheduled.Add(new ScheduleBauActivityDto
            {
                Id = activity.Id,
                StartUtc = start,
                EndUtc = end,
                RequestedDurationMinutes = item.RequestedMinutes,
                DurationMinutes = item.ScheduledMinutes
            });
        }

        await _context.SaveChangesAsync();

        var scheduledBauMinutes = scheduled.Sum(a => a.DurationMinutes);
        return Ok(new ScheduleBauDayResultDto
        {
            ScheduledCount = scheduled.Count,
            TotalBauMinutes = totalBauMinutes,
            ScheduledBauMinutes = scheduledBauMinutes,
            WasScaled = totalBauMinutes != scheduledBauMinutes,
            FixedActivityCount = fixedActivities.Count,
            Activities = scheduled.OrderBy(a => a.StartUtc).ToList()
        });
    }

    /// <summary>
    /// Export a print-ready A4 Excel daily report for the selected local day.
    /// </summary>
    [HttpGet("daily-report")]
    public async Task<IActionResult> ExportDailyReport([FromQuery] DateTime datum)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);
        var timeZone = ResolveBelgradeTimeZone();
        var localDay = datum.Date;
        var localNextDay = localDay.AddDays(1);
        var dayStartUtc = ConvertLocalToUtc(localDay, timeZone);
        var dayEndUtc = ConvertLocalToUtc(localNextDay, timeZone);

        var activities = await _context.Aktivnosti
            .Include(a => a.Klijent)
            .Include(a => a.Projekat)
                .ThenInclude(p => p!.Klijent)
            .Where(a => a.CreatedBy == currentUser.Id
                && (
                    (a.StartUtc.HasValue && a.EndUtc.HasValue && a.StartUtc < dayEndUtc && a.EndUtc > dayStartUtc)
                    || (!a.StartUtc.HasValue && a.Datum >= localDay && a.Datum < localNextDay)
                ))
            .OrderBy(a => a.StartUtc ?? a.Datum)
            .ThenBy(a => a.Id)
            .ToListAsync();

        var bauTypeLabels = await _context.Codebooks
            .Include(c => c.EntityType)
            .Where(c => c.EntityType != null
                && c.EntityType.Name == "BauActivityType"
                && c.IsActive)
            .ToDictionaryAsync(c => c.Code, c => c.Value);

        var reportRows = activities.Select(a => ToDailyReportRow(a, bauTypeLabels, timeZone)).ToList();
        var fileBytes = BuildDailyReportWorkbook(reportRows, localDay, currentUser.Name ?? currentUser.Email ?? "Korisnik");
        var fileName = $"Daily_Report_{localDay:yyyy-MM-dd}.xlsx";

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private static DailyReportRowDto ToDailyReportRow(
        Aktivnost activity,
        Dictionary<string, string> bauTypeLabels,
        TimeZoneInfo timeZone)
    {
        var startLocal = activity.StartUtc.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(activity.StartUtc.Value, DateTimeKind.Utc), timeZone)
            : (DateTime?)null;
        var endLocal = activity.EndUtc.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(activity.EndUtc.Value, DateTimeKind.Utc), timeZone)
            : (DateTime?)null;

        var durationMinutes = activity.StartUtc.HasValue && activity.EndUtc.HasValue
            ? Math.Max(0, (int)Math.Round((activity.EndUtc.Value - activity.StartUtc.Value).TotalMinutes))
            : activity.BauTrajanjeMinuta ?? 0;

        var bauTypeCode = activity.BauTipAktivnosti ?? string.Empty;
        var typeLabel = activity.Bau
            ? bauTypeLabels.GetValueOrDefault(bauTypeCode, bauTypeCode)
            : activity.Vrsta;
        var clientOrProject = activity.Bau
            ? activity.Klijent?.Naziv ?? "BAU"
            : activity.Projekat?.Naziv ?? "Projektna aktivnost";

        if (!activity.Bau && activity.Projekat?.Klijent != null)
            clientOrProject = $"{activity.Projekat.Klijent.Naziv} / {clientOrProject}";

        return new DailyReportRowDto
        {
            TimeRange = startLocal.HasValue && endLocal.HasValue ? $"{startLocal:HH:mm} - {endLocal:HH:mm}" : "",
            ClientOrProject = clientOrProject,
            ActivityType = typeLabel,
            Description = activity.Opis,
            Details = activity.Detalji,
            DurationMinutes = durationMinutes,
            IsBau = activity.Bau
        };
    }

    private static byte[] BuildDailyReportWorkbook(List<DailyReportRowDto> rows, DateTime localDay, string userName)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddZipEntry(archive, "[Content_Types].xml", BuildContentTypesXml());
            AddZipEntry(archive, "_rels/.rels", BuildRootRelsXml());
            AddZipEntry(archive, "xl/workbook.xml", BuildWorkbookXml());
            AddZipEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelsXml());
            AddZipEntry(archive, "xl/styles.xml", BuildStylesXml());
            AddZipEntry(archive, "xl/worksheets/sheet1.xml", BuildDailyReportSheetXml(rows, localDay, userName));
        }

        return stream.ToArray();
    }

    private static string BuildDailyReportSheetXml(List<DailyReportRowDto> rows, DateTime localDay, string userName)
    {
        var totalMinutes = rows.Sum(r => r.DurationMinutes);
        var bauMinutes = rows.Where(r => r.IsBau).Sum(r => r.DurationMinutes);
        var projectMinutes = rows.Where(r => !r.IsBau).Sum(r => r.DurationMinutes);

        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">""");
        sb.Append("""<sheetPr><pageSetUpPr fitToPage="1"/></sheetPr>""");
        sb.Append("<dimension ref=\"A1:F").Append(Math.Max(16, rows.Count + 11)).Append("\"/>");
        sb.Append("""<sheetViews><sheetView workbookViewId="0"/></sheetViews>""");
        sb.Append("""<sheetFormatPr defaultRowHeight="18"/>""");
        sb.Append("""<cols><col min="1" max="1" width="14" customWidth="1"/><col min="2" max="2" width="28" customWidth="1"/><col min="3" max="3" width="20" customWidth="1"/><col min="4" max="4" width="30" customWidth="1"/><col min="5" max="5" width="42" customWidth="1"/><col min="6" max="6" width="11" customWidth="1"/></cols>""");
        sb.Append("<sheetData>");

        AppendRow(sb, 1, new[] { Cell("A1", "DAILY REPORT", 1) }, height: 26);
        AppendRow(sb, 2, new[] { Cell("A2", $"Datum: {localDay:dd.MM.yyyy}", 2), Cell("D2", $"Korisnik: {userName}", 2) });
        AppendRow(sb, 4, new[] { Cell("A4", "Ukupno aktivnosti", 8), Cell("B4", rows.Count.ToString(), 9), Cell("D4", "Ukupno vreme", 8), Cell("E4", FormatMinutes(totalMinutes), 9) });
        AppendRow(sb, 5, new[] { Cell("A5", "BAU vreme", 8), Cell("B5", FormatMinutes(bauMinutes), 9), Cell("D5", "Projektno vreme", 8), Cell("E5", FormatMinutes(projectMinutes), 9) });
        AppendRow(sb, 7, new[]
        {
            Cell("A7", "Vreme", 4),
            Cell("B7", "Klijent / Projekat", 4),
            Cell("C7", "Tip aktivnosti", 4),
            Cell("D7", "Aktivnost", 4),
            Cell("E7", "Detalji", 4),
            Cell("F7", "Trajanje", 4)
        });

        var rowIndex = 8;
        foreach (var row in rows)
        {
            AppendRow(sb, rowIndex, new[]
            {
                Cell($"A{rowIndex}", row.TimeRange, 6),
                Cell($"B{rowIndex}", row.ClientOrProject, 5),
                Cell($"C{rowIndex}", row.ActivityType, 5),
                Cell($"D{rowIndex}", row.Description, 5),
                Cell($"E{rowIndex}", row.Details, 5),
                Cell($"F{rowIndex}", FormatMinutes(row.DurationMinutes), 7)
            }, height: 42);
            rowIndex++;
        }

        if (rows.Count == 0)
        {
            AppendRow(sb, rowIndex, new[] { Cell($"A{rowIndex}", "Nema aktivnosti za izabrani dan.", 5) }, height: 28);
            rowIndex++;
        }

        rowIndex += 2;
        AppendRow(sb, rowIndex, new[] { Cell($"A{rowIndex}", "Napomena", 3) });
        rowIndex++;
        AppendRow(sb, rowIndex, new[] { Cell($"A{rowIndex}", "Izveštaj je generisan iz ProjectOrganizer dnevnog kalendara.", 10) });

        sb.Append("</sheetData>");
        sb.Append("<mergeCells count=\"4\"><mergeCell ref=\"A1:F1\"/><mergeCell ref=\"A2:C2\"/><mergeCell ref=\"D2:F2\"/><mergeCell ref=\"A")
            .Append(rowIndex)
            .Append(":F")
            .Append(rowIndex)
            .Append("\"/></mergeCells>");
        sb.Append("""<printOptions horizontalCentered="1"/>""");
        sb.Append("""<pageMargins left="0.35" right="0.35" top="0.55" bottom="0.55" header="0.2" footer="0.2"/>""");
        sb.Append("""<pageSetup paperSize="9" orientation="portrait" fitToWidth="1" fitToHeight="0"/>""");
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, int rowIndex, IEnumerable<string> cells, double? height = null)
    {
        sb.Append("<row r=\"").Append(rowIndex).Append('"');
        if (height.HasValue)
            sb.Append(" ht=\"").Append(height.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)).Append("\" customHeight=\"1\"");
        sb.Append('>');
        foreach (var cell in cells)
            sb.Append(cell);
        sb.Append("</row>");
    }

    private static string Cell(string reference, string value, int styleIndex)
        => $"""<c r="{reference}" s="{styleIndex}" t="inlineStr"><is><t>{EscapeXml(value)}</t></is></c>""";

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var clean = new string(value.Where(ch =>
            ch == 0x9
            || ch == 0xA
            || ch == 0xD
            || (ch >= 0x20 && ch <= 0xD7FF)
            || (ch >= 0xE000 && ch <= 0xFFFD)).ToArray());

        return System.Security.SecurityElement.Escape(clean) ?? string.Empty;
    }

    private static string FormatMinutes(int minutes)
    {
        var hours = minutes / 60;
        var remaining = minutes % 60;
        return hours > 0 ? $"{hours}h {remaining:00}m" : $"{remaining}m";
    }

    private static void AddZipEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string BuildContentTypesXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>""";

    private static string BuildRootRelsXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""";

    private static string BuildWorkbookXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Daily Report" sheetId="1" r:id="rId1"/></sheets></workbook>""";

    private static string BuildWorkbookRelsXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""";

    private static string BuildStylesXml() =>
        """
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="5">
            <font><sz val="11"/><color rgb="FF1F2937"/><name val="Calibri"/></font>
            <font><b/><sz val="18"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
            <font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
            <font><b/><sz val="11"/><color rgb="FF1F2937"/><name val="Calibri"/></font>
            <font><i/><sz val="10"/><color rgb="FF64748B"/><name val="Calibri"/></font>
          </fonts>
          <fills count="6">
            <fill><patternFill patternType="none"/></fill>
            <fill><patternFill patternType="gray125"/></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FF1F4E79"/><bgColor indexed="64"/></patternFill></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FF2563EB"/><bgColor indexed="64"/></patternFill></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FFEFF6FF"/><bgColor indexed="64"/></patternFill></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FFF8FAFC"/><bgColor indexed="64"/></patternFill></fill>
          </fills>
          <borders count="2">
            <border><left/><right/><top/><bottom/><diagonal/></border>
            <border><left style="thin"><color rgb="FFE2E8F0"/></left><right style="thin"><color rgb="FFE2E8F0"/></right><top style="thin"><color rgb="FFE2E8F0"/></top><bottom style="thin"><color rgb="FFE2E8F0"/></bottom><diagonal/></border>
          </borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="11">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="0" fontId="3" fillId="0" borderId="0" xfId="0" applyFont="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="2" fillId="3" borderId="0" xfId="0" applyFont="1" applyFill="1"><alignment horizontal="left" vertical="center"/></xf>
            <xf numFmtId="0" fontId="2" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1"><alignment vertical="top" wrapText="1"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1"><alignment horizontal="center" vertical="top" wrapText="1"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1"><alignment horizontal="right" vertical="top"/></xf>
            <xf numFmtId="0" fontId="3" fillId="4" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="3" fillId="5" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1"><alignment horizontal="right" vertical="center"/></xf>
            <xf numFmtId="0" fontId="4" fillId="0" borderId="0" xfId="0" applyFont="1"><alignment vertical="top" wrapText="1"/></xf>
          </cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """.TrimStart();

    public static List<ScheduleBauPlanItem> BuildBauSchedulePlan(
        List<Aktivnost> activities,
        List<(DateTime Start, DateTime End)> freeSlots)
    {
        var orderedActivities = activities
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.Id)
            .ToList();

        var usableSlots = freeSlots
            .Select(s => (s.Start, s.End, Minutes: (int)(s.End - s.Start).TotalMinutes))
            .Where(s => s.Minutes > 0)
            .OrderBy(s => s.Start)
            .ToList();

        if (orderedActivities.Count < usableSlots.Count)
        {
            usableSlots = usableSlots
                .OrderByDescending(s => s.Minutes)
                .Take(orderedActivities.Count)
                .OrderBy(s => s.Start)
                .ToList();
        }

        var plan = new List<ScheduleBauPlanItem>();
        var activityIndex = 0;
        var remainingSlotMinutes = usableSlots.Sum(s => s.Minutes);
        var remainingRequestedMinutes = orderedActivities.Sum(a => a.BauTrajanjeMinuta!.Value);

        for (var slotIndex = 0; slotIndex < usableSlots.Count && activityIndex < orderedActivities.Count; slotIndex++)
        {
            var slot = usableSlots[slotIndex];
            var activitiesRemaining = orderedActivities.Count - activityIndex;
            var laterSlotCapacity = usableSlots
                .Skip(slotIndex + 1)
                .Sum(s => s.Minutes);
            var minForCurrentSlot = Math.Max(1, activitiesRemaining - laterSlotCapacity);
            var maxForCurrentSlot = Math.Min(activitiesRemaining, slot.Minutes);
            var targetRequestedForSlot = remainingSlotMinutes > 0
                ? (decimal)remainingRequestedMinutes * slot.Minutes / remainingSlotMinutes
                : remainingRequestedMinutes;
            var groupRequested = 0;
            var groupCount = 0;

            while (groupCount < maxForCurrentSlot)
            {
                var nextRequested = orderedActivities[activityIndex + groupCount].BauTrajanjeMinuta!.Value;
                if (groupCount >= minForCurrentSlot && groupRequested >= targetRequestedForSlot)
                    break;

                groupRequested += nextRequested;
                groupCount++;
            }

            groupCount = Math.Max(minForCurrentSlot, groupCount);

            var group = orderedActivities
                .Skip(activityIndex)
                .Take(groupCount)
                .Select(a => (Activity: a, RequestedMinutes: a.BauTrajanjeMinuta!.Value))
                .ToList();

            var scaledDurations = ScaleDurationsToTargetTime(group, slot.Minutes);
            var cursor = slot.Start;

            foreach (var duration in scaledDurations)
            {
                var end = cursor.AddMinutes(duration.ScheduledMinutes);
                plan.Add(new ScheduleBauPlanItem(
                    duration.Activity,
                    cursor,
                    end,
                    duration.RequestedMinutes,
                    duration.ScheduledMinutes));
                cursor = end;
            }

            activityIndex += groupCount;
            remainingSlotMinutes -= slot.Minutes;
            remainingRequestedMinutes -= group.Sum(a => a.RequestedMinutes);
        }

        return plan;
    }

    private static List<ScaledBauDuration> ScaleDurationsToTargetTime(
        List<(Aktivnost Activity, int RequestedMinutes)> activities,
        int targetMinutes)
    {
        var requestedTotal = activities.Sum(a => a.RequestedMinutes);
        if (requestedTotal <= 0 || targetMinutes <= 0)
            return new List<ScaledBauDuration>();

        var scaled = activities
            .Select(a =>
            {
                var exact = (decimal)a.RequestedMinutes * targetMinutes / requestedTotal;
                var scheduled = Math.Max(1, (int)Math.Floor(exact));
                return new
                {
                    a.Activity,
                    a.RequestedMinutes,
                    ScheduledMinutes = scheduled,
                    Remainder = exact - scheduled
                };
            })
            .ToList();

        var remainingMinutes = targetMinutes - scaled.Sum(a => a.ScheduledMinutes);
        var orderedForRemainder = scaled
            .OrderByDescending(a => a.Remainder)
            .ThenByDescending(a => a.RequestedMinutes)
            .ThenBy(a => a.Activity.CreatedAt)
            .ThenBy(a => a.Activity.Id)
            .ToList();

        var extraByActivityId = orderedForRemainder.ToDictionary(a => a.Activity.Id, _ => 0);
        for (var i = 0; i < remainingMinutes; i++)
        {
            var item = orderedForRemainder[i % orderedForRemainder.Count];
            extraByActivityId[item.Activity.Id]++;
        }

        return scaled
            .Select(a => new ScaledBauDuration(
                a.Activity,
                a.RequestedMinutes,
                a.ScheduledMinutes + extraByActivityId[a.Activity.Id]))
            .ToList();
    }

    public static List<(DateTime Start, DateTime End)> BuildFreeSlots(
        DateTime workStartUtc,
        DateTime workEndUtc,
        List<(DateTime Start, DateTime End)> busyIntervals)
    {
        var freeSlots = new List<(DateTime Start, DateTime End)>();
        var cursor = workStartUtc;

        foreach (var interval in MergeIntervals(busyIntervals))
        {
            if (interval.Start > cursor)
                freeSlots.Add((cursor, interval.Start));

            if (interval.End > cursor)
                cursor = interval.End;
        }

        if (cursor < workEndUtc)
            freeSlots.Add((cursor, workEndUtc));

        return freeSlots;
    }

    private static List<(DateTime Start, DateTime End)> MergeIntervals(List<(DateTime Start, DateTime End)> intervals)
    {
        var merged = new List<(DateTime Start, DateTime End)>();
        foreach (var interval in intervals.OrderBy(i => i.Start))
        {
            if (merged.Count == 0 || interval.Start > merged[^1].End)
            {
                merged.Add(interval);
                continue;
            }

            if (interval.End > merged[^1].End)
                merged[^1] = (merged[^1].Start, interval.End);
        }

        return merged;
    }

    public static DateTime ConvertLocalToUtc(DateTime localDateTime, TimeZoneInfo timeZone)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone);
    }

    public static TimeZoneInfo ResolveBelgradeTimeZone()
    {
        foreach (var id in new[] { "Europe/Belgrade", "Central European Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}

/// <summary>
/// DTO for calendar activities
/// </summary>
public class CalendarActivityDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Type { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public bool Bau { get; set; }
    public string? BauTipAktivnosti { get; set; }
}

/// <summary>
/// DTO for updating activity time
/// </summary>
public class UpdateActivityTimeDto
{
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
}

public class ScheduleBauDayRequestDto
{
    public DateTime Datum { get; set; }
}

public class ScheduleBauDayResultDto
{
    public int ScheduledCount { get; set; }
    public int TotalBauMinutes { get; set; }
    public int ScheduledBauMinutes { get; set; }
    public bool WasScaled { get; set; }
    public int FixedActivityCount { get; set; }
    public List<ScheduleBauActivityDto> Activities { get; set; } = new();
}

public class ScheduleBauActivityDto
{
    public int Id { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public int RequestedDurationMinutes { get; set; }
    public int DurationMinutes { get; set; }
}

public record ScaledBauDuration(Aktivnost Activity, int RequestedMinutes, int ScheduledMinutes);
public record ScheduleBauPlanItem(Aktivnost Activity, DateTime StartUtc, DateTime EndUtc, int RequestedMinutes, int ScheduledMinutes);

public class DailyReportRowDto
{
    public string TimeRange { get; set; } = string.Empty;
    public string ClientOrProject { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool IsBau { get; set; }
}
