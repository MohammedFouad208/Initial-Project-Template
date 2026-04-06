using System.Security.Claims;
using AdminTemplate.Application.Interfaces;

namespace AdminTemplate.Application.Services;

public static class PermissionServiceExtensions
{
    public static async Task<bool> CurrentUserHasPermissionAsync(
        this IPermissionService service,
        ClaimsPrincipal user,
        string objectName,
        string functionName)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return false;

        return await service.UserHasPermissionAsync(userId, objectName, functionName);
    }
}
