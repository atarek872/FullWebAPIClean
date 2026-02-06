namespace Application.Common.Interfaces.MultiTenancy;

public interface IFeatureEvaluationService
{
    Task<bool> IsEnabledAsync(string featureCode, CancellationToken cancellationToken = default);
}
