using Application.Common.Interfaces.MultiTenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresFeatureAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _featureCode;

    public RequiresFeatureAttribute(string featureCode)
    {
        _featureCode = featureCode;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var evaluator = context.HttpContext.RequestServices.GetRequiredService<IFeatureEvaluationService>();
        if (!await evaluator.IsEnabledAsync(_featureCode, context.HttpContext.RequestAborted))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
