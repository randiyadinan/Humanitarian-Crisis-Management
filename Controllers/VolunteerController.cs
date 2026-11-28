using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Models;
using HumanitarianAPI.Data;

namespace HumanitarianAPI.Controllers
{
    [Authorize]
    [Route("api/Volunteers")]
    [ApiController]
    public class VolunteerController : ControllerBase
    {
        private static readonly string[] AllowedStatuses = { "Available", "Deployed", "Unavailable" };

        private readonly ApplicationDbContext _context;

        public VolunteerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Volunteers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VolunteerResponseDto>>> GetVolunteers()
        {
            var volunteers = await _context.Volunteers
                .AsNoTracking()
                .OrderBy(v => v.Id)
                .ToListAsync();

            return Ok(volunteers.Select(MapToResponse).ToList());
        }

        // GET: api/Volunteers/available?skill=First%20Aid&specialisation=Medical
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<VolunteerResponseDto>>> GetAvailableVolunteers(
            [FromQuery] string? skill = null,
            [FromQuery] string? specialisation = null)
        {
            var volunteers = await _context.Volunteers
                .AsNoTracking()
                .Where(v => string.Equals(v.Status, "Available", StringComparison.OrdinalIgnoreCase))
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(skill))
            {
                var requestedSkill = skill.Trim();
                volunteers = volunteers.Where(v =>
                    v.Skills.Contains(requestedSkill, StringComparison.OrdinalIgnoreCase)
                    || v.Specialisation.Contains(requestedSkill, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(specialisation))
            {
                var requestedSpecialisation = specialisation.Trim();
                volunteers = volunteers.Where(v =>
                    v.Specialisation.Contains(requestedSpecialisation, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Ok(volunteers.Select(MapToResponse).ToList());
        }

        // GET: api/Volunteers/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<VolunteerResponseDto>> GetVolunteer(int id)
        {
            var volunteer = await _context.Volunteers.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            if (volunteer == null)
            {
                return NotFound(new { message = "Volunteer not found." });
            }

            return Ok(MapToResponse(volunteer));
        }

        // POST: api/Volunteers (authenticated citizens may register themselves)
        [HttpPost]
        public async Task<ActionResult<VolunteerResponseDto>> CreateVolunteer([FromBody] VolunteerCreateDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState.IsValid ? new { message = "Invalid volunteer details." } : ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Volunteer name is required." });
            }

            if (!IsValidContactNumber(request.ContactNumber))
            {
                return BadRequest(new { message = "A valid contact number is required." });
            }

            var status = NormalizeStatus(request.Status) ?? "Available";
            if (status == "Deployed" && !request.CrisisEventId.HasValue)
            {
                return BadRequest(new { message = "A deployed volunteer must be linked to a crisis event." });
            }

            if (!await ReferencesExist(request.CrisisEventId, request.ShelterId))
            {
                return BadRequest(new { message = "The specified crisis event or shelter does not exist." });
            }

            var volunteer = new Volunteer
            {
                Name = request.Name.Trim(),
                Skills = string.IsNullOrWhiteSpace(request.Skills) ? "General Relief" : request.Skills.Trim(),
                Specialisation = string.IsNullOrWhiteSpace(request.Specialisation)
                    ? "General Relief"
                    : request.Specialisation.Trim(),
                ContactNumber = request.ContactNumber.Trim(),
                Status = status,
                CrisisEventId = request.CrisisEventId,
                ShelterId = request.ShelterId
            };

            _context.Volunteers.Add(volunteer);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "An error occurred while saving the volunteer." });
            }

            return CreatedAtAction(nameof(GetVolunteer), new { id = volunteer.Id }, MapToResponse(volunteer));
        }

        // PUT: api/Volunteers/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateVolunteer(int id, [FromBody] VolunteerUpdateDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState.IsValid ? new { message = "Invalid volunteer details." } : ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Volunteer name is required." });
            }

