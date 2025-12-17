using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DokumentiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public DokumentiController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        var connectionString = configuration.GetConnectionString("BlobStorage");
        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerName = configuration["BlobStorage:ContainerName"] ?? "project-documents";
    }

    // GET: api/dokumenti/projekat/5
    [HttpGet("projekat/{projekatId}")]
    public async Task<ActionResult<IEnumerable<Dokument>>> GetByProjekatId(int projekatId)
    {
        var dokumenti = await _context.Dokumenti
            .Where(d => d.ProjekatId == projekatId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return Ok(dokumenti);
    }

    // POST: api/dokumenti/upload/5
    [HttpPost("upload/{projekatId}")]
    public async Task<ActionResult<Dokument>> UploadDokument(int projekatId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Fajl nije prosleđen");
        }

        // Proveri da li projekat postoji
        var projekat = await _context.Projekti.FindAsync(projekatId);
        if (projekat == null)
        {
            return NotFound("Projekat nije pronađen");
        }

        try
        {
            // Generiši unique blob name
            var extension = Path.GetExtension(file.FileName);
            var blobName = $"{projekatId}/{Guid.NewGuid()}{extension}";

            // Upload u Blob Storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            
            var blobClient = containerClient.GetBlobClient(blobName);
            
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = file.ContentType
                    }
                });
            }

            // Kreiraj metadata zapis u bazi
            var dokument = new Dokument
            {
                ProjekatId = projekatId,
                NazivFajla = file.FileName,
                TipFajla = extension.TrimStart('.').ToLower(),
                BlobUrl = blobClient.Uri.ToString(),
                Velicina = file.Length,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Dokumenti.Add(dokument);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByProjekatId), new { projekatId = dokument.ProjekatId }, dokument);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Greška pri upload-u: {ex.Message}");
        }
    }

    // GET: api/dokumenti/5/download
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDokument(int id)
    {
        var dokument = await _context.Dokumenti.FindAsync(id);
        if (dokument == null)
        {
            return NotFound();
        }

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobName = dokument.BlobUrl.Split('/').Skip(4).Last(); // Extract blob name from URL
            var blobClient = containerClient.GetBlobClient($"{dokument.ProjekatId}/{blobName.Split('/').Last()}");

            if (!await blobClient.ExistsAsync())
            {
                return NotFound("Fajl ne postoji u storage-u");
            }

            var download = await blobClient.DownloadAsync();
            
            return File(download.Value.Content, download.Value.ContentType, dokument.NazivFajla);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Greška pri preuzimanju: {ex.Message}");
        }
    }

    // DELETE: api/dokumenti/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDokument(int id)
    {
        var dokument = await _context.Dokumenti.FindAsync(id);
        if (dokument == null)
        {
            return NotFound();
        }

        try
        {
            // Obriši iz Blob Storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobName = dokument.BlobUrl.Split('/').Skip(4).Last();
            var blobClient = containerClient.GetBlobClient($"{dokument.ProjekatId}/{blobName.Split('/').Last()}");
            
            await blobClient.DeleteIfExistsAsync();

            // Obriši iz baze
            _context.Dokumenti.Remove(dokument);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Greška pri brisanju: {ex.Message}");
        }
    }
}
