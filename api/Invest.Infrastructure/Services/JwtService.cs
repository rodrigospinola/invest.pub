using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Invest.Domain.Entities;
using Invest.Domain.Interfaces;

namespace Invest.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly string _secret;
    private readonly string _expiration;

    public JwtService(IConfiguration configuration)
    {
        _secret = configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET não configurado.");
        _expiration = configuration["JWT_EXPIRATION"] ?? "24h";
    }

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("nome", user.Nome),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var hours = ParseExpirationHours(_expiration);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var sub = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            return sub != null ? Guid.Parse(sub) : null;
        }
        catch (Exception)
        {
            // Token inválido ou malformado — não logar (CLAUDE.md: nunca logar tokens JWT)
            return null;
        }
    }

    public TimeSpan GetAccessTokenExpiration()
    {
        var hours = ParseExpirationHours(_expiration);
        return TimeSpan.FromHours(hours);
    }

    private static double ParseExpirationHours(string expiration)
    {
        if (expiration.EndsWith("h") && double.TryParse(expiration.TrimEnd('h'), out var hours))
            return hours;
        if (expiration.EndsWith("d") && double.TryParse(expiration.TrimEnd('d'), out var days))
            return days * 24;
        return 24;
    }
}
