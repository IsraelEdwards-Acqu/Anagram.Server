using Anagram.Server.Data;
using Anagram.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] UserDto userDto, [FromServices] AnagramDbContext db)
    {
        if (db.Users.Any(u => u.Email == userDto.Email))
            return BadRequest("Email already exists");

        var hashed = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
        var user = new User { Email = userDto.Email, PasswordHash = hashed, Name = userDto.Email };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserDto userDto, [FromServices] AnagramDbContext db)
    {
        var user = db.Users.SingleOrDefault(u => u.Email == userDto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(userDto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        var token = GenerateJwtToken(user.Email);
        return Ok(new { token });
    }
    private string GenerateJwtToken(string email)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class UserDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}
