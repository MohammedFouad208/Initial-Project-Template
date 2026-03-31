using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AdminTemplate.Application.Services;

public class UserService : IUserService
{
    public Task<UserDto?> GetByIdAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<(IReadOnlyList<UserDto> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> CreateAsync(CreateUserDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> UpdateAsync(string id, UpdateUserDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> SetActiveAsync(string id, bool isActive)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> DeleteAsync(string id)
    {
        throw new NotImplementedException();
    }
}
