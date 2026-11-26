using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Data;
using HumanitarianAPI.Models;
using HumanitarianAPI.Services;
using System.ComponentModel.DataAnnotations;

namespace HumanitarianAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly JwtService _jwtService;

        public AuthController(
            ApplicationDbContext context,
            PasswordService passwordService,
            JwtService jwtService)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
        }

        // 1. Get All Users (Admin Only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        // 2. Register User
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "All fields are required."
                });
            }

            if (request.Password.Length < 8 ||
                !request.Password.Any(char.IsUpper) ||
                !request.Password.Any(char.IsLower) ||
                !request.Password.Any(char.IsDigit))
            {
                return BadRequest(new
                {
                    message = "Password must contain at least 8 characters, including an uppercase letter, a lowercase letter, and a number."
                });
            }

            var email = request.Email.Trim().ToLowerInvariant();

            var existingUser = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email);

            if (existingUser)
            {
                return Conflict(new
                {
                    message = "Email is already registered."
                });
            }

            var user = new User
            {
                Username = request.Username.Trim(),
                Email = email,

                // New users always receive Citizen role
                Role = "Citizen"
            };

            // Hash password before saving
            user.PasswordHash = _passwordService.HashPassword(
                user,
                request.Password
            );

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User registered successfully.",
                userId = user.Id,
                username = user.Username,
                email = user.Email,
                role = user.Role
            });
        }

        // 3. Login User
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Email and password are required."
                });
            }

            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            // Verify password
            if (user == null ||
                !_passwordService.VerifyPassword(
                    user,
                    request.Password))
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // Generate JWT Token
            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                message = "Login successful.",

                token = token,

                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Role
                }
            });
        }
    }

    // Registration DTO
    public class RegisterDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }

    // Login DTO
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}