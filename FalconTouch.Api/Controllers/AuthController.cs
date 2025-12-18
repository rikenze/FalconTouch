using FalconTouch.Domain.Entities;
using FalconTouch.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FalconTouch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IUserRepository _userRepository;

    public AuthController(IUserRepository userRepository, IConfiguration config)
    {
        _config = config;
        _userRepository = userRepository;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (await _userRepository.UserExistsAsync(request.Email))
            return BadRequest("Email já cadastrado.");

        var cpf = request.CPF
            .Replace(".", "")
            .Replace("-", "");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            CPF = cpf,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Player"
        };

        await _userRepository.AddUserAsync(user);

        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized();

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized();

        var token = GenerateJwt(user);
        return Ok(new { token });
    }

    private string GenerateJwt(User user)
    {
        var jwtKey = _config["Jwt:Key"]!;
        var issuer = _config["Jwt:Issuer"]!;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            issuer,
            claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record RegisterRequest(string Name, string Email, string CPF, string Password);
public record LoginRequest(string Email, string Password);
