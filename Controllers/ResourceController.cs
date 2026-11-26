using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Models;
using HumanitarianAPI.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HumanitarianAPI.Controllers
{
    [Route("api/Resources")]
    [ApiController]
    public class ResourceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ResourceController(ApplicationDbContext context) 
        { 
            _context = context; 
        }

        // 1. Get all resources
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Resource>>> GetResources() 
        { 
            return await _context.Resources.ToListAsync(); 
        }

        // Get a Resource by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Resource>> GetResource(int id)
        {
            var resource = await _context.Resources.FindAsync(id);

            if (resource == null)
            {
                return NotFound(new { message = "Resource not found." });
            }

            return Ok(resource);
        }

        // 2. Adding a new resource (POST)
        [HttpPost]
        public async Task<ActionResult<Resource>> CreateResource(
            [FromBody] Resource newResource)
        {
            if (string.IsNullOrWhiteSpace(newResource.Name))
            {
                return BadRequest(new { message = "Resource name is required." });
            }

            if (newResource.Quantity < 0)
            {
                return BadRequest(new { message = "Quantity cannot be negative." });
            }

            if (string.IsNullOrWhiteSpace(newResource.Location))
            {
                return BadRequest(new { message = "Location is required." });
            }

            if (string.IsNullOrWhiteSpace(newResource.Status))
            {
                return BadRequest(new { message = "Status is required." });
            }

            _context.Resources.Add(newResource);
            await _context.SaveChangesAsync();

            return Ok(newResource);
        }

        // 3. Updating a resource (PUT)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutResource(int id, [FromBody] Resource resource)
        {
            if (id != resource.Id)
            {
                return BadRequest(new { message = "Resource ID mismatch" });
            }

            if (string.IsNullOrWhiteSpace(resource.Name) ||
                string.IsNullOrWhiteSpace(resource.Location) ||
                string.IsNullOrWhiteSpace(resource.Status) ||
                resource.Quantity < 0)
            {
                return BadRequest(new
                {
                    message = "Invalid resource details."
                });
            }

            var existingResource = await _context.Resources.FindAsync(id);
            if (existingResource == null)
            {
                return NotFound(new { message = "Resource not found" });
            }

            existingResource.Name = resource.Name;
            existingResource.Quantity = resource.Quantity;
            existingResource.Location = resource.Location;
            existingResource.Status = resource.Status;
            existingResource.CrisisEventId = resource.CrisisEventId;
            existingResource.ShelterId = resource.ShelterId;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Resource updated successfully!", resource = existingResource });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResourceExists(id))
                {
                    return NotFound(new { message = "Resource not found during update" });
                }
                else
                {
                    throw;
                }
            }
        }

        // 4. Removing a resource (DELETE)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResource(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { message = "Resource not found" });
            }

            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Resource deleted successfully!" });
        }

        private bool ResourceExists(int id)
        {
            return _context.Resources.Any(e => e.Id == id);
        }
    }
}
