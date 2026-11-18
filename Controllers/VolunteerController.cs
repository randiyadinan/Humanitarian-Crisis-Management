using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Models;
using HumanitarianAPI.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HumanitarianAPI.Controllers
{
    [Route("api/Volunteers")]
    [ApiController]
    public class VolunteerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VolunteerController(ApplicationDbContext context) 
        { 
            _context = context; 
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Volunteer>>> GetVolunteers() 
        { 
            return await _context.Volunteers.ToListAsync(); 
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Volunteer>> GetVolunteer(int id)
        {
            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null) return NotFound(new { message = "Volunteer not found" });
            return Ok(volunteer);
        }

        [HttpPost]
        public async Task<ActionResult<Volunteer>> CreateVolunteer([FromBody] Volunteer newVolunteer)
        {
            _context.Volunteers.Add(newVolunteer);
            await _context.SaveChangesAsync();
            return Ok(newVolunteer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutVolunteer(int id, [FromBody] Volunteer volunteer)
        {
            if (id != volunteer.Id) return BadRequest(new { message = "Volunteer ID mismatch" });

            var existingVolunteer = await _context.Volunteers.FindAsync(id);
            if (existingVolunteer == null) return NotFound(new { message = "Volunteer not found" });

            existingVolunteer.Name = volunteer.Name;
            existingVolunteer.Skills = volunteer.Skills;
            existingVolunteer.ContactNumber = volunteer.ContactNumber;
            existingVolunteer.Status = volunteer.Status;
            existingVolunteer.CrisisEventId = volunteer.CrisisEventId;
            existingVolunteer.ShelterId = volunteer.ShelterId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Volunteer updated successfully!", volunteer = existingVolunteer });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVolunteer(int id)
        {
            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null) return NotFound(new { message = "Volunteer not found" });

            _context.Volunteers.Remove(volunteer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Volunteer deleted successfully!" });
        }
    }
}
