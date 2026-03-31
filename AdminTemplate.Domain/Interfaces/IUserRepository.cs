using AdminTemplate.Domain.Entities;

namespace AdminTemplate.Domain.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(Guid id);
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<IReadOnlyList<ApplicationUser>> GetAllAsync();
    Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null);
    Task AddAsync(ApplicationUser user);
    Task UpdateAsync(ApplicationUser user);
    Task DeleteAsync(Guid id);
}
