using Application.Common.Interfaces;
using Application.Common.Interfaces.MultiTenancy;
using Domain.Authorization;
using Domain.Entities;
using Infrastructure.Billing;
using Infrastructure.MultiTenancy;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using System.Security.Claims;
using System.Text;

namespace Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<IFeatureEvaluationService, FeatureEvaluationService>();
        services.AddScoped<ITenantCache, TenantMemoryCache>();
        services.AddScoped<ITenantStorageService, TenantStorageService>();
        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
        services.AddScoped<IBillingService, StripeBillingService>();

        services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 10;
            options.Password.RequiredUniqueChars = 4;
            options.User.RequireUniqueEmail = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        var jwtSettings = configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT secret is not configured"));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var tenantClaim = context.Principal?.FindFirst("tenant_id")?.Value;
                    if (!Guid.TryParse(tenantClaim, out var tenantId))
                    {
                        context.Fail("Missing tenant claim");
                        return;
                    }

                    var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var exists = await db.Tenants.AnyAsync(x => x.Id == tenantId && !x.IsDeleted);
                    if (!exists)
                    {
                        context.Fail("Invalid tenant.");
                    }
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin", "TenantAdmin"));
            options.AddPolicy("User", policy => policy.RequireRole("User", "TenantUser"));
            options.AddPolicy("ManageUsers", policy => policy.RequireAssertion(context =>
                context.User.IsInRole("Admin") ||
                context.User.HasClaim(PermissionConstants.PermissionClaimType, PermissionConstants.Permissions.FullAccess) ||
                context.User.HasClaim(PermissionConstants.PermissionClaimType, PermissionConstants.Permissions.RolesManage)));
        });

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        return services;
    }
}
