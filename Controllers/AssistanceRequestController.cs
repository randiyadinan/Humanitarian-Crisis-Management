using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Models;
using HumanitarianAPI.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HumanitarianAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssistanceRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Database එක Controller එකට සම්බන්ධ කිරීම
        public AssistanceRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Database එකේ තියෙන සියලුම ඉල්ලීම් ලබාගන්නා Endpoint එක (GET)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AssistanceRequest>>> GetRequests()
        {
            return await _context.AssistanceRequests.ToListAsync();
        }

        // 2. අලුත් ඉල්ලීමක් Database එකට ඇතුළත් කරන Endpoint එක (POST) - AI Logic සමඟින්
        [HttpPost]
        public async Task<ActionResult<AssistanceRequest>> CreateRequest([FromBody] AssistanceRequest newRequest)
        {
            // AI Decision-Support Logic: ස්වයංක්‍රීයව හදිසි අවස්ථා හඳුනා ගැනීම
            if (!string.IsNullOrEmpty(newRequest.Description))
            {
                string descLower = newRequest.Description.ToLower();
                if (descLower.Contains("urgent") || descLower.Contains("severe") || descLower.Contains("emergency"))
                {
                    newRequest.Urgency = "High";
                }
            }

            // අලුතින් එන ඉල්ලීම් වල Status එක සාමාන්‍යයෙන් "Pending" විය යුතුය
            if (string.IsNullOrEmpty(newRequest.Status))
            {
                newRequest.Status = "Pending";
            }

            _context.AssistanceRequests.Add(newRequest);
            await _context.SaveChangesAsync(); // දත්ත ස්ථිරවම Database එකේ Save කිරීම
            
            return Ok(newRequest);
        }

        // 3. Admin විසින් ඉල්ලීමක තත්ත්වය (Status/Urgency) Update කරන Endpoint එක (PUT)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRequest(int id, [FromBody] AssistanceRequest updatedRequest)
        {
            // URL එකේ ID එකයි, එවන දත්ත වල ID එකයි සමානදැයි බැලීම
            if (id != updatedRequest.Id)
            {
                return BadRequest(new { message = "Request ID mismatch" });
            }

            // ඩේටබේස් එකෙන් අදාළ ඉල්ලීම සෙවීම
            var existingRequest = await _context.AssistanceRequests.FindAsync(id);
            if (existingRequest == null)
            {
                return NotFound(new { message = "Assistance request not found" });
            }

            // Admin විසින් වෙනස් කළ දත්ත (Status සහ Urgency) යාවත්කාලීන කිරීම
            existingRequest.Urgency = updatedRequest.Urgency;
            existingRequest.Status = updatedRequest.Status;
            
            // අවශ්‍ය නම් අනිත් ඒවත් Update වීමට
            existingRequest.Category = updatedRequest.Category;
            existingRequest.Description = updatedRequest.Description;
            existingRequest.Location = updatedRequest.Location;

            try
            {
                // වෙනස්කම් ඩේටබේස් එකට සේව් කිරීම
                await _context.SaveChangesAsync();
                return Ok(new { message = "Request updated successfully!", request = existingRequest });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RequestExists(id))
                {
                    return NotFound(new { message = "Assistance request not found during update" });
                }
                else
                {
                    throw;
                }
            }
        }

        // 4. Admin විසින් ඉල්ලීමක් ඩේටබේස් එකෙන් ඉවත් කරන Endpoint එක (DELETE)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.AssistanceRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound(new { message = "Assistance request not found" });
            }

            _context.AssistanceRequests.Remove(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Request deleted successfully!" });
        }

        // ID එකක් ඩේටබේස් එකේ තියෙනවාදැයි පරීක්ෂා කරන කුඩා Function එකක්
        private bool RequestExists(int id)
        {
            return _context.AssistanceRequests.Any(e => e.Id == id);
        }
    }
}
