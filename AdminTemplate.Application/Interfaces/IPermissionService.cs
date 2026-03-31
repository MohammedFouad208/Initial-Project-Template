using AdminTemplate.Application.DTOs;

namespace AdminTemplate.Application.Interfaces;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(Guid roleId);
    Task<bool> UserHasPermissionAsync(Guid userId, string objectName, string functionName);
    Task SaveRolePermissionsAsync(Guid roleId, IEnumerable<PermissionDto> permissions);
}
