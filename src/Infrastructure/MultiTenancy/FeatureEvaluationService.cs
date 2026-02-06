using Application.Common.Interfaces.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.MultiTenancy;

public class FeatureEvaluationService : IFeatureEvaluationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public FeatureEvaluationService(ApplicationDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<bool> IsEnabledAsync(string featureCode, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved)
        {
            return false;
        }

        return await _dbContext.TenantFeatures
            .Include(tf => tf.Feature)
            .AnyAsync(tf => tf.TenantId == _tenantContext.TenantId &&
                            tf.Feature!.Code == featureCode &&
                            tf.IsEnabled,
                cancellationToken);
    }
}
