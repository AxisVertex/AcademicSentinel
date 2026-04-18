using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcademicSentinel.Server.Data;
using AcademicSentinel.Server.Models;
using AcademicSentinel.Server.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AcademicSentinel.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration; // Add this

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserResponseDto>> Register([FromBody] UserRegisterDto request)
    {
        // 1. Check if email exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest("A user with this email already exists.");
        }

        // 2. Hash the password using BCrypt
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 3. Create the real User model to save to the database
        var newUser = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // 4. Return the safe DTO (no password hash exposed)
        return Ok(new UserResponseDto
        {
            Id = newUser.Id,
            Email = newUser.Email,
            Role = newUser.Role
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserResponseDto>> Login([FromBody] UserLoginDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid email or password.");
        }

        // --- GENERATE JWT TOKEN ---
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role) // This sets "Student" or "Instructor"
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8), // Token lasts for 8 hours
            signingCredentials: creds
        );

        var jwtString = new JwtSecurityTokenHandler().WriteToken(token);
        // --------------------------

        return Ok(new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            Token = jwtString // Send the token back to the app!
        });
    }

    /// <summary>
    /// Development-only endpoint to initialize test data
    /// Creates default test user for integration testing
    /// </summary>
    [HttpPost("seed-test-user")]
    public async Task<ActionResult<string>> SeedTestUser()
    {
        // Check if test user already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "student@example.com");

        if (existingUser != null)
        {
            return Ok($"Test user already exists with ID: {existingUser.Id}");
        }

        // Create test user
        var testUser = new User
        {
            Email = "student@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePass123!"),
            Role = "Student",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        return Ok($"✅ Test user created successfully - ID: {testUser.Id}, Email: student@example.com");
    }

    /// <summary>
    /// Creates a test exam room for integration testing
    /// </summary>
    [HttpPost("seed-test-room")]
    public async Task<ActionResult<string>> SeedTestRoom()
    {
        // Check if test room already exists
        var existingRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.SubjectName == "Test Exam Room");

        if (existingRoom != null)
        {
            return Ok($"Test room already exists with ID: {existingRoom.Id}");
        }

        // Create test room
        var testRoom = new Room
        {
            SubjectName = "Test Exam Room",
            InstructorId = 1, // Assuming instructor with ID 1 exists, or default
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        _context.Rooms.Add(testRoom);
        await _context.SaveChangesAsync();

        return Ok($"✅ Test room created successfully - ID: {testRoom.Id}, SubjectName: Test Exam Room, Status: Active");
    }
}