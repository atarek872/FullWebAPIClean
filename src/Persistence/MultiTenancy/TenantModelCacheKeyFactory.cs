using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Persistence.MultiTenancy;

public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is ApplicationDbContext appDbContext)
        {
            return (context.GetType(), appDbContext.TenantSchema, designTime);
        }

        return (context.GetType(), designTime);
    }
}
