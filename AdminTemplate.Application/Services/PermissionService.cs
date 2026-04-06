using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Domain.Entities;
using AdminTemplate.Domain.Interfaces;

namespace AdminTemplate.Application.Services;

public class PermissionService : IPermissionService
{
    private const string SuperAdminRoleName = "SuperAdmin";

    private readonly IUserRepository _userRepository;
    private readonly IPermissionRepository _permissionRepository;

    public PermissionService(IUserRepository userRepository, IPermissionRepository permissionRepository)
    {
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(Guid roleId)
    {
        var entities = await _permissionRepository.GetByRoleIdAsync(roleId);
        return entities.Select(e => new PermissionDto(e.ObjectName, e.FunctionName))
                       .ToList()
                       .AsReadOnly();
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, string objectName, string functionName)
    {
        var roleNames = await _userRepository.GetRoleNamesAsync(userId);
        if (roleNames.Contains(SuperAdminRoleName, StringComparer.OrdinalIgnoreCase))
            return true;

        return await _permissionRepository.UserHasPermissionAsync(userId, objectName, functionName);
    }

    public async Task SaveRolePermissionsAsync(Guid roleId, IEnumerable<PermissionDto> permissions)
    {
        await _permissionRepository.DeleteByRoleIdAsync(roleId);

        var entities = permissions.Select(p => new RolePermission
        {
            RoleId = roleId,
            ObjectName = p.ObjectName,
            FunctionName = p.FunctionName
        });

        await _permissionRepository.AddRangeAsync(entities);
    }
}
