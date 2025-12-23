using FalconTouch.Application.Auth;
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
    private readonly IAuthService _authService;

    public AuthController(IUserRepository userRepository, IConfiguration config, IAuthService authService)
    {
        _config = config;
        _userRepository = userRepository;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { token = result.Value });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { result.Value });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);
        if (user is null)
            return NotFound(new { message = "Usuário não encontrado." });

        var token = GeneratePasswordResetToken(user);

        // TODO: enviar email com o token. Por enquanto, apenas retorna OK.
        return Ok(new { message = "Email de recuperação enviado.", token });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Token e nova senha são obrigatórios." });

        try
        {
            var principal = ValidatePasswordResetToken(request.Token);
            var userIdClaim = principal.FindFirst("sub")?.Value ?? principal.FindFirst("id")?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return BadRequest(new { message = "Token inválido." });

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user is null)
                return NotFound(new { message = "Usuário não encontrado." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateUserAsync(user);

            return Ok(new { message = "Senha redefinida com sucesso." });
        }
        catch
        {
            return BadRequest(new { message = "Token inválido ou expirado." });
        }
    }

    private string GeneratePasswordResetToken(User user)
    {
        var jwtKey = _config["Jwt:Key"]!;
        var issuer = _config["Jwt:Issuer"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer,
            issuer,
            claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal ValidatePasswordResetToken(string token)
    {
        var jwtKey = _config["Jwt:Key"]!;
        var issuer = _config["Jwt:Issuer"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            IssuerSigningKey = key
        }, out _);

        return principal;
    }
}