namespace FalconTouch.Application.Auth;

public record LoginResult(
    string Token,
    int UserId,
    string Email,
    string Role
);
