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

    // GET: api/codebooks/{type}
    [HttpGet("{type}")]
    public async Task<ActionResult<IEnumerable<CodebookDto>>> GetByType(string type)
    {
        var codebooks = await _context.Codebooks
            .Where(c => c.Type == type && c.IsActive)
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

    // GET: api/codebooks/types
    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<string>>> GetTypes()
    {
        var types = await _context.Codebooks
            .Where(c => c.IsActive)
            .Select(c => c.Type)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        return Ok(types);
    }
}

public class CodebookDto
{
    public string Code { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
