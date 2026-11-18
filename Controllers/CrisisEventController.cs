using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Data;
using HumanitarianAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HumanitarianAPI.Controllers
{
    public class AssignShelterDto
    {
        public int ShelterId { get; set; }
    }

    public class AssignResourceDto
    {
        public int ResourceId { get; set; }
        public int? ShelterId { get; set; }
    }

    public class AssignVolunteerDto
    {
        public int VolunteerId { get; set; }
        public int? ShelterId { get; set; }
    }

    public class AssignDonationDto
    {
        public int DonationId { get; set; }
    }

    [Route("api/CrisisEvents")]
    [ApiController]
    public class CrisisEventController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CrisisEventController(ApplicationDbContext context) 
        { 
            _context = context; 
        }

        // 1. සියලුම Crisis Events ලබාගැනීම (GET)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CrisisEvent>>> GetCrisisEvents() 
        { 
            return await _context.CrisisEvents.ToListAsync(); 
        }

        // 2. තනි Crisis Event එකක් ලබාගැනීම (GET by ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<CrisisEvent>> GetCrisisEvent(int id)
        {
            var crisisEvent = await _context.CrisisEvents.FindAsync(id);
            if (crisisEvent == null) return NotFound(new { message = "Crisis event not found" });
            return Ok(crisisEvent);
        }

        // 3. අලුත් Crisis Event එකක් ඇතුළත් කිරීම (POST)
        [HttpPost]
        public async Task<ActionResult<CrisisEvent>> PostCrisisEvent([FromBody] CrisisEvent crisisEvent)
        {
            _context.CrisisEvents.Add(crisisEvent);
            await _context.SaveChangesAsync();
            return Ok(crisisEvent);
        }

        // 4. Crisis Event එකක් Update කිරීම (PUT)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCrisisEvent(int id, [FromBody] CrisisEvent crisisEvent)
        {
            if (id != crisisEvent.Id)
            {
                return BadRequest(new { message = "Crisis Event ID mismatch" });
            }

            var existingEvent = await _context.CrisisEvents.FindAsync(id);
            if (existingEvent == null)
            {
                return NotFound(new { message = "Crisis event not found" });
            }

            existingEvent.Title = crisisEvent.Title;
            existingEvent.EventType = crisisEvent.EventType;
            existingEvent.Location = crisisEvent.Location;
            existingEvent.Severity = crisisEvent.Severity;
            existingEvent.DateReported = crisisEvent.DateReported;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Crisis event updated successfully!", crisisEvent = existingEvent });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CrisisEventExists(id))
                {
                    return NotFound(new { message = "Crisis event not found during update" });
                }
                else
                {
                    throw;
                }
            }
        }

        // 5. Crisis Event එකක් ඉවත් කිරීම (DELETE)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCrisisEvent(int id)
        {
            var crisisEvent = await _context.CrisisEvents.FindAsync(id);
            if (crisisEvent == null)
            {
                return NotFound(new { message = "Crisis event not found" });
            }

            // Unlink connected entities so they don't reference a deleted crisis
            var connectedShelters = await _context.Shelters.Where(s => s.CrisisEventId == id).ToListAsync();
            foreach (var s in connectedShelters) s.CrisisEventId = null;

            var connectedResources = await _context.Resources.Where(r => r.CrisisEventId == id).ToListAsync();
            foreach (var r in connectedResources) { r.CrisisEventId = null; r.ShelterId = null; }

            var connectedVolunteers = await _context.Volunteers.Where(v => v.CrisisEventId == id).ToListAsync();
            foreach (var v in connectedVolunteers) { v.CrisisEventId = null; v.ShelterId = null; }

            var connectedDonations = await _context.Donations.Where(d => d.CrisisEventId == id).ToListAsync();
            foreach (var d in connectedDonations) d.CrisisEventId = null;

            _context.CrisisEvents.Remove(crisisEvent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Crisis event deleted successfully!" });
        }

        // =========================================================================
        // 6. CRISIS EVENT COORDINATION & INTERCONNECTION ENDPOINTS (ADMIN COMMAND)
        // =========================================================================

        // Get single crisis connections (Shelters, Resources, Volunteers, Donations, Summary)
        [HttpGet("{id}/connections")]
        public async Task<IActionResult> GetCrisisConnections(int id)
        {
            var crisisEvent = await _context.CrisisEvents.FindAsync(id);
            if (crisisEvent == null) return NotFound(new { message = "Crisis event not found" });

            var shelters = await _context.Shelters.Where(s => s.CrisisEventId == id).ToListAsync();
            var resources = await _context.Resources.Where(r => r.CrisisEventId == id).ToListAsync();
            var volunteers = await _context.Volunteers.Where(v => v.CrisisEventId == id).ToListAsync();
            var donations = await _context.Donations.Where(d => d.CrisisEventId == id).ToListAsync();

            decimal totalDonations = donations.Sum(d => d.Amount);
            int totalCapacity = shelters.Sum(s => s.Capacity);
            int totalOccupancy = shelters.Sum(s => s.CurrentOccupancy);
            int totalResourcesQuantity = resources.Sum(r => r.Quantity);

            return Ok(new
            {
                crisisEvent,
                shelters,
                resources,
                volunteers,
                donations,
                summary = new
                {
                    totalShelters = shelters.Count,
                    totalCapacity,
                    totalOccupancy,
                    availableShelterSlots = Math.Max(0, totalCapacity - totalOccupancy),
                    totalResources = resources.Count,
                    totalResourcesQuantity,
                    totalVolunteers = volunteers.Count,
                    totalDonationsCount = donations.Count,
                    totalDonationsAmount = totalDonations
                }
            });
        }

        // Get full system overview matrix (All Crises + All Unassigned items for easy linking)
        [HttpGet("all-connections-matrix")]
        public async Task<IActionResult> GetAllConnectionsMatrix()
        {
            var crises = await _context.CrisisEvents.ToListAsync();
            var allShelters = await _context.Shelters.ToListAsync();
            var allResources = await _context.Resources.ToListAsync();
            var allVolunteers = await _context.Volunteers.ToListAsync();
            var allDonations = await _context.Donations.ToListAsync();

            var crisisMatrices = crises.Select(c => new
            {
                crisisEvent = c,
                shelters = allShelters.Where(s => s.CrisisEventId == c.Id).ToList(),
                resources = allResources.Where(r => r.CrisisEventId == c.Id).ToList(),
                volunteers = allVolunteers.Where(v => v.CrisisEventId == c.Id).ToList(),
                donations = allDonations.Where(d => d.CrisisEventId == c.Id).ToList(),
                stats = new
                {
                    sheltersCount = allShelters.Count(s => s.CrisisEventId == c.Id),
                    resourcesCount = allResources.Count(r => r.CrisisEventId == c.Id),
                    volunteersCount = allVolunteers.Count(v => v.CrisisEventId == c.Id),
                    donationsCount = allDonations.Count(d => d.CrisisEventId == c.Id),
                    donationsTotal = allDonations.Where(d => d.CrisisEventId == c.Id).Sum(d => d.Amount)
                }
            }).ToList();

            var unassigned = new
            {
                shelters = allShelters.Where(s => s.CrisisEventId == null || s.CrisisEventId == 0).ToList(),
                resources = allResources.Where(r => r.CrisisEventId == null || r.CrisisEventId == 0).ToList(),
                volunteers = allVolunteers.Where(v => v.CrisisEventId == null || v.CrisisEventId == 0).ToList(),
                donations = allDonations.Where(d => d.CrisisEventId == null || d.CrisisEventId == 0).ToList()
            };

            return Ok(new
            {
                crises = crisisMatrices,
                unassigned
            });
        }

        // Connect Shelter to Crisis
        [HttpPost("{id}/connect-shelter")]
        public async Task<IActionResult> ConnectShelter(int id, [FromBody] AssignShelterDto dto)
        {
            var crisis = await _context.CrisisEvents.FindAsync(id);
            if (crisis == null) return NotFound(new { message = "Crisis event not found" });

            var shelter = await _context.Shelters.FindAsync(dto.ShelterId);
            if (shelter == null) return NotFound(new { message = "Shelter not found" });

            shelter.CrisisEventId = id;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Shelter '{shelter.Name}' connected to Crisis '{crisis.Title}'", shelter });
        }

        // Disconnect Shelter from Crisis
        [HttpPost("{id}/disconnect-shelter/{shelterId}")]
        public async Task<IActionResult> DisconnectShelter(int id, int shelterId)
        {
            var shelter = await _context.Shelters.FindAsync(shelterId);
            if (shelter == null) return NotFound(new { message = "Shelter not found" });

            if (shelter.CrisisEventId == id)
            {
                shelter.CrisisEventId = null;
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "Shelter disconnected from crisis", shelter });
        }

        // Connect Resource to Crisis (and optionally to a Shelter)
        [HttpPost("{id}/connect-resource")]
        public async Task<IActionResult> ConnectResource(int id, [FromBody] AssignResourceDto dto)
        {
            var crisis = await _context.CrisisEvents.FindAsync(id);
            if (crisis == null) return NotFound(new { message = "Crisis event not found" });

            var resource = await _context.Resources.FindAsync(dto.ResourceId);
            if (resource == null) return NotFound(new { message = "Resource not found" });

            resource.CrisisEventId = id;
            if (dto.ShelterId.HasValue && dto.ShelterId.Value > 0)
            {
                resource.ShelterId = dto.ShelterId.Value;
            }
            resource.Status = "Dispatched";
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Resource '{resource.Name}' allocated to Crisis '{crisis.Title}'", resource });
        }

        // Disconnect Resource from Crisis
        [HttpPost("{id}/disconnect-resource/{resourceId}")]
        public async Task<IActionResult> DisconnectResource(int id, int resourceId)
        {
            var resource = await _context.Resources.FindAsync(resourceId);
            if (resource == null) return NotFound(new { message = "Resource not found" });

            if (resource.CrisisEventId == id)
            {
                resource.CrisisEventId = null;
                resource.ShelterId = null;
                resource.Status = "Available";
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "Resource unallocated from crisis", resource });
        }

        // Connect Volunteer to Crisis (and optionally to a Shelter)
        [HttpPost("{id}/connect-volunteer")]
        public async Task<IActionResult> ConnectVolunteer(int id, [FromBody] AssignVolunteerDto dto)
        {
            var crisis = await _context.CrisisEvents.FindAsync(id);
            if (crisis == null) return NotFound(new { message = "Crisis event not found" });

            var volunteer = await _context.Volunteers.FindAsync(dto.VolunteerId);
            if (volunteer == null) return NotFound(new { message = "Volunteer not found" });

            volunteer.CrisisEventId = id;
            if (dto.ShelterId.HasValue && dto.ShelterId.Value > 0)
            {
                volunteer.ShelterId = dto.ShelterId.Value;
            }
            volunteer.Status = "Deployed";
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Volunteer '{volunteer.Name}' deployed to Crisis '{crisis.Title}'", volunteer });
        }

        // Disconnect Volunteer from Crisis
        [HttpPost("{id}/disconnect-volunteer/{volunteerId}")]
        public async Task<IActionResult> DisconnectVolunteer(int id, int volunteerId)
        {
            var volunteer = await _context.Volunteers.FindAsync(volunteerId);
            if (volunteer == null) return NotFound(new { message = "Volunteer not found" });

            if (volunteer.CrisisEventId == id)
            {
                volunteer.CrisisEventId = null;
                volunteer.ShelterId = null;
                volunteer.Status = "Available";
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "Volunteer unassigned from crisis", volunteer });
        }

        // Connect Donation to Crisis
        [HttpPost("{id}/connect-donation")]
        public async Task<IActionResult> ConnectDonation(int id, [FromBody] AssignDonationDto dto)
        {
            var crisis = await _context.CrisisEvents.FindAsync(id);
            if (crisis == null) return NotFound(new { message = "Crisis event not found" });

            var donation = await _context.Donations.FindAsync(dto.DonationId);
            if (donation == null) return NotFound(new { message = "Donation not found" });

            donation.CrisisEventId = id;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Donation #{donation.Id} allocated to Crisis '{crisis.Title}'", donation });
        }

        // Disconnect Donation from Crisis
        [HttpPost("{id}/disconnect-donation/{donationId}")]
        public async Task<IActionResult> DisconnectDonation(int id, int donationId)
        {
            var donation = await _context.Donations.FindAsync(donationId);
            if (donation == null) return NotFound(new { message = "Donation not found" });

            if (donation.CrisisEventId == id)
            {
                donation.CrisisEventId = null;
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "Donation unallocated from crisis", donation });
        }

        private bool CrisisEventExists(int id)
        {
            return _context.CrisisEvents.Any(e => e.Id == id);
        }
    }
}
