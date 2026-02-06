using Application.Common.Interfaces.MultiTenancy;
using Application.Tenants.Commands;
using Application.Tenants.DTOs;
using Application.Tenants.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Admin;

[ApiController]
[Route("api/admin/tenants")]
[Authorize(Policy = "Admin")]
public class TenantAdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantOnboardingService _onboardingService;

    public TenantAdminController(IMediator mediator, ITenantOnboardingService onboardingService)
    {
        _mediator = mediator;
        _onboardingService = onboardingService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetTenantsQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
        => Ok(new { TenantId = await _mediator.Send(new CreateTenantCommand(request), cancellationToken) });

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateTenantSettingsRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateTenantSettingsCommand(request), cancellationToken);
        return NoContent();
    }

    [HttpPut("assign-plan")]
    public async Task<IActionResult> AssignPlan([FromBody] AssignPlanRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AssignPlanCommand(request), cancellationToken);
        return NoContent();
    }

    [HttpPut("suspend")]
    public async Task<IActionResult> Suspend([FromBody] SuspendTenantRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SuspendTenantCommand(request), cancellationToken);
        return NoContent();
    }

    [HttpPost("{tenantId:guid}/onboard")]
    public async Task<IActionResult> Onboard(Guid tenantId, [FromBody] OnboardingRequest request, CancellationToken cancellationToken)
    {
        await _onboardingService.RunAsync(tenantId, request.AdminEmail, request.AdminPassword, cancellationToken);
        return Ok();
    }
}

public sealed record OnboardingRequest(string AdminEmail, string AdminPassword);
