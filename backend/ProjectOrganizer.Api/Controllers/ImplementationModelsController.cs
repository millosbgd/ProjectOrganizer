using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ImplementationModelsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ImplementationModelsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ImplementationModels
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ImplementationModel>>> GetImplementationModels(
            [FromQuery] bool? aktivan = null)
        {
            var query = _context.ImplementationModels
                .Include(m => m.Items)
                .AsQueryable();

            if (aktivan.HasValue)
                query = query.Where(m => m.Aktivan == aktivan.Value);

            var models = await query.OrderBy(m => m.Naziv).ToListAsync();
            return Ok(models);
        }

        // GET: api/ImplementationModels/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ImplementationModel>> GetImplementationModel(int id)
        {
            var model = await _context.ImplementationModels
                .Include(m => m.Items)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (model == null)
            {
                return NotFound();
            }

            return Ok(model);
        }

        // POST: api/ImplementationModels
        [HttpPost]
        public async Task<ActionResult<ImplementationModel>> CreateImplementationModel(ImplementationModel model)
        {
            _context.ImplementationModels.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetImplementationModel), new { id = model.Id }, model);
        }

        // PUT: api/ImplementationModels/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateImplementationModel(int id, ImplementationModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var existingModel = await _context.ImplementationModels
                .Include(m => m.Items)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingModel == null)
            {
                return NotFound();
            }

            // Update model properties
            existingModel.Naziv = model.Naziv;
            existingModel.Opis = model.Opis;
            existingModel.Aktivan = model.Aktivan;

            // Update items - remove deleted, add new, update existing
            var existingItemIds = existingModel.Items.Select(i => i.Id).ToList();
            var incomingItemIds = model.Items.Where(i => i.Id != 0).Select(i => i.Id).ToList();

            // Remove deleted items
            var itemsToRemove = existingModel.Items.Where(i => !incomingItemIds.Contains(i.Id)).ToList();
            foreach (var item in itemsToRemove)
            {
                _context.ImplementationItems.Remove(item);
            }

            // Add or update items
            foreach (var item in model.Items)
            {
                if (item.Id == 0)
                {
                    // New item
                    item.ImplementationModelId = id;
                    _context.ImplementationItems.Add(item);
                }
                else
                {
                    // Update existing item
                    var existingItem = existingModel.Items.FirstOrDefault(i => i.Id == item.Id);
                    if (existingItem != null)
                    {
                        existingItem.Naziv = item.Naziv;
                        existingItem.Detalji = item.Detalji;
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ImplementationModelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/ImplementationModels/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImplementationModel(int id)
        {
            var model = await _context.ImplementationModels.FindAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            _context.ImplementationModels.Remove(model);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ImplementationModelExists(int id)
        {
            return _context.ImplementationModels.Any(e => e.Id == id);
        }
    }
}
