using Microsoft.AspNetCore.Mvc;
using HumanitarianAPI.Data;
using HumanitarianAPI.Models;

namespace HumanitarianAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. පද්ධතියේ සිටින සියලුම Users ලා බැලීමට (Frontend Validation සඳහා)
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = _context.Users.ToList();
            return Ok(users);
        }

        // 2. අලුත් User කෙනෙක් Register කිරීම
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            // Role එකක් දීලා නැත්නම් Default එක "Citizen" ලෙස සකසයි
            if (string.IsNullOrEmpty(user.Role))
            {
                user.Role = "Citizen";
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User registered successfully", userId = user.Id });
        }

        // 3. User Login වීම
        [HttpPost("login")]
        public IActionResult Login([FromBody] User loginDto)
        {
            // Email එක සහ Password එක ගැලපෙනවාදැයි ඩේටබේස් එකෙන් බැලීම
            var user = _context.Users.FirstOrDefault(u => u.Email == loginDto.Email && u.PasswordHash == loginDto.PasswordHash);
            
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid credentials or account not found" });
            }
            
            return Ok(new { message = "Login successful", role = user.Role, username = user.Username });
        }
    }
}
