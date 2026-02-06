using Application.Common.Interfaces.MultiTenancy;
using Serilog.Core;
using Serilog.Events;

namespace Infrastructure.MultiTenancy;

public class TenantIdLogEnricher : ILogEventEnricher
{
    private readonly ITenantContext _tenantContext;

    public TenantIdLogEnricher(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var value = _tenantContext.IsResolved ? _tenantContext.TenantId.ToString() : "none";
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TenantId", value));
    }
}
