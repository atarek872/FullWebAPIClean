using Application.Common.Interfaces.MultiTenancy;
using Domain.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Persistence.MultiTenancy;

public class TenantDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=FullWebApiClean;Trusted_Connection=True;TrustServerCertificate=True;");
        var tenantSchema = args.FirstOrDefault(x => x.StartsWith("--schema="))?.Split('=')[1] ?? "public";
        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeTenantContext(tenantSchema));
    }

    private class DesignTimeTenantContext : ITenantContext
    {
        public DesignTimeTenantContext(string schema) => TenantSchema = schema;
        public bool IsResolved => true;
        public Guid TenantId => Guid.Empty;
        public string TenantSchema { get; }
        public PlanType Plan => PlanType.Free;
        public TenantStatus Status => TenantStatus.Active;
        public int ApiRequestLimitPerDay => int.MaxValue;
        public long StorageLimitMb => long.MaxValue;
        public void SetTenant(TenantContextSnapshot snapshot) { }
    }
}
