using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CodebooksController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CodebooksController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/codebooks/entities
    [HttpGet("entities")]
    public async Task<ActionResult<IEnumerable<CodebookEntityDto>>> GetEntities()
    {
        var entities = await _context.CodebookEntities
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new CodebookEntityDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description
            })
            .ToListAsync();

        return Ok(entities);
    }

    // GET: api/codebooks/entity/{entityTypeId}
    [HttpGet("entity/{entityTypeId}")]
    public async Task<ActionResult<IEnumerable<CodebookDto>>> GetByEntityType(int entityTypeId)
    {
        var codebooks = await _context.Codebooks
            .Where(c => c.EntityTypeId == entityTypeId && c.IsActive)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Value)
            .Select(c => new CodebookDto
            {
                Code = c.Code,
                Value = c.Value
            })
            .ToListAsync();

        return Ok(codebooks);
    }

    // GET: api/codebooks/entityname/{entityName}
    [HttpGet("entityname/{entityName}")]
    public async Task<ActionResult<IEnumerable<CodebookDto>>> GetByEntityName(string entityName)
    {
        var codebooks = await _context.Codebooks
            .Include(c => c.EntityType)
            .Where(c => c.EntityType!.Name == entityName && c.IsActive && c.EntityType.IsActive)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Value)
            .Select(c => new CodebookDto
            {
                Code = c.Code,
                Value = c.Value
            })
            .ToListAsync();

        return Ok(codebooks);
    }
}

public class CodebookEntityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CodebookDto
{
    public string Code { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
