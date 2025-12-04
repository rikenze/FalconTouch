using FalconTouch.Application.Common;

namespace FalconTouch.Application.Auth;

public interface IAuthService
{
    Task<Result> RegisterAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<Result<LoginResult>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}
