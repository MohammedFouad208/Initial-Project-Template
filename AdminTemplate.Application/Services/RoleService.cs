using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Domain.Entities;
using AdminTemplate.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AdminTemplate.Application.Services;

public class RoleService : IRoleService
{
    private const string SuperAdminRoleName = "SuperAdmin";

    private static readonly IdentityError ProtectedRole = new()
    {
        Code        = "ProtectedRole",
        Description = "The SuperAdmin role cannot be deleted."
    };

    private static readonly IdentityError RoleHasUsers = new()
    {
        Code        = "RoleHasUsers",
        Description = "Cannot delete a role that has assigned users."
    };

    private static readonly IdentityError RoleNotFound = new()
    {
        Code        = "RoleNotFound",
        Description = "Role not found."
    };

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _roleManager           = roleManager;
        _roleRepository        = roleRepository;
        _permissionRepository  = permissionRepository;
    }

    public async Task<RoleDto?> GetByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return null;
        }

        var role = await _roleRepository.GetByIdAsync(guid);
        if (role is null)
        {
            return null;
        }

        var userCount       = await _roleRepository.GetUserCountAsync(role.Id);
        var permissionCount = await _permissionRepository.GetCountByRoleIdAsync(role.Id);

        return ToDto(role, userCount, permissionCount);
    }

    public Task<int> GetTotalCountAsync()
    {
        return Task.FromResult(_roleManager.Roles.Count());
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        var result = new List<RoleDto>(roles.Count);

        foreach (var role in roles)
        {
            var userCount       = await _roleRepository.GetUserCountAsync(role.Id);
            var permissionCount = await _permissionRepository.GetCountByRoleIdAsync(role.Id);
            result.Add(ToDto(role, userCount, permissionCount));
        }

        return result;
    }

    public async Task<(IReadOnlyList<RoleDto> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null,
        string sortColumn = "name",
        string sortDirection = "asc")
    {
        var (roles, total) = await _roleRepository.GetPagedAsync(
            pageIndex, pageSize, searchTerm, sortColumn, sortDirection);

        var result = new List<RoleDto>(roles.Count);
        foreach (var role in roles)
        {
            var userCount       = await _roleRepository.GetUserCountAsync(role.Id);
            var permissionCount = await _permissionRepository.GetCountByRoleIdAsync(role.Id);
            result.Add(ToDto(role, userCount, permissionCount));
        }

        return (result, total);
    }

    public async Task<IdentityResult> CreateAsync(CreateRoleDto dto)
    {
        var role = new ApplicationRole(dto.Name.Trim())
        {
            Description = dto.Description?.Trim()
        };

        return await _roleManager.CreateAsync(role);
    }

    public async Task<IdentityResult> UpdateAsync(string id, UpdateRoleDto dto)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return IdentityResult.Failed(RoleNotFound);
        }

        var role = await _roleRepository.GetByIdAsync(guid);
        if (role is null)
        {
            return IdentityResult.Failed(RoleNotFound);
        }

        role.Name        = dto.Name.Trim();
        role.Description = dto.Description?.Trim();

        return await _roleManager.UpdateAsync(role);
    }

    public async Task<IdentityResult> DeleteAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return IdentityResult.Failed(RoleNotFound);
        }

        var role = await _roleRepository.GetByIdAsync(guid);
        if (role is null)
        {
            return IdentityResult.Failed(RoleNotFound);
        }

        if (string.Equals(role.Name, SuperAdminRoleName, StringComparison.OrdinalIgnoreCase))
        {
            return IdentityResult.Failed(ProtectedRole);
        }

        if (await _roleRepository.HasUsersAsync(guid))
        {
            return IdentityResult.Failed(RoleHasUsers);
        }

        await _roleRepository.DeleteAsync(guid);
        return IdentityResult.Success;
    }

    private static RoleDto ToDto(ApplicationRole role, int userCount, int permissionCount) =>
        new(
            role.Id.ToString(),
            role.Name ?? string.Empty,
            role.Description,
            userCount,
            permissionCount,
            role.CreatedAt);
}
