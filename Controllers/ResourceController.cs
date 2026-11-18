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

        // 1. සියලුම Resources ලබාගැනීම (GET)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Resource>>> GetResources() 
        { 
            return await _context.Resources.ToListAsync(); 
        }

        // 2. අලුත් Resource එකක් ඇතුළත් කිරීම (POST)
        [HttpPost]
        public async Task<ActionResult<Resource>> CreateResource([FromBody] Resource newResource)
        {
            _context.Resources.Add(newResource);
            await _context.SaveChangesAsync();
            return Ok(newResource);
        }

        // 3. Resource එකක් Update කිරීම (PUT)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutResource(int id, [FromBody] Resource resource)
        {
            if (id != resource.Id)
            {
                return BadRequest(new { message = "Resource ID mismatch" });
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

        // 4. Resource එකක් ඉවත් කිරීම (DELETE)
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
