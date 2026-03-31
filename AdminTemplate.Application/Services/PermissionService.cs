using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Interfaces;

namespace AdminTemplate.Application.Services;

public class PermissionService : IPermissionService
{
    public Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(Guid roleId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UserHasPermissionAsync(Guid userId, string objectName, string functionName)
    {
        throw new NotImplementedException();
    }

    public Task SaveRolePermissionsAsync(Guid roleId, IEnumerable<PermissionDto> permissions)
    {
        throw new NotImplementedException();
    }
}
