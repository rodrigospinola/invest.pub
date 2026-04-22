namespace Invest.Application.Responses;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserResponse User
);
