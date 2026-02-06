using Infrastructure;
using Persistence;
using Worker;
using Worker.Jobs;
using Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddHostedService<BackgroundWorker>();
builder.Services.AddScoped<ITenantJob, EmailDigestJob>();
builder.Services.AddScoped<ITenantJob, CleanupInactiveItemsJob>();
builder.Services.AddScoped<ITenantJob, BillingUsageSyncJob>();

var host = builder.Build();
host.Run();