            if (!IsValidContactNumber(request.ContactNumber))
            {
                return BadRequest(new { message = "A valid contact number is required." });
            }

            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null)
            {
                return NotFound(new { message = "Volunteer not found." });
            }

            var status = string.IsNullOrWhiteSpace(request.Status)
                ? volunteer.Status
                : NormalizeStatus(request.Status);

            if (status == null)
            {
                return BadRequest(new { message = $"Invalid status. Allowed values: {string.Join(", ", AllowedStatuses)}." });
            }

            if (status == "Deployed" && !request.CrisisEventId.HasValue)
            {
                return BadRequest(new { message = "A deployed volunteer must be linked to a crisis event." });
            }

            if (!await ReferencesExist(request.CrisisEventId, request.ShelterId))
            {
                return BadRequest(new { message = "The specified crisis event or shelter does not exist." });
            }

            volunteer.Name = request.Name.Trim();
            volunteer.Skills = string.IsNullOrWhiteSpace(request.Skills) ? volunteer.Skills : request.Skills.Trim();
            volunteer.Specialisation = string.IsNullOrWhiteSpace(request.Specialisation)
                ? volunteer.Specialisation
                : request.Specialisation.Trim();
            volunteer.ContactNumber = request.ContactNumber.Trim();
            volunteer.Status = status;
            volunteer.CrisisEventId = request.CrisisEventId;
            volunteer.ShelterId = request.ShelterId;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Volunteer updated successfully.", volunteer = MapToResponse(volunteer) });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await VolunteerExists(id))
                {
                    return NotFound(new { message = "Volunteer not found during update." });
                }

                throw;
            }
        }

        // PUT: api/Volunteers/5/availability
        [HttpPut("{id:int}/availability")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<VolunteerResponseDto>> UpdateAvailability(
            int id, [FromBody] VolunteerAvailabilityDto request)
        {
            var status = NormalizeStatus(request?.Status);
            if (status == null)
            {
                return BadRequest(new { message = $"Invalid status. Allowed values: {string.Join(", ", AllowedStatuses)}." });
            }

            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null)
            {
                return NotFound(new { message = "Volunteer not found." });
            }

            if (status == "Deployed" && !volunteer.CrisisEventId.HasValue)
            {
                return BadRequest(new { message = "Assign a crisis event before deploying this volunteer." });
            }

            volunteer.Status = status;
            await _context.SaveChangesAsync();
            return Ok(MapToResponse(volunteer));
        }

        // POST: api/Volunteers/5/assign
        [HttpPost("{id:int}/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<VolunteerResponseDto>> AssignVolunteer(
            int id, [FromBody] VolunteerAssignmentDto request)
        {
            if (request == null || !ModelState.IsValid || request.CrisisEventId <= 0)
            {
                return BadRequest(new { message = "A valid crisis event is required for assignment." });
            }

            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null)
            {
                return NotFound(new { message = "Volunteer not found." });
            }

            if (string.Equals(volunteer.Status, "Unavailable", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = "Unavailable volunteers cannot be deployed." });
            }

            if (!await ReferencesExist(request.CrisisEventId, request.ShelterId))
            {
                return BadRequest(new { message = "The specified crisis event or shelter does not exist." });
            }

            volunteer.CrisisEventId = request.CrisisEventId;
            if (request.ShelterId.HasValue)
            {
                volunteer.ShelterId = request.ShelterId;
            }

            volunteer.AssignedTask = string.IsNullOrWhiteSpace(request.Task)
                ? "Field deployment"
                : request.Task.Trim();
            volunteer.Status = "Deployed";

            await _context.SaveChangesAsync();
            return Ok(MapToResponse(volunteer));
        }

        // POST: api/Volunteers/5/release
        [HttpPost("{id:int}/release")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<VolunteerResponseDto>> ReleaseVolunteer(int id)
        {
            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null)
            {
                return NotFound(new { message = "Volunteer not found." });
            }

            if (!volunteer.CrisisEventId.HasValue && string.IsNullOrWhiteSpace(volunteer.AssignedTask))
            {
                return BadRequest(new { message = "This volunteer is not currently deployed." });
            }

            volunteer.CrisisEventId = null;
            volunteer.ShelterId = null;
            volunteer.AssignedTask = string.Empty;
            volunteer.Status = "Available";

            await _context.SaveChangesAsync();
            return Ok(MapToResponse(volunteer));
        }

        // DELETE: api/Volunteers/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVolunteer(int id)
        {
            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null)
            {
                return NotFound(new { message = "Volunteer not found." });
            }

            _context.Volunteers.Remove(volunteer);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Volunteer deleted successfully." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the volunteer." });
            }
        }

        private async Task<bool> ReferencesExist(int? crisisEventId, int? shelterId)
        {
            if (crisisEventId.HasValue && !await _context.CrisisEvents.AnyAsync(c => c.Id == crisisEventId.Value))
            {
                return false;
            }

            if (shelterId.HasValue && !await _context.Shelters.AnyAsync(s => s.Id == shelterId.Value))
            {
                return false;
            }

            return true;
        }

        private async Task<bool> VolunteerExists(int id)
        {
            return await _context.Volunteers.AnyAsync(v => v.Id == id);
        }

        private static bool IsValidContactNumber(string? contactNumber)
        {
            if (string.IsNullOrWhiteSpace(contactNumber))
            {
                return false;
            }

            var value = contactNumber.Trim();
            return value.Length is >= 7 and <= 20
                   && value.All(c => char.IsDigit(c) || c is '+' or '-' or ' ' or '(' or ')');
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

        private static VolunteerResponseDto MapToResponse(Volunteer volunteer)
        {
            return new VolunteerResponseDto
            {
                Id = volunteer.Id,
                Name = volunteer.Name,
                Skills = volunteer.Skills,
                Specialisation = volunteer.Specialisation,
                ContactNumber = volunteer.ContactNumber,
                AssignedTask = volunteer.AssignedTask,
                Status = volunteer.Status,
                CrisisEventId = volunteer.CrisisEventId,
                ShelterId = volunteer.ShelterId
            };
        }
    }
}
