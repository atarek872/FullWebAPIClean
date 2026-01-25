using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<string> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");
        var expiryMinutesStr = jwtSettings["ExpiryMinutes"] ?? throw new InvalidOperationException("JWT ExpiryMinutes is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(double.Parse(expiryMinutesStr)),
            signingCredentials: creds
        );
        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    public Task<string> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Task.FromResult(Convert.ToBase64String(randomNumber));
    }

    public Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Implement validation logic
        return Task.FromResult(true);
    }

    public Task<User?> GetUserFromRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Implement logic to get user from refresh token
        return Task.FromResult<User?>(null);
    }
}