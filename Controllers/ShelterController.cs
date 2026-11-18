using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Data;
using HumanitarianAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HumanitarianAPI.Controllers
{
    [Route("api/Shelters")]
    [ApiController]
    public class ShelterController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ShelterController(ApplicationDbContext context) 
        { 
            _context = context; 
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Shelter>>> GetShelters() 
        { 
            return await _context.Shelters.ToListAsync(); 
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Shelter>> GetShelter(int id)
        {
            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null) return NotFound(new { message = "Shelter not found" });
            return Ok(shelter);
        }

        [HttpPost]
        public async Task<ActionResult<Shelter>> CreateShelter([FromBody] Shelter newShelter)
        {
            _context.Shelters.Add(newShelter);
            await _context.SaveChangesAsync();
            return Ok(newShelter);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutShelter(int id, [FromBody] Shelter shelter)
        {
            if (id != shelter.Id) return BadRequest(new { message = "Shelter ID mismatch" });

            var existingShelter = await _context.Shelters.FindAsync(id);
            if (existingShelter == null) return NotFound(new { message = "Shelter not found" });

            existingShelter.Name = shelter.Name;
            existingShelter.Location = shelter.Location;
            existingShelter.Capacity = shelter.Capacity;
            existingShelter.CurrentOccupancy = shelter.CurrentOccupancy;
            existingShelter.CrisisEventId = shelter.CrisisEventId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Shelter updated successfully!", shelter = existingShelter });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShelter(int id)
        {
            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null) return NotFound(new { message = "Shelter not found" });

            // Unlink resources or volunteers pointing to this shelter
            var resources = await _context.Resources.Where(r => r.ShelterId == id).ToListAsync();
            foreach (var r in resources) r.ShelterId = null;

            var volunteers = await _context.Volunteers.Where(v => v.ShelterId == id).ToListAsync();
            foreach (var v in volunteers) v.ShelterId = null;

            _context.Shelters.Remove(shelter);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shelter deleted successfully!" });
        }
    }
}
