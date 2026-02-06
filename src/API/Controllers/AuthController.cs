using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var rolesToAssign = request.Roles.Count > 0 ? request.Roles : ["Guest"];
        var roleResult = await _userManager.AddToRolesAsync(user, rolesToAssign);
        if (!roleResult.Succeeded)
        {
            return BadRequest(roleResult.Errors);
        }

        return Ok(new { Message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return Unauthorized("Invalid credentials");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded)
        {
            return Unauthorized("Invalid credentials");
        }

        var tokenPair = await _tokenService.GenerateTokenPairAsync(user, GetClientIpAddress(), cancellationToken);

        return Ok(new
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            User = new { user.Id, user.Email, user.FirstName, user.LastName }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenPair = await _tokenService.RefreshTokenAsync(request.RefreshToken, GetClientIpAddress(), cancellationToken);
        if (tokenPair is null)
        {
            return Unauthorized("Invalid or expired refresh token");
        }

        return Ok(new
        {
            AccessToken = tokenPair.Value.AccessToken,
            RefreshToken = tokenPair.Value.RefreshToken
        });
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        var revoked = await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, GetClientIpAddress(), cancellationToken);
        if (!revoked)
        {
            return NotFound("Refresh token not found");
        }

        return Ok(new { Message = "Refresh token revoked" });
    }

    [Authorize]
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return Unauthorized("Invalid access token");
        }

        await _tokenService.RevokeAllRefreshTokensAsync(userId, GetClientIpAddress(), cancellationToken);
        return Ok(new { Message = "All refresh tokens revoked" });
    }

    [Authorize(Policy = "ManageUsers")]
    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return NotFound("User not found");
        }

        var result = await _userManager.AddToRoleAsync(user, request.RoleName);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { Message = "Role assigned successfully" });
    }

    private string GetClientIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RevokeTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class AssignRoleRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string RoleName { get; set; } = string.Empty;
}
