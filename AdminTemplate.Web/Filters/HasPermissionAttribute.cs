using System.Security.Claims;
using AdminTemplate.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace AdminTemplate.Web.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _objectName;
    private readonly string _functionName;

    public HasPermissionAttribute(string objectName, string functionName)
    {
        _objectName = objectName;
        _functionName = functionName;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var permissionService = context.HttpContext.RequestServices
            .GetRequiredService<IPermissionService>();

        var hasPermission = await permissionService.UserHasPermissionAsync(
            userId, _objectName, _functionName);

        if (!hasPermission)
        {
            context.Result = new ViewResult
            {
                ViewName = "~/Views/Shared/Error403.cshtml",
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
