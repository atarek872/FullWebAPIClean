using Application.Common.Interfaces.MultiTenancy;
using Domain.MultiTenancy;

namespace API.Middleware;

public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantStore tenantStore, ITenantContext tenantContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path == "/" || path.StartsWith("/swagger") || path.StartsWith("/health"))
        {
            await _next(context);
            return;
        }

        var tenant = await ResolveTenantAsync(context, tenantStore);
        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found." });
            return;
        }

        if (tenant.Status != TenantStatus.Active)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant is not active." });
            return;
        }

        tenantContext.SetTenant(new TenantContextSnapshot(
            tenant.Id,
            tenant.Schema,
            tenant.Plan,
            tenant.Status,
            tenant.ApiRequestLimitPerDay,
            tenant.StorageLimitMb));

        context.Items["TenantId"] = tenant.Id;
        await _next(context);
    }

    private static async Task<Domain.Entities.Multitenancy.Tenant?> ResolveTenantAsync(HttpContext context, ITenantStore tenantStore)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantHeader) && Guid.TryParse(tenantHeader, out var tenantId))
        {
            return await tenantStore.GetByIdAsync(tenantId);
        }

        var host = context.Request.Host.Host;
        var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
        {
            var subdomain = parts[0];
            return await tenantStore.GetBySubdomainAsync(subdomain);
        }

        return null;
    }
}
