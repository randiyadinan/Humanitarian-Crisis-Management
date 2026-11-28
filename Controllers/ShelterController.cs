using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Data;
using HumanitarianAPI.Models;

namespace HumanitarianAPI.Controllers
{
    [Authorize]
    [Route("api/Shelters")]
    [ApiController]
    public class ShelterController : ControllerBase
    {
        private static readonly string[] AllowedStatuses = { "Open", "Full", "Closed" };

        private readonly ApplicationDbContext _context;

        public ShelterController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Shelters
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShelterResponseDto>>> GetShelters()
        {
            var shelters = await _context.Shelters
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .ToListAsync();

            return Ok(shelters.Select(MapToResponse).ToList());
        }

        // GET: api/Shelters/available?minCapacity=5&status=Open
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<ShelterResponseDto>>> GetAvailableShelters(
            [FromQuery] int minCapacity = 1,
            [FromQuery] string? status = null)
        {
            if (minCapacity < 0)
            {
                return BadRequest(new { message = "Minimum capacity cannot be negative." });
            }

            var query = _context.Shelters
                .AsNoTracking()
                .Where(s => !string.Equals(s.Status, "Closed", StringComparison.OrdinalIgnoreCase)
                            && (s.Capacity - s.CurrentOccupancy) >= minCapacity);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var requestedStatus = status.Trim();
                query = query.Where(s => string.Equals(s.Status, requestedStatus, StringComparison.OrdinalIgnoreCase));
            }

            var shelters = await query.OrderBy(s => s.Id).ToListAsync();
            return Ok(shelters.Select(MapToResponse).ToList());
        }

        // GET: api/Shelters/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ShelterResponseDto>> GetShelter(int id)
        {
            var shelter = await _context.Shelters.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (shelter == null)
            {
                return NotFound(new { message = "Shelter not found." });
            }

            return Ok(MapToResponse(shelter));
        }

        // GET: api/Shelters/5/capacity
        [HttpGet("{id:int}/capacity")]
        public async Task<ActionResult> GetCapacity(int id)
        {
            var shelter = await _context.Shelters.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (shelter == null)
            {
                return NotFound(new { message = "Shelter not found." });
            }

            return Ok(new
            {
                shelter.Id,
                Capacity = shelter.Capacity,
                CurrentOccupancy = shelter.CurrentOccupancy,
                AvailableCapacity = Math.Max(0, shelter.Capacity - shelter.CurrentOccupancy),
                shelter.Status
            });
        }

        // GET: api/Shelters/5/occupants
        [HttpGet("{id:int}/occupants")]
        public async Task<ActionResult<IEnumerable<ShelterOccupant>>> GetOccupants(int id)
        {
            if (!await ShelterExists(id))
            {
                return NotFound(new { message = "Shelter not found." });
            }

            var occupants = await _context.ShelterOccupants
                .AsNoTracking()
                .Where(o => o.ShelterId == id)
                .OrderBy(o => o.Id)
                .ToListAsync();

            return Ok(occupants);
        }

