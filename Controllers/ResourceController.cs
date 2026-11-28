using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Models;
using HumanitarianAPI.Data;

namespace HumanitarianAPI.Controllers
{
    [Authorize]
    [Route("api/Resources")]
    [ApiController]
    public class ResourceController : ControllerBase
    {
        private static readonly string[] AllowedStatuses =
            { "Available", "Reserved", "Dispatched", "Depleted", "Damaged" };

        private readonly ApplicationDbContext _context;

        public ResourceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Resources
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResourceResponseDto>>> GetResources()
        {
            var resources = await _context.Resources
                .AsNoTracking()
                .OrderBy(r => r.Id)
                .ToListAsync();

            return Ok(resources.Select(MapToResponse).ToList());
        }

        // GET: api/Resources/available?category=Water&minQuantity=5
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<ResourceResponseDto>>> GetAvailableResources(
            [FromQuery] string? category = null,
            [FromQuery] int minQuantity = 1)
        {
            if (minQuantity < 0)
            {
                return BadRequest(new { message = "Minimum quantity cannot be negative." });
            }

            var query = _context.Resources
                .AsNoTracking()
                .Where(r => string.Equals(r.Status, "Available", StringComparison.OrdinalIgnoreCase)
                            && (r.Quantity - r.AllocatedQuantity) >= minQuantity);

            if (!string.IsNullOrWhiteSpace(category))
            {
                var requestedCategory = category.Trim();
                query = query.Where(r => string.Equals(r.Category, requestedCategory, StringComparison.OrdinalIgnoreCase));
            }

            var resources = await query.OrderBy(r => r.Id).ToListAsync();
            return Ok(resources.Select(MapToResponse).ToList());
        }

        // GET: api/Resources/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _context.Resources
                .AsNoTracking()
                .Select(r => r.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/Resources/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ResourceResponseDto>> GetResource(int id)
        {
            var resource = await _context.Resources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (resource == null)
            {
                return NotFound(new { message = "Resource not found." });
            }

            return Ok(MapToResponse(resource));
        }

        // POST: api/Resources
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResourceResponseDto>> CreateResource([FromBody] ResourceCreateDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState.IsValid ? new { message = "Invalid resource details." } : ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Location))
            {
                return BadRequest(new { message = "Resource name and location are required." });
            }

            if (request.Quantity < 0)
            {
                return BadRequest(new { message = "Quantity cannot be negative." });
            }

            var status = NormalizeStatus(request.Status);
            if (status == null)
            {
                return BadRequest(new { message = $"Invalid status. Allowed values: {string.Join(", ", AllowedStatuses)}." });
            }

            if (!await ReferencesExist(request.CrisisEventId, request.ShelterId))
            {
                return BadRequest(new { message = "The specified crisis event or shelter does not exist." });
            }

            var resource = new Resource
            {
                Name = request.Name.Trim(),
                Category = NormalizeCategory(request.Category),
                Quantity = request.Quantity,
                AllocatedQuantity = 0,
                Location = request.Location.Trim(),
                Status = status,
                CrisisEventId = request.CrisisEventId,
                ShelterId = request.ShelterId
            };

