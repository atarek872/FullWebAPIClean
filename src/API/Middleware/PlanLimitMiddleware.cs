using Application.Common.Interfaces.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace API.Middleware;

public class PlanLimitMiddleware
{
    private readonly RequestDelegate _next;

    public PlanLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ApplicationDbContext dbContext)
    {
        if (!tenantContext.IsResolved)
        {
            await _next(context);
            return;
        }

        var today = DateTime.UtcNow.Date;
        var dailyUsage = await dbContext.UsageRecords
            .Where(x => x.TenantId == tenantContext.TenantId && x.Metric == "api_requests" && x.CapturedAtUtc >= today)
            .SumAsync(x => (long?)x.Amount) ?? 0;

        if (dailyUsage >= tenantContext.ApiRequestLimitPerDay)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { error = "Plan request limit reached" });
            return;
        }

        dbContext.UsageRecords.Add(new Domain.Entities.Multitenancy.UsageRecord
        {
            TenantId = tenantContext.TenantId,
            Metric = "api_requests",
            Amount = 1,
            PlanAtCapture = tenantContext.Plan
        });
        await dbContext.SaveChangesAsync(context.RequestAborted);

        await _next(context);
    }
}
