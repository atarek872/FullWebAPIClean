using Application.Common.Interfaces;
using Domain.Authorization;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;

public class TokenService : ITokenService
{
    private const string RefreshTokenProvider = "FullWebAPI";
    private const string RefreshTokenName = "RefreshToken";
    private const string RefreshTokenExpiryName = "RefreshTokenExpiry";

    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public TokenService(IConfiguration configuration, UserManager<User> userManager, RoleManager<Role> roleManager, ApplicationDbContext dbContext)
    {
        _configuration = configuration;
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public async Task<string> GenerateAccessTokenAsync(User user, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var hasMembership = await _dbContext.UserTenantMemberships.AnyAsync(x => x.UserId == user.Id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);
        if (!hasMembership)
        {
            throw new InvalidOperationException("User is not mapped to tenant.");
        }

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");
        var expiryMinutesStr = jwtSettings["ExpiryMinutes"] ?? throw new InvalidOperationException("JWT ExpiryMinutes is not configured.");

        var roles = await _userManager.GetRolesAsync(user);
        var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role));
        var permissionClaims = new List<Claim>();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;
            var claims = await _roleManager.GetClaimsAsync(role);
            permissionClaims.AddRange(claims.Where(claim => claim.Type == PermissionConstants.PermissionClaimType));
        }

        var membership = await _dbContext.UserTenantMemberships.FirstAsync(x => x.UserId == user.Id && x.TenantId == tenantId, cancellationToken);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new("tenant_id", tenantId.ToString()),
            new("tenant_role", membership.Role),
            new("tenant_permissions", membership.PermissionsCsv),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roleClaims);
        claims.AddRange(permissionClaims.DistinctBy(claim => $"{claim.Type}:{claim.Value}", StringComparer.OrdinalIgnoreCase));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(expiryMinutesStr)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        var refreshToken = CreateSecureRandomToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, refreshToken);
        await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName, refreshTokenExpiry.ToString("O"));
        return refreshToken;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => await GetUserFromRefreshTokenAsync(refreshToken, cancellationToken) is not null;

    public async Task<User?> GetUserFromRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        foreach (var user in _userManager.Users.Where(u => !u.IsDeleted && u.IsActive))
        {
            var storedToken = await _userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
            if (!string.Equals(storedToken, refreshToken, StringComparison.Ordinal)) continue;
            var expiryRaw = await _userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);
            if (!DateTime.TryParse(expiryRaw, out var expiry) || expiry <= DateTime.UtcNow) return null;
            return user;
        }

        return null;
    }

    public async Task<(string AccessToken, string RefreshToken)> GenerateTokenPairAsync(User user, Guid tenantId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var accessToken = await GenerateAccessTokenAsync(user, tenantId, cancellationToken);
        var refreshToken = await GenerateRefreshTokenAsync(user, cancellationToken);
        return (accessToken, refreshToken);
    }

    public async Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken, Guid tenantId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await GetUserFromRefreshTokenAsync(refreshToken, cancellationToken);
        if (user is null) return null;
        return await GenerateTokenPairAsync(user, tenantId, ipAddress, cancellationToken);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await GetUserFromRefreshTokenAsync(refreshToken, cancellationToken);
        if (user is null) return false;
        await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);
        return true;
    }

    public async Task<int> RevokeAllRefreshTokensAsync(Guid userId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return 0;
        await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);
        return 1;
    }

    private static string CreateSecureRandomToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
