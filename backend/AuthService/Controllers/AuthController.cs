using AuthService.Data;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthController(AuthDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Email atau password salah." });

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            RefreshToken = Guid.NewGuid().ToString(),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Department = user.Department
            }
        });
    }

    // POST /api/auth/register
    [HttpPost("register")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new { message = "Email sudah terdaftar." });

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Department = request.Department,
            Role = request.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "User berhasil dibuat.", userId = user.Id });
    }

    // GET /api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        if (userId == null) return Unauthorized();

        var user = await _db.Users.FindAsync(Guid.Parse(userId));
        if (user == null) return NotFound();

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            Department = user.Department
        });
    }

    // GET /api/auth/users  (SuperAdmin only)
    [HttpGet("users")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _db.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                Department = u.Department
            }).ToListAsync();

        return Ok(users);
    }
}