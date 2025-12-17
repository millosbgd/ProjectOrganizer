using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/notes/projekat/5
    [HttpGet("projekat/{projekatId}")]
    public async Task<ActionResult<IEnumerable<Note>>> GetByProjekatId(int projekatId)
    {
        var notes = await _context.Notes
            .Where(n => n.ProjekatId == projekatId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(notes);
    }

    // GET: api/notes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Note>> GetById(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        
        if (note == null)
        {
            return NotFound();
        }

        return Ok(note);
    }

    // POST: api/notes
    [HttpPost]
    public async Task<ActionResult<Note>> Create(Note note)
    {
        note.CreatedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = note.Id }, note);
    }

    // PUT: api/notes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Note note)
    {
        if (id != note.Id)
        {
            return BadRequest();
        }

        note.UpdatedAt = DateTime.UtcNow;
        _context.Entry(note).State = EntityState.Modified;
        _context.Entry(note).Property(n => n.CreatedAt).IsModified = false;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Notes.AnyAsync(n => n.Id == id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/notes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note == null)
        {
            return NotFound();
        }

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