        // POST: api/Shelters
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ShelterResponseDto>> CreateShelter([FromBody] ShelterCreateDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState.IsValid ? new { message = "Invalid shelter details." } : ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Location))
            {
                return BadRequest(new { message = "Shelter name and location are required." });
            }

            if (request.Capacity < 0 || request.CurrentOccupancy < 0 || request.CurrentOccupancy > request.Capacity)
            {
                return BadRequest(new { message = "Invalid shelter capacity or occupancy." });
            }

            var status = NormalizeStatus(request.Status)
                         ?? (request.CurrentOccupancy >= request.Capacity ? "Full" : "Open");

            if (!await CrisisExists(request.CrisisEventId))
            {
                return BadRequest(new { message = "The specified crisis event does not exist." });
            }

            var shelter = new Shelter
            {
                Name = request.Name.Trim(),
                Location = request.Location.Trim(),
                Capacity = request.Capacity,
                CurrentOccupancy = request.CurrentOccupancy,
                Status = status,
                CrisisEventId = request.CrisisEventId
            };

            _context.Shelters.Add(shelter);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetShelter), new { id = shelter.Id }, MapToResponse(shelter));
        }

        // PUT: api/Shelters/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateShelter(int id, [FromBody] ShelterUpdateDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState.IsValid ? new { message = "Invalid shelter details." } : ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Location))
            {
                return BadRequest(new { message = "Shelter name and location are required." });
            }

            if (request.Capacity < 0 || request.CurrentOccupancy < 0 || request.CurrentOccupancy > request.Capacity)
            {
                return BadRequest(new { message = "Invalid shelter capacity or occupancy." });
            }

            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null)
            {
                return NotFound(new { message = "Shelter not found." });
            }

            var assignedPersons = await _context.ShelterOccupants
                .Where(o => o.ShelterId == id)
                .SumAsync(o => o.PersonCount);

            if (request.CurrentOccupancy < assignedPersons)
            {
                return BadRequest(new
                {
                    message = $"Occupancy cannot be lower than the assigned occupant count ({assignedPersons})."
                });
            }

            if (!string.IsNullOrWhiteSpace(request.Status) && NormalizeStatus(request.Status) == null)
            {
                return BadRequest(new { message = $"Invalid status. Allowed values: {string.Join(", ", AllowedStatuses)}." });
            }

            if (!await CrisisExists(request.CrisisEventId))
            {
                return BadRequest(new { message = "The specified crisis event does not exist." });
            }

            shelter.Name = request.Name.Trim();
            shelter.Location = request.Location.Trim();
            shelter.Capacity = request.Capacity;
            shelter.CurrentOccupancy = request.CurrentOccupancy;
            shelter.Status = string.IsNullOrWhiteSpace(request.Status)
                ? shelter.Status
                : NormalizeStatus(request.Status)!;
            shelter.CrisisEventId = request.CrisisEventId;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Shelter updated successfully.", shelter = MapToResponse(shelter) });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ShelterExists(id))
                {
                    return NotFound(new { message = "Shelter not found during update." });
                }

                throw;
            }
        }

        // PUT: api/Shelters/5/status
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ShelterResponseDto>> UpdateStatus(int id, [FromBody] ShelterStatusDto request)
        {
            var status = NormalizeStatus(request?.Status);
            if (status == null)
            {
                return BadRequest(new { message = $"Invalid status. Allowed values: {string.Join(", ", AllowedStatuses)}." });
            }

            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null)
            {
                return NotFound(new { message = "Shelter not found." });
            }

            shelter.Status = status;
            await _context.SaveChangesAsync();
            return Ok(MapToResponse(shelter));
        }

        // POST: api/Shelters/5/occupants
        [HttpPost("{id:int}/occupants")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ShelterResponseDto>> AssignOccupants(
            int id, [FromBody] ShelterAssignmentDto request)
        {
            if (request == null || request.PersonCount <= 0)
            {
                return BadRequest(new { message = "A positive person count is required." });
            }

            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null)
            {
                return NotFound(new { message = "Shelter not found." });
            }

            if (string.Equals(shelter.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = "Closed shelters cannot accept assignments." });
            }

            var availableCapacity = shelter.Capacity - shelter.CurrentOccupancy;
            if (request.PersonCount > availableCapacity)
            {
                return BadRequest(new
                {
                    message = $"Assignment exceeds available capacity ({availableCapacity} slots)."
                });
            }

            var occupant = new ShelterOccupant
            {
                ShelterId = id,
                HouseholdName = string.IsNullOrWhiteSpace(request.HouseholdName)
                    ? $"Household {id}-{Guid.NewGuid().ToString("N")[..6]}"
                    : request.HouseholdName.Trim(),
                PersonCount = request.PersonCount
            };

            _context.ShelterOccupants.Add(occupant);
            shelter.CurrentOccupancy += request.PersonCount;
            shelter.Status = shelter.CurrentOccupancy >= shelter.Capacity ? "Full" : "Open";

            await _context.SaveChangesAsync();
            return Ok(MapToResponse(shelter));
        }

        // DELETE: api/Shelters/5/occupants/7
        [HttpDelete("{id:int}/occupants/{occupantId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ShelterResponseDto>> RemoveOccupants(int id, int occupantId)
        {
            var occupant = await _context.ShelterOccupants
                .FirstOrDefaultAsync(o => o.Id == occupantId && o.ShelterId == id);
            if (occupant == null)
            {
                return NotFound(new { message = "Shelter assignment not found." });
            }

            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null)
            {
                return NotFound(new { message = "Shelter not found." });
            }

            _context.ShelterOccupants.Remove(occupant);
            shelter.CurrentOccupancy = Math.Max(0, shelter.CurrentOccupancy - occupant.PersonCount);
            if (!string.Equals(shelter.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                shelter.Status = "Open";
            }

            await _context.SaveChangesAsync();
            return Ok(MapToResponse(shelter));
        }

        // DELETE: api/Shelters/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteShelter(int id)
        {
            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null)
            {
                return NotFound(new { message = "Shelter not found." });
            }

            var occupants = await _context.ShelterOccupants.Where(o => o.ShelterId == id).ToListAsync();
            _context.ShelterOccupants.RemoveRange(occupants);

            var resources = await _context.Resources.Where(r => r.ShelterId == id).ToListAsync();
            foreach (var resource in resources)
            {
                resource.ShelterId = null;
            }

            var volunteers = await _context.Volunteers.Where(v => v.ShelterId == id).ToListAsync();
            foreach (var volunteer in volunteers)
            {
                volunteer.ShelterId = null;
            }

            _context.Shelters.Remove(shelter);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shelter deleted successfully." });
        }

        private async Task<bool> CrisisExists(int? crisisEventId)
        {
            return !crisisEventId.HasValue
                   || await _context.CrisisEvents.AnyAsync(c => c.Id == crisisEventId.Value);
        }

        private async Task<bool> ShelterExists(int id)
        {
            return await _context.Shelters.AnyAsync(s => s.Id == id);
        }

        private static string? NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            return AllowedStatuses.FirstOrDefault(s =>
                string.Equals(s, status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static ShelterResponseDto MapToResponse(Shelter shelter)
        {
            return new ShelterResponseDto
            {
                Id = shelter.Id,
                Name = shelter.Name,
                Location = shelter.Location,
                Capacity = shelter.Capacity,
                CurrentOccupancy = shelter.CurrentOccupancy,
                AvailableCapacity = Math.Max(0, shelter.Capacity - shelter.CurrentOccupancy),
                Status = shelter.Status,
                CrisisEventId = shelter.CrisisEventId
            };
        }
    }
}
