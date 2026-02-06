using API.Middleware;
using Application;
using Domain.Authorization;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Persistence;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddPersistenceServices(builder.Configuration);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FullWebAPI", Version = "v1" });
    options.OperationFilter<TenantHeaderOperationFilter>();
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme }
    };
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, Array.Empty<string>() } });
});

builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var key = context.User.FindFirst("tenant_id")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions { AutoReplenishment = true, PermitLimit = 100, Window = TimeSpan.FromMinutes(1) });
    });
});

builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddCors(options => options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.MapGet("/", () => Results.Redirect("/swagger"));
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseResponseCompression();
app.UseMiddleware<TenantResolverMiddleware>();
app.UseMiddleware<PlanLimitMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await SeedIdentityDataAsync(app);
app.Run();

static async Task SeedIdentityDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var defaultRoles = new[]
    {
        new { Name = "Admin", Description = "Default Admin role", Permissions = PermissionConstants.All.ToArray() },
        new { Name = "User", Description = "Default User role", Permissions = new[] { PermissionConstants.Permissions.UsersView } },
        new { Name = "TenantAdmin", Description = "Tenant admin role", Permissions = PermissionConstants.All.ToArray() },
        new { Name = "TenantUser", Description = "Tenant user role", Permissions = new[] { PermissionConstants.Permissions.UsersView } }
    };

    foreach (var roleDefinition in defaultRoles)
    {
        var role = await roleManager.FindByNameAsync(roleDefinition.Name);
        if (role is null)
        {
            role = new Role { Name = roleDefinition.Name, Description = roleDefinition.Description };
            await roleManager.CreateAsync(role);
        }

        var existingClaims = await roleManager.GetClaimsAsync(role);
        foreach (var permission in roleDefinition.Permissions)
        {
            if (existingClaims.Any(claim => claim.Type == PermissionConstants.PermissionClaimType && claim.Value.Equals(permission, StringComparison.OrdinalIgnoreCase))) continue;
            await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim(PermissionConstants.PermissionClaimType, permission));
        }
    }
}
