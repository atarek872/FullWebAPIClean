using Domain.Authorization;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/identity-admin")]
[Authorize(Policy = "ManageUsers")]
public class IdentityAdministrationController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public IdentityAdministrationController(UserManager<User> userManager, RoleManager<Role> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet("permissions/catalog")]
    public IActionResult GetPermissionsCatalog()
    {
        var groups = PermissionConstants.Groups
            .Select(group => new PermissionGroupDto(group.Key, group.Value.Order().ToList()))
            .OrderBy(group => group.Name)
            .ToList();

        return Ok(new
        {
            Permissions = PermissionConstants.All.Order().ToList(),
            Groups = groups
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = _userManager.Users.Where(u => !u.IsDeleted).ToList();
        var response = new List<UserDetailsResponse>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            response.Add(MapUserResponse(user, roles));
        }

        return Ok(response.OrderBy(u => u.Email));
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
        {
            return NotFound("User not found");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(MapUserResponse(user, roles));
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateManagedUserRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = request.IsActive,
            CreatedBy = User.Identity?.Name
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors);
        }

        if (request.Roles.Count > 0)
        {
            var unknownRoles = request.Roles.Where(role => !_roleManager.Roles.Any(r => r.Name == role)).ToList();
            if (unknownRoles.Count > 0)
            {
                return BadRequest($"Unknown roles: {string.Join(", ", unknownRoles)}");
            }

            var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!roleResult.Succeeded)
            {
                return BadRequest(roleResult.Errors);
            }
        }

        return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, new { Message = "User created successfully", user.Id });
    }

    [Authorize(Policy = "CanEditUsers")]
    [HttpPut("users/{userId:guid}")]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
        {
            return NotFound("User not found");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.UserName = request.Email;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = User.Identity?.Name;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { Message = "User updated successfully" });
    }

    [Authorize(Policy = "CanDeleteUsers")]
    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
        {
            return NotFound("User not found");
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = User.Identity?.Name;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { Message = "User deleted successfully" });
    }

    [HttpPut("users/{userId:guid}/roles")]
    public async Task<IActionResult> SetUserRoles(Guid userId, [FromBody] SetUserRolesRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
        {
            return NotFound("User not found");
        }

        var unknownRoles = request.Roles.Where(role => !_roleManager.Roles.Any(r => r.Name == role)).ToList();
        if (unknownRoles.Count > 0)
        {
            return BadRequest($"Unknown roles: {string.Join(", ", unknownRoles)}");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return BadRequest(removeResult.Errors);
        }

        var addResult = await _userManager.AddToRolesAsync(user, request.Roles.Distinct(StringComparer.OrdinalIgnoreCase));
        if (!addResult.Succeeded)
        {
            return BadRequest(addResult.Errors);
        }

        return Ok(new { Message = "User roles updated successfully" });
    }

    [Authorize(Policy = "CanExportUsers")]
    [HttpGet("users/export")]
    public async Task<IActionResult> ExportUsers(CancellationToken cancellationToken)
    {
        var users = _userManager.Users.Where(u => !u.IsDeleted).ToList();
        var rows = new List<string> { "Id,Email,FirstName,LastName,IsActive,Roles" };

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add($"{user.Id},{EscapeCsv(user.Email)},{EscapeCsv(user.FirstName)},{EscapeCsv(user.LastName)},{user.IsActive},{EscapeCsv(string.Join('|', roles.Order()))}");
        }

        var csv = string.Join(Environment.NewLine, rows);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"users-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet("roles")]
    public IActionResult GetRoles()
    {
        var roles = _roleManager.Roles
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Name)
            .Select(r => new RoleSummaryResponse(r.Id, r.Name ?? string.Empty, r.Description, r.IsActive))
            .ToList();

        return Ok(roles);
    }

    [HttpGet("roles/{roleId:guid}")]
    public async Task<IActionResult> GetRole(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null || role.IsDeleted)
        {
            return NotFound("Role not found");
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(c => c.Type == PermissionConstants.PermissionClaimType)
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();

        var groups = claims
            .Where(c => c.Type == PermissionConstants.PermissionGroupClaimType)
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();

        return Ok(new RoleDetailsResponse(role.Id, role.Name ?? string.Empty, role.Description, role.IsActive, permissions, groups));
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (await _roleManager.RoleExistsAsync(request.Name))
        {
            return Conflict("Role already exists");
        }

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedBy = User.Identity?.Name
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetRole), new { roleId = role.Id }, new { Message = "Role created successfully", role.Id });
    }

    [HttpPut("roles/{roleId:guid}")]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] UpdateRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null || role.IsDeleted)
        {
            return NotFound("Role not found");
        }

        role.Name = request.Name;
        role.Description = request.Description;
        role.IsActive = request.IsActive;
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = User.Identity?.Name;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { Message = "Role updated successfully" });
    }

    [HttpDelete("roles/{roleId:guid}")]
    public async Task<IActionResult> DeleteRole(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null || role.IsDeleted)
        {
            return NotFound("Role not found");
        }

        role.IsDeleted = true;
        role.IsActive = false;
        role.DeletedAt = DateTime.UtcNow;
        role.DeletedBy = User.Identity?.Name;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { Message = "Role deleted successfully" });
    }

    [HttpPut("roles/{roleId:guid}/permissions")]
    public async Task<IActionResult> SetRolePermissions(Guid roleId, [FromBody] SetRolePermissionsRequest request)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null || role.IsDeleted)
        {
            return NotFound("Role not found");
        }

        var invalidPermissions = request.Permissions.Where(permission => !PermissionConstants.All.Contains(permission)).ToList();
        if (invalidPermissions.Count > 0)
        {
            return BadRequest($"Invalid permissions: {string.Join(", ", invalidPermissions)}");
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in claims.Where(claim => claim.Type == PermissionConstants.PermissionClaimType))
        {
            var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
            if (!removeResult.Succeeded)
            {
                return BadRequest(removeResult.Errors);
            }
        }

        foreach (var permission in request.Permissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var addResult = await _roleManager.AddClaimAsync(role, new Claim(PermissionConstants.PermissionClaimType, permission));
            if (!addResult.Succeeded)
            {
                return BadRequest(addResult.Errors);
            }
        }

        return Ok(new { Message = "Role permissions updated successfully" });
    }

    [HttpPost("roles/{roleId:guid}/groups")]
    public async Task<IActionResult> AddGroupToRole(Guid roleId, [FromBody] AddGroupToRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null || role.IsDeleted)
        {
            return NotFound("Role not found");
        }

        if (!PermissionConstants.Groups.TryGetValue(request.GroupName, out var groupPermissions))
        {
            return BadRequest($"Unknown group: {request.GroupName}");
        }

        var currentClaims = await _roleManager.GetClaimsAsync(role);
        if (!currentClaims.Any(claim => claim.Type == PermissionConstants.PermissionGroupClaimType && claim.Value.Equals(request.GroupName, StringComparison.OrdinalIgnoreCase)))
        {
            var addGroupResult = await _roleManager.AddClaimAsync(role, new Claim(PermissionConstants.PermissionGroupClaimType, request.GroupName));
            if (!addGroupResult.Succeeded)
            {
                return BadRequest(addGroupResult.Errors);
            }
        }

        foreach (var permission in groupPermissions)
        {
            if (currentClaims.Any(claim => claim.Type == PermissionConstants.PermissionClaimType && claim.Value.Equals(permission, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var addPermissionResult = await _roleManager.AddClaimAsync(role, new Claim(PermissionConstants.PermissionClaimType, permission));
            if (!addPermissionResult.Succeeded)
            {
                return BadRequest(addPermissionResult.Errors);
            }
        }

        return Ok(new { Message = "Permission group added to role successfully" });
    }

    [HttpDelete("roles/{roleId:guid}/permissions/{permission}")]
    public async Task<IActionResult> RemovePermissionFromRole(Guid roleId, string permission)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null || role.IsDeleted)
        {
            return NotFound("Role not found");
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        var permissionClaims = claims
            .Where(claim => claim.Type == PermissionConstants.PermissionClaimType && claim.Value.Equals(permission, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (permissionClaims.Count == 0)
        {
            return NotFound("Permission not assigned to role");
        }

        foreach (var claim in permissionClaims)
        {
            var result = await _roleManager.RemoveClaimAsync(role, claim);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
        }

        return Ok(new { Message = "Permission removed successfully" });
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static UserDetailsResponse MapUserResponse(User user, IList<string> roles) =>
        new(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, user.IsActive, roles.Order().ToList());
}

public record PermissionGroupDto(string Name, IReadOnlyCollection<string> Permissions);

public record UserDetailsResponse(Guid Id, string Email, string FirstName, string LastName, bool IsActive, IReadOnlyCollection<string> Roles);

public record RoleSummaryResponse(Guid Id, string Name, string Description, bool IsActive);

public record RoleDetailsResponse(Guid Id, string Name, string Description, bool IsActive, IReadOnlyCollection<string> Permissions, IReadOnlyCollection<string> Groups);

public class CreateManagedUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public List<string> Roles { get; set; } = [];
}

public class UpdateUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class SetUserRolesRequest
{
    public List<string> Roles { get; set; } = [];
}

public class CreateRoleRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public class UpdateRoleRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class SetRolePermissionsRequest
{
    public List<string> Permissions { get; set; } = [];
}

public class AddGroupToRoleRequest
{
    [Required]
    public string GroupName { get; set; } = string.Empty;
}
