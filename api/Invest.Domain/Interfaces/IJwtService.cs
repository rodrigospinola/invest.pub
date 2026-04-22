using Invest.Domain.Entities;

namespace Invest.Domain.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
    TimeSpan GetAccessTokenExpiration();
}
