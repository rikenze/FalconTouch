using FalconTouch.Application.Common;

namespace FalconTouch.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResult>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public record AuthResult(bool Success, string? Token = null, string? Error = null);
