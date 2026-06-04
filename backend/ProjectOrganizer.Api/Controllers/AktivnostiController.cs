using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;
using ProjectOrganizer.Api.Services;
using System.Security.Claims;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AktivnostiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AktivnostiController> _logger;
    private readonly OpenAIService _openAIService;
    private readonly UserService _userService;
    private readonly EncryptionService _encryptionService;
    private readonly IServiceScopeFactory _scopeFactory;

    public AktivnostiController(
        ApplicationDbContext context, 
        ILogger<AktivnostiController> logger,
        OpenAIService openAIService,
        UserService userService,
        EncryptionService encryptionService,
        IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _logger = logger;
        _openAIService = openAIService;
        _userService = userService;
        _encryptionService = encryptionService;
        _scopeFactory = scopeFactory;
    }

    // GET: api/Aktivnosti
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Aktivnost>>> GetAktivnosti(
        [FromQuery] int? projekatId = null, 
        [FromQuery] bool myActivitiesOnly = false)
    {
        var query = _context.Aktivnosti
            .Include(a => a.Projekat)
            .Include(a => a.CreatedByUser)
            .AsQueryable();

        if (projekatId.HasValue)
            query = query.Where(a => a.ProjekatId == projekatId.Value);

        // Filter by current user if requested (default is to show only user's activities)
        if (myActivitiesOnly)
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            query = query.Where(a => a.CreatedBy == currentUser.Id);
        }

        var aktivnosti = await query
            .OrderByDescending(a => a.Datum)
            .ToListAsync();

        var aktivnostIds = aktivnosti.Select(a => a.Id).ToList();
        if (aktivnostIds.Count > 0)
        {
            var taskCounts = await _context.DevOpsTasksCandidates
                .Where(t => aktivnostIds.Contains(t.AktivnostId))
                .GroupBy(t => t.AktivnostId)
                .Select(g => new { AktivnostId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.AktivnostId, x => x.Total);

            var readyCounts = await _context.DevOpsTaskStatusHistory
                .Where(h => h.DurationMinutes == null
                    && h.Status == "Ready"
                    && aktivnostIds.Contains(h.DevOpsTasksCandidate!.AktivnostId))
                .GroupBy(h => h.DevOpsTasksCandidate!.AktivnostId)
                .Select(g => new { AktivnostId = g.Key, Ready = g.Select(h => h.DevOpsTaskCandidateId).Distinct().Count() })
                .ToDictionaryAsync(x => x.AktivnostId, x => x.Ready);

            foreach (var aktivnost in aktivnosti)
            {
                aktivnost.DevOpsTasksTotalCount = taskCounts.GetValueOrDefault(aktivnost.Id);
                aktivnost.DevOpsTasksReadyCount = readyCounts.GetValueOrDefault(aktivnost.Id);
            }
        }

        return Ok(aktivnosti);
    }

    // GET: api/Aktivnosti/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Aktivnost>> GetAktivnost(int id)
    {
        var aktivnost = await _context.Aktivnosti
            .Include(a => a.Projekat)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (aktivnost == null)
            return NotFound();

        return Ok(aktivnost);
    }

    // POST: api/Aktivnosti
    [HttpPost]
    public async Task<ActionResult<Aktivnost>> CreateAktivnost(Aktivnost aktivnost)
    {
        // BAU activities don't need a project
        if (aktivnost.Bau)
        {
            aktivnost.ProjekatId = null;
        }
        else
        {
            // Check if Projekat exists for non-BAU activities
            if (aktivnost.ProjekatId == null || !await _context.Projekti.AnyAsync(p => p.Id == aktivnost.ProjekatId))
                return BadRequest("Projekat ne postoji.");
        }

        // Get current user and set as creator
        var currentUser = await _userService.EnsureUserExistsAsync(User);
        aktivnost.CreatedBy = currentUser.Id;

        aktivnost.CreatedAt = DateTime.UtcNow;
        aktivnost.UpdatedAt = DateTime.UtcNow;

        _context.Aktivnosti.Add(aktivnost);
        await _context.SaveChangesAsync();

        // AI reminder ekstrakcija (fire-and-forget sa try/catch, ne blokira odgovor)
        _ = TryScheduleAiReminderAsync(aktivnost.Id, aktivnost.ProjekatId, currentUser.Auth0Id, currentUser.Id, aktivnost.Opis, aktivnost.Detalji);

        return CreatedAtAction(nameof(GetAktivnost), new { id = aktivnost.Id }, aktivnost);
    }

    // POST: api/Aktivnosti/bau-batch
    [HttpPost("bau-batch")]
    public async Task<ActionResult<BauBatchCreateResultDto>> CreateBauBatch([FromBody] BauBatchCreateRequest request)
    {
        var validation = await ValidateBauBatchRequest(request);
        if (validation.Error != null)
            return validation.Error;

        var currentUser = await _userService.EnsureUserExistsAsync(User);
        var now = DateTime.UtcNow;
        var datum = request.Datum.Date;

        var aktivnosti = validation.Rows.Select(row =>
        {
            var typeCode = row.BauTipAktivnosti.Trim();
            var typeLabel = validation.TypeLabels.GetValueOrDefault(typeCode, typeCode);

            return new Aktivnost
            {
                Opis = typeLabel,
                Detalji = row.Detalji?.Trim() ?? string.Empty,
                Datum = datum,
                StartUtc = row.StartUtc,
                EndUtc = row.EndUtc,
                Status = "Završeno",
                Vrsta = "BAU",
                Bau = true,
                KlijentId = row.KlijentId,
                BauTipAktivnosti = typeCode,
                BauTrajanjeMinuta = row.TrajanjeMinuta,
                ProjekatId = null,
                ProjectImplementationItemId = null,
                CreatedBy = currentUser.Id,
                CreatedAt = now,
                UpdatedAt = now
            };
        }).ToList();

        _context.Aktivnosti.AddRange(aktivnosti);
        await _context.SaveChangesAsync();

        return Ok(new BauBatchCreateResultDto
        {
            Count = aktivnosti.Count,
            ActivityIds = aktivnosti.Select(a => a.Id).ToList()
        });
    }

    // POST: api/Aktivnosti/bau-batch/preview
    [HttpPost("bau-batch/preview")]
    public async Task<ActionResult<BauBatchPreviewResultDto>> PreviewBauBatch([FromBody] BauBatchCreateRequest request)
    {
        var validation = await ValidateBauBatchRequest(request);
        if (validation.Error != null)
            return validation.Error;

        var currentUser = await _userService.EnsureUserExistsAsync(User);
        var timeZone = ActivitiesController.ResolveBelgradeTimeZone();
        var localDay = request.Datum.Date;
        var workStartLocal = localDay.AddHours(8);
        var workEndLocal = localDay.AddHours(16);
        var workStartUtc = ActivitiesController.ConvertLocalToUtc(workStartLocal, timeZone);
        var workEndUtc = ActivitiesController.ConvertLocalToUtc(workEndLocal, timeZone);

        var fixedActivities = await _context.Aktivnosti
            .Include(a => a.Projekat)
                .ThenInclude(p => p!.Klijent)
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

        var freeSlots = ActivitiesController.BuildFreeSlots(workStartUtc, workEndUtc, busyIntervals);
        var totalFreeMinutes = freeSlots.Sum(s => (int)(s.End - s.Start).TotalMinutes);

        if (totalFreeMinutes < validation.Rows.Count)
        {
            return BadRequest(new
            {
                message = $"Nema dovoljno slobodnog vremena za sve BAU aktivnosti. Slobodno: {totalFreeMinutes} min, aktivnosti: {validation.Rows.Count}."
            });
        }

        var now = DateTime.UtcNow;
        var transientActivities = validation.Rows
            .Select((row, index) => new Aktivnost
            {
                Id = -(index + 1),
                Bau = true,
                KlijentId = row.KlijentId,
                BauTipAktivnosti = row.BauTipAktivnosti.Trim(),
                BauTrajanjeMinuta = row.TrajanjeMinuta,
                CreatedAt = now.AddTicks(index),
                UpdatedAt = now
            })
            .ToList();

        var plan = ActivitiesController.BuildBauSchedulePlan(transientActivities, freeSlots);
        var clientsById = validation.ClientNames;
        var bauItems = plan.Select(item =>
        {
            var rowIndex = Math.Abs(item.Activity.Id) - 1;
            var row = validation.Rows[rowIndex];
            var typeCode = row.BauTipAktivnosti.Trim();

            return new BauBatchPreviewItemDto
            {
                RowIndex = rowIndex,
                KlijentId = row.KlijentId,
                KlijentNaziv = clientsById.GetValueOrDefault(row.KlijentId, "Klijent"),
                BauTipAktivnosti = typeCode,
                BauTipAktivnostiNaziv = validation.TypeLabels.GetValueOrDefault(typeCode, typeCode),
                IsBau = true,
                Detalji = row.Detalji?.Trim() ?? string.Empty,
                RequestedDurationMinutes = row.TrajanjeMinuta,
                ScheduledDurationMinutes = item.ScheduledMinutes,
                StartUtc = item.StartUtc,
                EndUtc = item.EndUtc
            };
        });

        var fixedItems = fixedActivities.Select(activity =>
        {
            var start = activity.StartUtc!.Value < workStartUtc ? workStartUtc : activity.StartUtc.Value;
            var end = activity.EndUtc!.Value > workEndUtc ? workEndUtc : activity.EndUtc.Value;
            var projectName = activity.Projekat != null
                ? activity.Projekat.Klijent != null
                    ? $"{activity.Projekat.Klijent.Naziv} / {activity.Projekat.Naziv}"
                    : activity.Projekat.Naziv
                : "Projektna aktivnost";
            var durationMinutes = Math.Max(1, (int)Math.Round((end - start).TotalMinutes));

            return new BauBatchPreviewItemDto
            {
                RowIndex = null,
                KlijentId = activity.KlijentId ?? 0,
                KlijentNaziv = projectName,
                BauTipAktivnosti = activity.Vrsta,
                BauTipAktivnostiNaziv = activity.Vrsta,
                IsBau = false,
                Detalji = activity.Detalji,
                RequestedDurationMinutes = durationMinutes,
                ScheduledDurationMinutes = durationMinutes,
                StartUtc = start,
                EndUtc = end
            };
        });

        var items = bauItems
            .Concat(fixedItems)
            .OrderBy(i => i.StartUtc)
            .ThenBy(i => i.IsBau)
            .ToList();

        var requestedMinutes = validation.Rows.Sum(r => r.TrajanjeMinuta);
        var scheduledMinutes = items.Where(i => i.IsBau).Sum(i => i.ScheduledDurationMinutes);

        return Ok(new BauBatchPreviewResultDto
        {
            TotalRequestedMinutes = requestedMinutes,
            TotalScheduledMinutes = scheduledMinutes,
            WasScaled = requestedMinutes != scheduledMinutes,
            FixedActivityCount = fixedActivities.Count,
            Items = items
        });
    }

    private async Task<BauBatchValidationResult> ValidateBauBatchRequest(BauBatchCreateRequest request)
    {
        if (request.Rows == null || request.Rows.Count == 0)
            return BauBatchValidationResult.WithError(BadRequest(new { message = "Unesite najmanje jednu BAU aktivnost." }));

        var durationOptions = new HashSet<int> { 15, 30, 45, 60, 90 };
        var rows = request.Rows
            .Where(r => r != null && (r.KlijentId > 0 || !string.IsNullOrWhiteSpace(r.BauTipAktivnosti) || r.TrajanjeMinuta > 0 || !string.IsNullOrWhiteSpace(r.Detalji)))
            .ToList();

        if (rows.Count == 0)
            return BauBatchValidationResult.WithError(BadRequest(new { message = "Unesite najmanje jednu popunjenu BAU aktivnost." }));

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.KlijentId <= 0)
                return BauBatchValidationResult.WithError(BadRequest(new { message = $"Red {i + 1}: klijent je obavezan." }));
            if (string.IsNullOrWhiteSpace(row.BauTipAktivnosti))
                return BauBatchValidationResult.WithError(BadRequest(new { message = $"Red {i + 1}: tip aktivnosti je obavezan." }));
            if (!durationOptions.Contains(row.TrajanjeMinuta))
                return BauBatchValidationResult.WithError(BadRequest(new { message = $"Red {i + 1}: trajanje mora biti 15, 30, 45, 60 ili 90 minuta." }));
            if ((row.StartUtc.HasValue && !row.EndUtc.HasValue) || (!row.StartUtc.HasValue && row.EndUtc.HasValue))
                return BauBatchValidationResult.WithError(BadRequest(new { message = $"Red {i + 1}: vreme početka i završetka moraju biti zadata zajedno." }));
            if (row.StartUtc.HasValue && row.EndUtc.HasValue && row.EndUtc <= row.StartUtc)
                return BauBatchValidationResult.WithError(BadRequest(new { message = $"Red {i + 1}: vreme završetka mora biti posle vremena početka." }));
        }

        var clientIds = rows.Select(r => r.KlijentId).Distinct().ToList();
        var clients = await _context.Klijenti
            .Where(k => clientIds.Contains(k.Id))
            .Select(k => new { k.Id, k.Naziv })
            .ToListAsync();

        var clientNames = clients.ToDictionary(k => k.Id, k => k.Naziv);
        var missingClientId = clientIds.FirstOrDefault(id => !clientNames.ContainsKey(id));
        if (missingClientId > 0)
            return BauBatchValidationResult.WithError(BadRequest(new { message = $"Klijent sa ID {missingClientId} ne postoji." }));

        var allowedBauTypes = await _context.Codebooks
            .Include(c => c.EntityType)
            .Where(c => c.EntityType!.Name == "BauActivityType" && c.IsActive && c.EntityType.IsActive)
            .Select(c => new { c.Code, c.Value })
            .ToListAsync();

        var allowedCodes = allowedBauTypes.Select(t => t.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var typeLabels = allowedBauTypes.ToDictionary(t => t.Code, t => t.Value, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rows.Count; i++)
        {
            if (!allowedCodes.Contains(rows[i].BauTipAktivnosti.Trim()))
                return BauBatchValidationResult.WithError(BadRequest(new { message = $"Red {i + 1}: izabrani tip BAU aktivnosti nije važeći." }));
        }

        return new BauBatchValidationResult(rows, typeLabels, clientNames, null);
    }

    // PUT: api/Aktivnosti/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAktivnost(int id, Aktivnost aktivnost)
    {
        if (id != aktivnost.Id)
            return BadRequest();

        var existingAktivnost = await _context.Aktivnosti.FindAsync(id);
        if (existingAktivnost == null)
            return NotFound();

        existingAktivnost.Opis = aktivnost.Opis;
        existingAktivnost.Detalji = aktivnost.Detalji;
        existingAktivnost.Datum = aktivnost.Datum;
        existingAktivnost.StartUtc = aktivnost.StartUtc;
        existingAktivnost.EndUtc = aktivnost.EndUtc;
        existingAktivnost.Status = aktivnost.Status;
        existingAktivnost.Vrsta = aktivnost.Vrsta;
        existingAktivnost.ProjekatId = aktivnost.ProjekatId;
        existingAktivnost.ProjectImplementationItemId = aktivnost.ProjectImplementationItemId;
        existingAktivnost.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Aktivnosti.AnyAsync(a => a.Id == id))
                return NotFound();
            throw;
        }

        // AI reminder ekstrakcija (fire-and-forget sa try/catch, ne blokira odgovor)
        var updater = await _userService.EnsureUserExistsAsync(User);
        _ = TryScheduleAiReminderAsync(id, existingAktivnost.ProjekatId, updater.Auth0Id, updater.Id, existingAktivnost.Opis, existingAktivnost.Detalji);

        return NoContent();
    }

    // DELETE: api/Aktivnosti/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAktivnost(int id)
    {
        var aktivnost = await _context.Aktivnosti.FindAsync(id);
        if (aktivnost == null)
            return NotFound();

        // Check if activity is linked to an implementation item
        if (aktivnost.ProjectImplementationItemId.HasValue && aktivnost.ProjectImplementationItemId.Value > 0)
        {
            return BadRequest(new { message = "Ne možete obrisati aktivnost koja je vezana za stavku implementacije." });
        }

        // Check if activity has saved DevOps tasks
        var hasTasks = await _context.DevOpsTasksCandidates
            .AnyAsync(t => t.AktivnostId == id);
        
        if (hasTasks)
        {
            return BadRequest(new { message = "Ne možete obrisati aktivnost koja ima sačuvane DevOps taskove." });
        }

        _context.Aktivnosti.Remove(aktivnost);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/Aktivnosti/5/generate-zapisnik
    [HttpPost("{id}/generate-zapisnik")]
    public async Task<ActionResult<string>> GenerateZapisnik(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Get user settings
        var userSettings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (userSettings == null || string.IsNullOrWhiteSpace(userSettings.OpenAiApiKey))
            return BadRequest("Morate prvo konfigurisati OpenAI API ključ u podešavanjima.");

        var aktivnost = await _context.Aktivnosti
            .Include(a => a.Projekat)
                .ThenInclude(p => p.Klijent)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (aktivnost == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(aktivnost.Detalji))
            return BadRequest("Aktivnost nema detalje za generisanje zapisnika.");

        try
        {
            // Decrypt the API key before using it
            var decryptedApiKey = _encryptionService.Decrypt(userSettings.OpenAiApiKey);
            if (string.IsNullOrWhiteSpace(decryptedApiKey))
                return BadRequest("OpenAI API ključ nije validan. Molimo ažurirajte ga u podešavanjima.");

            var zapisnik = await _openAIService.GenerateZapisnikAsync(
                decryptedApiKey,
                userSettings.OpenAiModel,
                aktivnost.Projekat.Klijent.Naziv,
                aktivnost.Projekat.Naziv,
                aktivnost.Datum,
                aktivnost.Vrsta,
                aktivnost.Status,
                aktivnost.Opis,
                aktivnost.Detalji
            );

            return Ok(zapisnik);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating zapisnik for aktivnost {AktivnostId}", id);
            return StatusCode(500, "Greška prilikom generisanja zapisnika.");
        }
    }

    // POST: api/Aktivnosti/{id}/generate-devops-tasks
    [HttpPost("{id}/generate-devops-tasks")]
    public async Task<ActionResult<string>> GenerateDevOpsTasks(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var userSettings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (userSettings == null || string.IsNullOrWhiteSpace(userSettings.OpenAiApiKey))
            return BadRequest("Morate prvo konfigurisati OpenAI API ključ u podešavanjima.");

        var aktivnost = await _context.Aktivnosti
            .Include(a => a.Projekat)
                .ThenInclude(p => p.Klijent)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (aktivnost == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(aktivnost.Detalji))
            return BadRequest("Aktivnost nema detalje za generisanje taskova.");

        try
        {
            // Decrypt the API key before using it
            var decryptedApiKey = _encryptionService.Decrypt(userSettings.OpenAiApiKey);
            if (string.IsNullOrWhiteSpace(decryptedApiKey))
                return BadRequest("OpenAI API ključ nije validan. Molimo ažurirajte ga u podešavanjima.");

            var tasks = await _openAIService.GenerateDevOpsTasks(
                decryptedApiKey,
                userSettings.OpenAiModel,
                aktivnost.Projekat.Klijent.Naziv,
                aktivnost.Projekat.Naziv,
                aktivnost.Opis,
                aktivnost.Detalji
            );

            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating DevOps tasks for aktivnost {AktivnostId}", id);
            return StatusCode(500, "Greška prilikom generisanja taskova.");
        }
    }

    // POST: api/Aktivnosti/{id}/parse-devops-tasks
    [HttpPost("{id}/parse-devops-tasks")]
    public async Task<ActionResult<List<object>>> ParseDevOpsTasksFromText(int id, [FromBody] ParseTasksRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var parsedTasks = ParseDevOpsTasksToObjects(request.TasksText);
            return Ok(parsedTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing DevOps tasks for aktivnost {AktivnostId}", id);
            return StatusCode(500, "Greška prilikom parsiranja taskova.");
        }
    }

    // POST: api/Aktivnosti/{id}/save-selected-tasks
    [HttpPost("{id}/save-selected-tasks")]
    public async Task<ActionResult> SaveSelectedTasks(int id, [FromBody] SaveTasksRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Auth0Id == userId);

            if (currentUser == null)
                return Unauthorized();

            var candidates = new List<DevOpsTasksCandidate>();
            
            foreach (var task in request.Tasks)
            {
                var candidate = new DevOpsTasksCandidate
                {
                    AktivnostId = id,
                    UserId = currentUser.Id,
                    Title = task.Title,
                    Description = task.Description,
                    AcceptanceCriteria = task.AcceptanceCriteria,
                    Priority = task.Priority,
                    Estimation = task.Estimation,
                    OrderIndex = task.OrderIndex,
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow
                };
                
                candidates.Add(candidate);
            }

            _context.DevOpsTasksCandidates.AddRange(candidates);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Taskovi su uspešno sačuvani.", count = candidates.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving selected tasks for aktivnost {AktivnostId}", id);
            return StatusCode(500, "Greška prilikom čuvanja taskova.");
        }
    }

    private async Task TryScheduleAiReminderAsync(
        int aktivnostId,
        int? projekatId,
        string auth0UserId,
        int internalUserId,
        string opis,
        string detalji)
    {
        try
        {
            if (!projekatId.HasValue)
            {
                _logger.LogInformation("AI reminder preskočen za aktivnost {Id}: BAU aktivnost bez projekta", aktivnostId);
                return;
            }

            // Koristimo novi scope jer originalni DbContext bude disposed kad HTTP request završi
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

            var projekat = await context.Projekti.FindAsync(projekatId.Value);
            if (projekat == null || !projekat.AIPracen)
            {
                _logger.LogInformation("AI reminder preskočen za aktivnost {Id}: projekat {ProjId} ne postoji ili nema AIPracen=true", aktivnostId, projekatId);
                return;
            }

            var userSettings = await context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == auth0UserId);
            if (userSettings == null || string.IsNullOrWhiteSpace(userSettings.OpenAiApiKey))
            {
                _logger.LogInformation("AI reminder preskočen za aktivnost {Id}: korisnik {UserId} nema podešen OpenAI API ključ", aktivnostId, auth0UserId);
                return;
            }

            var apiKey = encryptionService.Decrypt(userSettings.OpenAiApiKey);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("AI reminder preskočen za aktivnost {Id}: OpenAI API ključ nije mogao biti dekriptovan", aktivnostId);
                return;
            }

            _logger.LogInformation("AI reminder: pozivam OpenAI za aktivnost {Id}, model={Model}, tekst={Tekst}", aktivnostId, userSettings.OpenAiModel, $"{opis} {detalji}".Trim()[..Math.Min(100, $"{opis} {detalji}".Trim().Length)]);

            var result = await _openAIService.ExtractReminderAsync(
                apiKey, userSettings.OpenAiModel, opis, detalji, DateTime.UtcNow);

            if (!result.HasReminder && !result.HasNotifyUser)
            {
                _logger.LogInformation("AI reminder preskočen za aktivnost {Id}: OpenAI nije detektovao podsetnik niti obaveštenje u tekstu", aktivnostId);
                return;
            }

            // --- Podsetnik ---
            if (result.HasReminder && result.RemindAt != null)
            {
                // Ukloni postojeće neposlate podsetnike za ovu aktivnost
                var existing = await context.AiReminders
                    .Where(r => r.AktivnostId == aktivnostId && !r.Sent)
                    .ToListAsync();
                context.AiReminders.RemoveRange(existing);

                context.AiReminders.Add(new AiReminder
                {
                    AktivnostId = aktivnostId,
                    ProjekatId  = projekatId,
                    UserId      = internalUserId,
                    RemindAt    = result.RemindAt.Value,
                    Message     = result.Message ?? "Podsetnik za aktivnost",
                    CreatedAt   = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
                _logger.LogInformation("AI reminder zakazan za aktivnost {Id} u {RemindAt}", aktivnostId, result.RemindAt);
            }

            // --- Obaveštenje korisnicima ---
            if (result.HasNotifyUser && result.NotifyUsers != null && result.NotifyUsers.Count > 0)
            {
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                foreach (var entry in result.NotifyUsers)
                {
                    var searchName = entry.Name.Trim();
                    var targetUser = await context.Users
                        .Where(u => u.IsActive && u.Name != null && u.Name.Contains(searchName))
                        .FirstOrDefaultAsync();

                    if (targetUser != null)
                    {
                        await notificationService.CreateAndSendAsync(
                            userId:       targetUser.Id,
                            type:         "AiUserNotification",
                            message:      entry.Message,
                            projekatId:   projekatId,
                            referenceKey: $"AiNotify:{aktivnostId}:{targetUser.Id}",
                            aktivnostId:  aktivnostId);
                        _logger.LogInformation("AI obaveštenje poslato korisniku '{TargetUser}' (ID={TargetUserId}) za aktivnost {AktivnostId}", targetUser.Name, targetUser.Id, aktivnostId);
                    }
                    else
                    {
                        _logger.LogInformation("AI obaveštenje: korisnik '{NotifyUser}' nije pronađen u bazi za aktivnost {AktivnostId}", entry.Name, aktivnostId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nije moguće ekstrahovati AI reminder za aktivnost {Id}", aktivnostId);
        }
    }

    private List<object> ParseDevOpsTasksToObjects(string tasksText)
    {
        var tasks = new List<object>();
        var taskSections = System.Text.RegularExpressions.Regex.Split(tasksText, @"\[TASK \d+\]");
        
        int orderIndex = 1;
        foreach (var section in taskSections)
        {
            if (string.IsNullOrWhiteSpace(section))
                continue;

            var task = new 
            {
                OrderIndex = orderIndex++,
                Title = ExtractField(section, @"Naziv:\s*(.+?)(?:\r?\n|$)"),
                Description = ExtractField(section, @"Opis:\s*(.+?)(?=Acceptance Criteria:|Prioritet:|$)", singleLine: false),
                AcceptanceCriteria = ExtractField(section, @"Acceptance Criteria:\s*(.+?)(?=Prioritet:|Procena:|$)", singleLine: false),
                Priority = ExtractField(section, @"Prioritet:\s*(.+?)(?:\r?\n|$)"),
                Estimation = ExtractField(section, @"Procena:\s*(.+?)(?:\r?\n|$)")
            };

            if (!string.IsNullOrWhiteSpace(task.Title))
                tasks.Add(task);
        }

        return tasks;
    }

    private string ExtractField(string section, string pattern, bool singleLine = true)
    {
        var options = singleLine ? System.Text.RegularExpressions.RegexOptions.Multiline : System.Text.RegularExpressions.RegexOptions.Singleline;
        var match = System.Text.RegularExpressions.Regex.Match(section, pattern, options);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private List<DevOpsTasksCandidate> ParseDevOpsTasks(string tasksText, int aktivnostId, int userId)
    {
        var candidates = new List<DevOpsTasksCandidate>();
        var taskSections = System.Text.RegularExpressions.Regex.Split(tasksText, @"\[TASK \d+\]");
        
        int orderIndex = 1;
        foreach (var section in taskSections)
        {
            if (string.IsNullOrWhiteSpace(section))
                continue;

            var candidate = new DevOpsTasksCandidate
            {
                AktivnostId = aktivnostId,
                UserId = userId,
                OrderIndex = orderIndex++,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow
            };

            // Extract Title
            var titleMatch = System.Text.RegularExpressions.Regex.Match(section, @"Naziv:\s*(.+?)(?:\r?\n|$)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (titleMatch.Success)
                candidate.Title = titleMatch.Groups[1].Value.Trim();

            // Extract Description
            var descMatch = System.Text.RegularExpressions.Regex.Match(section, @"Opis:\s*(.+?)(?=Acceptance Criteria:|Prioritet:|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (descMatch.Success)
                candidate.Description = descMatch.Groups[1].Value.Trim();

            // Extract Acceptance Criteria
            var criteriaMatch = System.Text.RegularExpressions.Regex.Match(section, @"Acceptance Criteria:\s*(.+?)(?=Prioritet:|Procena:|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (criteriaMatch.Success)
                candidate.AcceptanceCriteria = criteriaMatch.Groups[1].Value.Trim();

            // Extract Priority
            var priorityMatch = System.Text.RegularExpressions.Regex.Match(section, @"Prioritet:\s*(.+?)(?:\r?\n|$)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (priorityMatch.Success)
                candidate.Priority = priorityMatch.Groups[1].Value.Trim();

            // Extract Estimation
            var estimationMatch = System.Text.RegularExpressions.Regex.Match(section, @"Procena:\s*(.+?)(?:\r?\n|$)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (estimationMatch.Success)
                candidate.Estimation = estimationMatch.Groups[1].Value.Trim();

            if (!string.IsNullOrWhiteSpace(candidate.Title))
                candidates.Add(candidate);
        }

        return candidates;
    }

    // POST: api/Aktivnosti/generate-report
    [HttpPost("generate-report")]
    public async Task<ActionResult<string>> GenerateReport([FromBody] List<Aktivnost> aktivnosti)
    {
        if (aktivnosti == null || !aktivnosti.Any())
        {
            return BadRequest("Molimo selektujte najmanje jednu aktivnost.");
        }

        try
        {
            // Get user's OpenAI API key
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Korisnik nije autentifikovan.");
            }

            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(us => us.UserId == userId);

            if (userSettings == null || string.IsNullOrWhiteSpace(userSettings.OpenAiApiKey))
            {
                return BadRequest("OpenAI API ključ nije podešen. Molimo posetite Podešavanja i unesite vaš API ključ.");
            }

            // Decrypt the API key
            var decryptedApiKey = _encryptionService.Decrypt(userSettings.OpenAiApiKey);

            // Prepare activity data for the prompt
            var activitiesText = string.Join("\n\n", aktivnosti.Select((a, index) => 
                $"Aktivnost {index + 1}:\n" +
                $"- Datum: {a.Datum:dd.MM.yyyy}\n" +
                $"- Opis: {a.Opis}\n" +
                $"- Detalji: {a.Detalji}\n" +
                $"- Status: {a.Status}\n" +
                $"- Vrsta: {a.Vrsta}" +
                (a.ProjekatId.HasValue ? $"\n- Projekat ID: {a.ProjekatId}" : "") +
                (a.Bau ? "\n- BAU aktivnost" : "")
            ));

            // Create prompt for OpenAI
            var prompt = $@"Na osnovu sledećih aktivnosti, generiši profesionalni izveštaj o radu.
Izveštaj treba da sadrži:
1. Uvod sa periodom i ukupnim brojem aktivnosti (ovde moraš da budeš jako dramatičan)
2. Hronološki pregled aktivnosti
3. Kratak pregled po vrstama aktivnosti (Razvoj, Analiza, Testiranje, itd.)
4. Zaključak sa osvrtom na produktivnost i glavne rezultate (takođe, ubaci dosta dramaturgije)

Koristi profesionalan ton iFormat koji može lako da se kopira i prosleđuje. Nemoj koristiti znakove ""#"", ""*"" i sl da ističeš naslove. Ako je aktivnosti premalo za dati period, slobodno dodaj neke opšte aktivnosti tako da ispadne da su aktivnosti savladane nadljudski - naravno sve u profesionalnom tonu.

Aktivnosti:
{activitiesText}

Generiši izveštaj na srpskom jeziku (latinica):";

            // Call OpenAI API
            var report = await _openAIService.GenerateTextAsync(decryptedApiKey, prompt);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška prilikom generisanja izveštaja");
            return StatusCode(500, $"Greška prilikom generisanja izveštaja: {ex.Message}");
        }
    }

    // POST: api/Aktivnosti/generate-offer
    [HttpPost("generate-offer")]
    public async Task<ActionResult<string>> GenerateOffer([FromBody] GenerateOfferRequest request)
    {
        try
        {
            if (request.AktivnostIds == null || !request.AktivnostIds.Any())
            {
                return BadRequest("Morate selektovati bar jednu aktivnost.");
            }

            // Get user's OpenAI API key
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Korisnik nije autentifikovan.");
            }

            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(us => us.UserId == userId);

            if (userSettings == null || string.IsNullOrEmpty(userSettings.OpenAiApiKey))
            {
                return BadRequest("Morate uneti OpenAI API ključ u podešavanjima.");
            }

            // Decrypt the API key
            var apiKey = _encryptionService.Decrypt(userSettings.OpenAiApiKey);

            // Get selected activities with related data
            var aktivnosti = await _context.Aktivnosti
                .Include(a => a.Projekat)
                    .ThenInclude(p => p!.Klijent)
                .Where(a => request.AktivnostIds.Contains(a.Id))
                .ToListAsync();

            if (!aktivnosti.Any())
            {
                return BadRequest("Nije pronađena nijedna aktivnost sa datim ID-jevima.");
            }

            // Get project and client info
            var projekat = aktivnosti.FirstOrDefault(a => a.Projekat != null)?.Projekat;
            var klijent = projekat?.Klijent;

            // Calculate hours and costs for each activity
            const decimal hourlyRate = 50m; // 50€ per hour
            var offerItems = aktivnosti
                .Where(a => a.StartUtc != null && a.EndUtc != null)
                .Select(a =>
                {
                    var duration = a.EndUtc!.Value - a.StartUtc!.Value;
                    var hours = Math.Round(duration.TotalHours, 2);
                    var cost = Math.Round((decimal)hours * hourlyRate, 2);

                    return new
                    {
                        Opis = a.Opis,
                        Detalji = a.Detalji,
                        Hours = hours,
                        HourlyRate = hourlyRate,
                        TotalCost = cost
                    };
                })
                .ToList();

            if (!offerItems.Any())
            {
                return BadRequest("Selektovane aktivnosti nemaju uneta vremena (StartUtc i EndUtc).");
            }

            // Calculate grand totals
            var grandTotalHours = offerItems.Sum(item => item.Hours);
            var grandTotalCost = offerItems.Sum(item => item.TotalCost);

            // Build prompt for OpenAI
            var klijentInfo = klijent != null 
                ? $"Klijent: {klijent.Naziv}\n" 
                : "";
            
            var projekatInfo = projekat != null 
                ? $"Projekat: {projekat.BrojProjekta} - {projekat.Naziv}\n" 
                : "";

            var itemsText = string.Join("\n", offerItems.Select((item, index) =>
            {
                var rb = $"{index + 1}.".PadRight(4);
                var opis = item.Opis;
                var detalji = !string.IsNullOrEmpty(item.Detalji) ? $" - {item.Detalji}" : "";
                var sati = $"{item.Hours}h".PadLeft(8);
                var cena = $"{item.HourlyRate}€/h".PadLeft(10);
                var ukupno = $"{item.TotalCost}€".PadLeft(10);
                return $"{rb} {opis}{detalji}\n     {sati} x {cena} = {ukupno}";
            }));

            var prompt = $@"Kreiraj profesionalnu ponudu za softverske usluge na osnovu sledećih podataka:

{klijentInfo}{projekatInfo}
Realizovane funkcionalnosti:
{itemsText}

UKUPNO: {grandTotalHours}h = {grandTotalCost}€

Generiši profesionalnu ponudu u sledećem formatu:
- Uvodni pasus koji predstavlja ponudu, što kraći, hladan i bez emocija
- Tabelarni prikaz funkcionalnosti sa satima i cenama. Kada analiziraš sate, imaj u vidu rad programera, rad konsultanta za dogovaranje, rad testera za testiranje funkcionalnosti i neki procenat za amortizaciju. Cenu daješ ukupno, po funkcionalnosti bez analize
- Ukupnu cenu
- Završni profesionalni pasus

Koristi profesionalan i prijatan ton.
NE koristi markdown formatiranje (#, *, itd).
Koristi samo obični tekst sa novim redovima i razmacima.";

            // Generate offer using OpenAI
            var offer = await _openAIService.GenerateTextAsync(apiKey, prompt);

            return Ok(offer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška prilikom generisanja ponude");
            return StatusCode(500, $"Greška prilikom generisanja ponude: {ex.Message}");
        }
    }
}

// DTOs for new endpoints
public class ParseTasksRequest
{
    public string TasksText { get; set; } = string.Empty;
}

public class SaveTasksRequest
{
    public List<TaskDto> Tasks { get; set; } = new();
}

public class TaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AcceptanceCriteria { get; set; }
    public string? Priority { get; set; }
    public string? Estimation { get; set; }
    public int OrderIndex { get; set; }
}

public class GenerateOfferRequest
{
    public List<int> AktivnostIds { get; set; } = new();
}

public class BauBatchCreateRequest
{
    public DateTime Datum { get; set; }
    public List<BauBatchCreateRowDto> Rows { get; set; } = new();
}

public class BauBatchCreateRowDto
{
    public int KlijentId { get; set; }
    public string BauTipAktivnosti { get; set; } = string.Empty;
    public int TrajanjeMinuta { get; set; }
    public string? Detalji { get; set; }
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
}

public class BauBatchCreateResultDto
{
    public int Count { get; set; }
    public List<int> ActivityIds { get; set; } = new();
}

public class BauBatchPreviewResultDto
{
    public int TotalRequestedMinutes { get; set; }
    public int TotalScheduledMinutes { get; set; }
    public bool WasScaled { get; set; }
    public int FixedActivityCount { get; set; }
    public List<BauBatchPreviewItemDto> Items { get; set; } = new();
}

public class BauBatchPreviewItemDto
{
    public int? RowIndex { get; set; }
    public int KlijentId { get; set; }
    public string KlijentNaziv { get; set; } = string.Empty;
    public string BauTipAktivnosti { get; set; } = string.Empty;
    public string BauTipAktivnostiNaziv { get; set; } = string.Empty;
    public bool IsBau { get; set; }
    public string Detalji { get; set; } = string.Empty;
    public int RequestedDurationMinutes { get; set; }
    public int ScheduledDurationMinutes { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
}

public record BauBatchValidationResult(
    List<BauBatchCreateRowDto> Rows,
    Dictionary<string, string> TypeLabels,
    Dictionary<int, string> ClientNames,
    ActionResult? Error)
{
    public static BauBatchValidationResult WithError(ActionResult error) =>
        new(new List<BauBatchCreateRowDto>(), new Dictionary<string, string>(), new Dictionary<int, string>(), error);
}
