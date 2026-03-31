using AdminTemplate.Domain.Entities;

namespace AdminTemplate.Domain.Interfaces;

public interface IRoleRepository
{
    Task<ApplicationRole?> GetByIdAsync(Guid id);
    Task<ApplicationRole?> GetByNameAsync(string name);
    Task<IReadOnlyList<ApplicationRole>> GetAllAsync();
    Task<(IReadOnlyList<ApplicationRole> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null);
    Task AddAsync(ApplicationRole role);
    Task UpdateAsync(ApplicationRole role);
    Task DeleteAsync(Guid id);
    Task<bool> HasUsersAsync(Guid roleId);
}