            _context.Resources.Add(resource);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "An error occurred while saving the resource." });
            }

            return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, MapToResponse(resource));
        }

        // PUT: api/Resources/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateResource(int id, [FromBody] ResourceUpdateDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState.IsValid ? new { message = "Invalid resource details." } : ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Location))
            {
                return BadRequest(new { message = "Resource name and location are required." });
            }

            if (request.Quantity < 0)
            {
                return BadRequest(new { message = "Quantity cannot be negative." });
            }

            var status = NormalizeStatus(request.Status);
            if (status == null)
            {
                return BadRequest(new { message = $"Invalid status. Allowed values: {string.Join(", ", AllowedStatuses)}." });
            }

            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { message = "Resource not found." });
            }

            if (request.Quantity < resource.AllocatedQuantity)
            {
                return BadRequest(new
                {
                    message = $"Quantity cannot be lower than the allocated quantity ({resource.AllocatedQuantity})."
                });
            }

            if (!await ReferencesExist(request.CrisisEventId, request.ShelterId))
            {
                return BadRequest(new { message = "The specified crisis event or shelter does not exist." });
            }

            resource.Name = request.Name.Trim();
            resource.Category = NormalizeCategory(request.Category);
            resource.Quantity = request.Quantity;
            resource.Location = request.Location.Trim();
            resource.Status = status;
            resource.CrisisEventId = request.CrisisEventId;
            resource.ShelterId = request.ShelterId;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Resource updated successfully.", resource = MapToResponse(resource) });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ResourceExists(id))
                {
                    return NotFound(new { message = "Resource not found during update." });
                }

                throw;
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "An error occurred while saving the resource." });
            }
        }

        // DELETE: api/Resources/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteResource(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { message = "Resource not found." });
            }

            if (resource.AllocatedQuantity > 0)
            {
                return Conflict(new
                {
                    message = "Release allocated units before deleting this resource."
                });
            }

            _context.Resources.Remove(resource);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Resource deleted successfully." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the resource." });
            }
        }

        // POST: api/Resources/5/allocate
        [HttpPost("{id:int}/allocate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResourceResponseDto>> AllocateResource(
            int id, [FromBody] ResourceAllocationDto request)
        {
            if (request == null || !ModelState.IsValid || request.Quantity <= 0)
            {
                return BadRequest(new { message = "A positive allocation quantity is required." });
            }

            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { message = "Resource not found." });
            }

            if (!string.Equals(resource.Status, "Available", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = "Only resources with status Available can be allocated." });
            }

            var availableQuantity = resource.Quantity - resource.AllocatedQuantity;
            if (request.Quantity > availableQuantity)
            {
                return BadRequest(new
                {
                    message = $"Requested quantity exceeds availability ({availableQuantity} units available)."
                });
            }

            if (!await ReferencesExist(request.CrisisEventId, request.ShelterId))
            {
                return BadRequest(new { message = "The specified crisis event or shelter does not exist." });
            }

            resource.AllocatedQuantity += request.Quantity;

            if (request.CrisisEventId.HasValue)
            {
                resource.CrisisEventId = request.CrisisEventId;
            }

            if (request.ShelterId.HasValue)
            {
                resource.ShelterId = request.ShelterId;
            }

            resource.Status = resource.AllocatedQuantity >= resource.Quantity ? "Dispatched" : "Available";

            try
            {
                await _context.SaveChangesAsync();
                return Ok(MapToResponse(resource));
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "An error occurred while allocating the resource." });
            }
        }

        // POST: api/Resources/5/release
        [HttpPost("{id:int}/release")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResourceResponseDto>> ReleaseResource(
            int id, [FromBody] ResourceReleaseDto request)
        {
            if (request == null || !ModelState.IsValid || request.Quantity <= 0)
            {
                return BadRequest(new { message = "A positive release quantity is required." });
            }

            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { message = "Resource not found." });
            }

            if (resource.AllocatedQuantity <= 0)
            {
                return BadRequest(new { message = "This resource has no allocated units to release." });
            }

            if (request.Quantity > resource.AllocatedQuantity)
            {
                return BadRequest(new
                {
                    message = $"Cannot release more than the allocated quantity ({resource.AllocatedQuantity})."
                });
            }

            resource.AllocatedQuantity -= request.Quantity;

            if (resource.AllocatedQuantity == 0)
            {
                resource.CrisisEventId = null;
                resource.ShelterId = null;
            }

            resource.Status = resource.Quantity == 0 ? "Depleted" : "Available";

            try
            {
                await _context.SaveChangesAsync();
                return Ok(MapToResponse(resource));
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "An error occurred while releasing the resource." });
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

        private async Task<bool> ResourceExists(int id)
        {
            return await _context.Resources.AnyAsync(r => r.Id == id);
        }

        private static string NormalizeCategory(string? category)
        {
            return string.IsNullOrWhiteSpace(category) ? "Uncategorised" : category.Trim();
        }

        private static string? NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "Available";
            }

            return AllowedStatuses.FirstOrDefault(s =>
                string.Equals(s, status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static ResourceResponseDto MapToResponse(Resource resource)
        {
            return new ResourceResponseDto
            {
                Id = resource.Id,
                Name = resource.Name,
                Category = resource.Category,
                Quantity = resource.Quantity,
                AllocatedQuantity = resource.AllocatedQuantity,
                AvailableQuantity = Math.Max(0, resource.Quantity - resource.AllocatedQuantity),
                Location = resource.Location,
                Status = resource.Status,
                CrisisEventId = resource.CrisisEventId,
                ShelterId = resource.ShelterId
            };
        }
    }
}
