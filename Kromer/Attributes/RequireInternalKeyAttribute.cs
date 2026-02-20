using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kromer.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireInternalKeyAttribute() : TypeFilterAttribute(typeof(RequireInternalKeyFilter));

public class RequireInternalKeyFilter(IConfiguration configuration) : IAuthorizationFilter
{
    public const string InternalKeyHeader = "Kromer-Key";
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(InternalKeyHeader, out var requestKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        var key = configuration.GetValue<string>("InternalKey");
        if (key is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (key != requestKey)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}