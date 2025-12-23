using FalconTouch.Application.Auth;
using FalconTouch.Application.Common;
using FalconTouch.Domain.Entities;
using FalconTouch.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FalconTouch.Infrastructure.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _config;

        public AuthService(IUserRepository userRepository, IConfiguration config)
        {
            _userRepository = userRepository;
            _config = config;
        }

        public async Task<Result<AuthResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user is null)
                return Result<AuthResult>.Fail("Usuario inexistente.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Result<AuthResult>.Fail("Not authorized user.");

            var token = GenerateJwt(user);

            var result = new AuthResult(Success: true, Token: token, Error: null);

            return Result<AuthResult>.Ok(result);
        }

        public async Task<Result<AuthResult>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            if (await _userRepository.UserExistsAsync(request.Email))
                return Result<AuthResult>.Fail("Email já cadastrado.");

            var cpf = new string(request.CPF.Where(Char.IsDigit).ToArray());

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                CPF = cpf,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "Player"
            };

            await _userRepository.AddUserAsync(user);

            var token = GenerateJwt(user);

            var result = new AuthResult(Success: true, Token: token, Error: null);

            return Result<AuthResult>.Ok(result);
        }

        private string GenerateJwt(User user)
        {
            var jwtKey = _config["Jwt:Key"]!;
            var issuer = _config["Jwt:Issuer"]!;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new("id", user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new("name", user.Name),
                new("cpf", user.CPF),
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
}