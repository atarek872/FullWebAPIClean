using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default);
    Task<string> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<User?> GetUserFromRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}