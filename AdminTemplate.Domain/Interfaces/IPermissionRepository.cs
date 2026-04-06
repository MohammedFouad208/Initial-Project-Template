using AdminTemplate.Domain.Entities;

namespace AdminTemplate.Domain.Interfaces;

public interface IPermissionRepository
{
    Task<IReadOnlyList<RolePermission>> GetByRoleIdAsync(Guid roleId);
    Task<bool> ExistsAsync(Guid roleId, string objectName, string functionName);
    Task<bool> UserHasPermissionAsync(Guid userId, string objectName, string functionName);
    Task AddAsync(RolePermission permission);
    Task AddRangeAsync(IEnumerable<RolePermission> permissions);
    Task DeleteByRoleIdAsync(Guid roleId);
    Task DeleteAsync(Guid id);
}
