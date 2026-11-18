using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Models;
using HumanitarianAPI.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HumanitarianAPI.Controllers
{
    [Route("api/Donations")]
    [ApiController]
    public class DonationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DonationController(ApplicationDbContext context) 
        { 
            _context = context; 
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Donation>>> GetDonations() 
        { 
            return await _context.Donations.ToListAsync(); 
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Donation>> GetDonation(int id)
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation == null) return NotFound(new { message = "Donation not found" });
            return Ok(donation);
        }

        [HttpPost]
        public async Task<ActionResult<Donation>> CreateDonation([FromBody] Donation donation)
        {
            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();
            return Ok(donation);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutDonation(int id, [FromBody] Donation donation)
        {
            if (id != donation.Id) return BadRequest(new { message = "Donation ID mismatch" });

            var existingDonation = await _context.Donations.FindAsync(id);
            if (existingDonation == null) return NotFound(new { message = "Donation not found" });

            existingDonation.DonorName = donation.DonorName;
            existingDonation.DonationType = donation.DonationType;
            existingDonation.Amount = donation.Amount;
            existingDonation.Description = donation.Description;
            existingDonation.Date = donation.Date;
            existingDonation.CrisisEventId = donation.CrisisEventId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Donation updated successfully!", donation = existingDonation });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDonation(int id)
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation == null) return NotFound(new { message = "Donation not found" });

            _context.Donations.Remove(donation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Donation deleted successfully!" });
        }
    }
}
