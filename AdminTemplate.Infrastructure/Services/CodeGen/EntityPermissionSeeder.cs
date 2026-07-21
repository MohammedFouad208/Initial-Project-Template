using AdminTemplate.Application.Interfaces.CodeGen;
using AdminTemplate.Domain.Entities;
using AdminTemplate.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AdminTemplate.Infrastructure.Services.CodeGen;

public class EntityPermissionSeeder : IEntityPermissionSeeder
{
    private static readonly string[] DefaultFunctions = ["Browse", "Create", "Update", "Delete", "Export"];

    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public EntityPermissionSeeder(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
    {
        _roleRepository       = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task SeedPermissionsAsync(string entityName)
    {
        var superAdminRole = await _roleRepository.GetByNameAsync("SuperAdmin");
        if (superAdminRole is null) return;

        foreach (var func in DefaultFunctions)
        {
            var exists = await _permissionRepository.ExistsAsync(superAdminRole.Id, entityName, func);
            if (!exists)
            {
                await _permissionRepository.AddAsync(new RolePermission
                {
                    RoleId     = superAdminRole.Id,
                    ObjectName = entityName,
                    FunctionName = func,
                    CreatedAt  = DateTime.UtcNow
                });
            }
        }
    }
}
