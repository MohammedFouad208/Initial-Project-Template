using AdminTemplate.Domain.Entities;
using AdminTemplate.Domain.Interfaces;
using AdminTemplate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AdminTemplate.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly ApplicationDbContext _context;

    public PermissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RolePermission>> GetByRoleIdAsync(Guid roleId)
    {
        return await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid roleId, string objectName, string functionName)
    {
        return await _context.RolePermissions.AnyAsync(rp =>
            rp.RoleId == roleId &&
            rp.ObjectName == objectName &&
            rp.FunctionName == functionName);
    }

    public async Task<int> GetCountByRoleIdAsync(Guid roleId)
    {
        return await _context.RolePermissions.CountAsync(rp => rp.RoleId == roleId);
    }

    public async Task AddAsync(RolePermission permission)
    {
        await _context.RolePermissions.AddAsync(permission);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<RolePermission> permissions)
    {
        await _context.RolePermissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByRoleIdAsync(Guid roleId)
    {
        var rows = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        if (rows.Count == 0)
        {
            return;
        }

        _context.RolePermissions.RemoveRange(rows);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var row = await _context.RolePermissions.FindAsync(id);
        if (row is null)
        {
            return;
        }

        _context.RolePermissions.Remove(row);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, string objectName, string functionName)
    {
        return await _context.UserRoles
            .Join(_context.RolePermissions,
                ur => ur.RoleId,
                rp => rp.RoleId,
                (ur, rp) => new { ur.UserId, rp.ObjectName, rp.FunctionName })
            .AnyAsync(x =>
                x.UserId == userId &&
                x.ObjectName == objectName &&
                x.FunctionName == functionName);
    }
}
