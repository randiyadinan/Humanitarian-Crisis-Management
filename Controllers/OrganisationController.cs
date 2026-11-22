using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Data;
using HumanitarianAPI.Models;

namespace HumanitarianAPI.Controllers
{
[Route("api/[controller]")]
[ApiController]
public class OrganisationController : ControllerBase
{
private readonly ApplicationDbContext _context;

    public OrganisationController(ApplicationDbContext context)
    {
        _context = context;
    }
    // GET: api/Organisation
    // Get all organisations
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Organisation>>> GetOrganisations()
    {
        var organisations = await _context.Organisations.ToListAsync();
        return Ok(organisations);
    }
    // GET: api/Organisation/5
    // Get organisation by ID
    [HttpGet("{id}")]
    public async Task<ActionResult<Organisation>> GetOrganisation(int id)
    {
        var organisation = await _context.Organisations.FindAsync(id);
        if (organisation == null)
        {
            return NotFound(new
            {
                message = "Organisation not found."
            });
        }
        return Ok(organisation);
    }
    // POST: api/Organisation
    // Create a new organisation
    [HttpPost]
    public async Task<ActionResult<Organisation>> CreateOrganisation(
        [FromBody] Organisation organisation)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        if (string.IsNullOrWhiteSpace(organisation.Name) ||
            string.IsNullOrWhiteSpace(organisation.Sector) ||
            string.IsNullOrWhiteSpace(organisation.ContactPerson) ||
            string.IsNullOrWhiteSpace(organisation.Phone))
        {
            return BadRequest(new
            {
                message = "All fields are required."
            });
        }
        organisation.Name = organisation.Name.Trim();
        organisation.Sector = organisation.Sector.Trim();
        organisation.ContactPerson = organisation.ContactPerson.Trim();
        organisation.Phone = organisation.Phone.Trim();
        _context.Organisations.Add(organisation);
        await _context.SaveChangesAsync();
        return CreatedAtAction(
            nameof(GetOrganisation),
            new { id = organisation.Id },
            organisation
        );
    }
    // PUT: api/Organisation/5
    // Update an existing organisation
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrganisation(
        int id,
        [FromBody] Organisation organisation)
    {
        if (id != organisation.Id)
        {
            return BadRequest(new
            {
                message = "Organisation ID mismatch."
            });
        }
        if (string.IsNullOrWhiteSpace(organisation.Name) ||
            string.IsNullOrWhiteSpace(organisation.Sector) ||
            string.IsNullOrWhiteSpace(organisation.ContactPerson) ||
            string.IsNullOrWhiteSpace(organisation.Phone))
        {
            return BadRequest(new
            {
                message = "All fields are required."
            });
        }
        var existingOrganisation =
            await _context.Organisations.FindAsync(id);
        if (existingOrganisation == null)
        {
            return NotFound(new
            {
                message = "Organisation not found."
            });
        }
        existingOrganisation.Name = organisation.Name.Trim();
        existingOrganisation.Sector = organisation.Sector.Trim();
        existingOrganisation.ContactPerson = organisation.ContactPerson.Trim();
        existingOrganisation.Phone = organisation.Phone.Trim();
        await _context.SaveChangesAsync();
        return Ok(new
        {
            message = "Organisation updated successfully.",
            organisation = existingOrganisation
        });
    }
    // DELETE: api/Organisation/5
    // Delete an organisation
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrganisation(int id)
    {
        var organisation =
            await _context.Organisations.FindAsync(id);
        if (organisation == null)
        {
            return NotFound(new
            {
                message = "Organisation not found."
            });
        }
        _context.Organisations.Remove(organisation);
        await _context.SaveChangesAsync();
        return Ok(new
        {
            message = "Organisation deleted successfully."
        });
    }
}

}