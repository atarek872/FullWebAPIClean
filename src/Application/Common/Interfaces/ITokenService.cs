using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default);
    Task<string> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<User?> GetUserFromRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<(string AccessToken, string RefreshToken)> GenerateTokenPairAsync(User user, string ipAddress, CancellationToken cancellationToken = default);
    Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default);
    Task<int> RevokeAllRefreshTokensAsync(Guid userId, string ipAddress, CancellationToken cancellationToken = default);
}
