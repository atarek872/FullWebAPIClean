using Application.Common.Interfaces.MultiTenancy;
using Domain.Entities;
using Domain.Entities.Multitenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.MultiTenancy;

public class TenantOnboardingService : ITenantOnboardingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;

    public TenantOnboardingService(ApplicationDbContext dbContext, RoleManager<Role> roleManager, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task RunAsync(Guid tenantId, string adminEmail, string adminPassword, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants.FirstAsync(x => x.Id == tenantId, cancellationToken);
        await _dbContext.Database.ExecuteSqlRawAsync($"IF NOT EXISTS (SELECT schema_name FROM information_schema.schemata WHERE schema_name = '{tenant.Schema}') EXEC('CREATE SCHEMA [{tenant.Schema}]')", cancellationToken);

        foreach (var roleName in new[] { "TenantAdmin", "TenantManager", "TenantUser" })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new Role { Name = roleName, Description = $"{roleName} for tenant" });
            }
        }

        var admin = await _userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new User { Email = adminEmail, UserName = adminEmail, FirstName = "Tenant", LastName = "Admin" };
            var result = await _userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join(';', result.Errors.Select(x => x.Description)));
        }

        await _userManager.AddToRoleAsync(admin, "TenantAdmin");

        if (!await _dbContext.UserTenantMemberships.AnyAsync(x => x.TenantId == tenantId && x.UserId == admin.Id, cancellationToken))
        {
            _dbContext.UserTenantMemberships.Add(new UserTenantMembership { TenantId = tenantId, UserId = admin.Id, Role = "TenantAdmin", PermissionsCsv = "*", IsDefault = true });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
