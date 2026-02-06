using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Entities.Multitenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;
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
    private readonly ApplicationDbContext _dbContext;

    public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, ITokenService tokenService, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(x => x.Id == request.TenantId && !x.IsDeleted, cancellationToken);
        if (tenant is null)
        {
            return BadRequest("Invalid tenant");
        }

        var user = new User { UserName = request.Email, Email = request.Email, FirstName = request.FirstName, LastName = request.LastName, IsActive = true };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        var rolesToAssign = request.Roles.Count > 0 ? request.Roles : ["User"];
        var roleResult = await _userManager.AddToRolesAsync(user, rolesToAssign);
        if (!roleResult.Succeeded) return BadRequest(roleResult.Errors);

        _dbContext.UserTenantMemberships.Add(new UserTenantMembership
        {
            UserId = user.Id,
            TenantId = request.TenantId,
            Role = rolesToAssign.First(),
            PermissionsCsv = string.Join(',', rolesToAssign),
            IsDefault = true
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { Message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive) return Unauthorized("Invalid credentials");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded) return Unauthorized("Invalid credentials");

        var tokenPair = await _tokenService.GenerateTokenPairAsync(user, request.TenantId, GetClientIpAddress(), cancellationToken);
        return Ok(new { tokenPair.AccessToken, tokenPair.RefreshToken, User = new { user.Id, user.Email, user.FirstName, user.LastName } });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenPair = await _tokenService.RefreshTokenAsync(request.RefreshToken, request.TenantId, GetClientIpAddress(), cancellationToken);
        return tokenPair is null ? Unauthorized("Invalid or expired refresh token") : Ok(new { tokenPair.Value.AccessToken, tokenPair.Value.RefreshToken });
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
        => await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, GetClientIpAddress(), cancellationToken) ? Ok() : NotFound();

    [Authorize]
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdRaw, out var userId)) return Unauthorized("Invalid access token");
        await _tokenService.RevokeAllRefreshTokensAsync(userId, GetClientIpAddress(), cancellationToken);
        return Ok();
    }

    private string GetClientIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

public class RegisterRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    [Required] public string FirstName { get; set; } = string.Empty;
    [Required] public string LastName { get; set; } = string.Empty;
    [Required] public Guid TenantId { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    [Required] public Guid TenantId { get; set; }
}

public class RefreshTokenRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
    [Required] public Guid TenantId { get; set; }
}

public class RevokeTokenRequest { [Required] public string RefreshToken { get; set; } = string.Empty; }
