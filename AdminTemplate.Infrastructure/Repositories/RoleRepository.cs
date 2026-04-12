using AdminTemplate.Domain.Entities;
using AdminTemplate.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdminTemplate.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoleRepository(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<ApplicationRole?> GetByIdAsync(Guid id)
    {
        return await _roleManager.FindByIdAsync(id.ToString());
    }

    public async Task<IReadOnlyList<ApplicationRole>> GetAllAsync()
    {
        return await _roleManager.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<ApplicationRole> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null,
        string sortColumn = "name",
        string sortDirection = "asc")
    {
        var query = _roleManager.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(r =>
                r.Name!.Contains(searchTerm) ||
                (r.Description != null && r.Description.Contains(searchTerm)));
        }

        var total = await query.CountAsync();

        var isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortColumn.ToLowerInvariant() switch
        {
            "description"  => isDesc ? query.OrderByDescending(r => r.Description) : query.OrderBy(r => r.Description),
            "createdat"    => isDesc ? query.OrderByDescending(r => r.CreatedAt)   : query.OrderBy(r => r.CreatedAt),
            _              => isDesc ? query.OrderByDescending(r => r.Name)        : query.OrderBy(r => r.Name)
        };

        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<ApplicationRole?> GetByNameAsync(string name)
    {
        return await _roleManager.FindByNameAsync(name);
    }

    public async Task AddAsync(ApplicationRole role)
    {
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create role: {error}");
        }
    }

    public async Task UpdateAsync(ApplicationRole role)
    {
        await _roleManager.UpdateAsync(role);
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
        {
            return;
        }

        await _roleManager.DeleteAsync(role);
    }

    public async Task<bool> HasUsersAsync(Guid roleId)
    {
        var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        if (role is null || string.IsNullOrWhiteSpace(role.Name))
        {
            return false;
        }

        var users = await _userManager.GetUsersInRoleAsync(role.Name);
        return users.Count > 0;
    }

    public async Task<int> GetUserCountAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null || string.IsNullOrWhiteSpace(role.Name))
        {
            return 0;
        }

        var users = await _userManager.GetUsersInRoleAsync(role.Name);
        return users.Count;
    }
}
