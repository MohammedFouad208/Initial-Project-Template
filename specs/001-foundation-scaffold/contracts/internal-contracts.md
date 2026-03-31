# Internal Service Contracts: Foundation & Project Scaffold

**Branch**: `001-foundation-scaffold`
**Date**: 2026-03-29
**Scope**: Internal C# interface contracts defined in Domain and Application layers. These are not HTTP endpoints — they are the cross-layer boundaries that enforce clean architecture.

---

## Domain Layer Contracts (Interfaces in `AdminTemplate.Domain`)

### IUserRepository

```csharp
namespace AdminTemplate.Domain.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string id);
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<IReadOnlyList<ApplicationUser>> GetAllAsync();
    Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, string? searchTerm = null);
    Task AddAsync(ApplicationUser user);
    Task UpdateAsync(ApplicationUser user);
    Task DeleteAsync(string id);
}
```

**Notes**:
- `GetByEmailAsync` is used by the seeder to check whether the SuperAdmin user already exists.
- `GetPagedAsync` is used by Phase 6 (User Management DataTable).
- `AddAsync` / `UpdateAsync` delegate to `UserManager<ApplicationUser>` in the Infrastructure implementation to ensure password hashing and Identity validation run correctly.

---

### IRoleRepository

```csharp
namespace AdminTemplate.Domain.Interfaces;

public interface IRoleRepository
{
    Task<ApplicationRole?> GetByIdAsync(string id);
    Task<ApplicationRole?> GetByNameAsync(string name);
    Task<IReadOnlyList<ApplicationRole>> GetAllAsync();
    Task<(IReadOnlyList<ApplicationRole> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, string? searchTerm = null);
    Task AddAsync(ApplicationRole role);
    Task UpdateAsync(ApplicationRole role);
    Task DeleteAsync(string id);
    Task<bool> HasUsersAsync(string roleId);
}
```

**Notes**:
- `GetByNameAsync` is used by the seeder to check whether SuperAdmin role exists.
- `HasUsersAsync` is the guard used by Phase 7 (Role Management) to prevent deletion of a role that has assigned users.

---

### IPermissionRepository

```csharp
namespace AdminTemplate.Domain.Interfaces;

public interface IPermissionRepository
{
    Task<IReadOnlyList<RolePermission>> GetByRoleIdAsync(string roleId);
    Task<bool> ExistsAsync(string roleId, string objectName, string functionName);
    Task AddAsync(RolePermission permission);
    Task AddRangeAsync(IEnumerable<RolePermission> permissions);
    Task DeleteByRoleIdAsync(string roleId);
    Task DeleteAsync(Guid id);
}
```

**Notes**:
- `ExistsAsync` is used by the seeder to implement idempotent permission seeding.
- `DeleteByRoleIdAsync` is used by Phase 8 (Role-Permission Matrix) to replace all permissions for a role atomically.
- `AddRangeAsync` enables batch inserts for efficient seeding and batch saves from the permission matrix.

---

## Application Layer Contracts (Interfaces in `AdminTemplate.Application`)

### IPermissionProvider

```csharp
namespace AdminTemplate.Application.Providers;

public interface IPermissionProvider
{
    /// <summary>
    /// Returns all permission objects and their functions as defined
    /// in the permissions.json configuration file.
    /// </summary>
    IReadOnlyList<PermissionObjectDto> GetAll();
}
```

**Notes**:
- Implemented in Phase 1 by `JsonPermissionProvider` (Infrastructure layer).
- Registered as a singleton — the JSON file is read once at startup.
- Used by the seeder in Phase 1 and by the permission matrix UI in Phase 8.

---

### IUserService (stub — full implementation in Phase 6)

```csharp
namespace AdminTemplate.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(string id);
    Task<(IReadOnlyList<UserDto> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, string? searchTerm = null);
    Task<IdentityResult> CreateAsync(CreateUserDto dto);
    Task<IdentityResult> UpdateAsync(string id, UpdateUserDto dto);
    Task<IdentityResult> SetActiveAsync(string id, bool isActive);
    Task<IdentityResult> DeleteAsync(string id);
}
```

---

### IRoleService (stub — full implementation in Phase 7)

```csharp
namespace AdminTemplate.Application.Interfaces;

public interface IRoleService
{
    Task<RoleDto?> GetByIdAsync(string id);
    Task<IReadOnlyList<RoleDto>> GetAllAsync();
    Task<(IReadOnlyList<RoleDto> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, string? searchTerm = null);
    Task<IdentityResult> CreateAsync(CreateRoleDto dto);
    Task<IdentityResult> UpdateAsync(string id, UpdateRoleDto dto);
    Task<IdentityResult> DeleteAsync(string id);
}
```

---

### IPermissionService (stub — full implementation in Phase 4)

```csharp
namespace AdminTemplate.Application.Interfaces;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(string roleId);
    Task<bool> UserHasPermissionAsync(string userId, string objectName, string functionName);
    Task SaveRolePermissionsAsync(string roleId, IEnumerable<PermissionDto> permissions);
}
```

---

## DTOs (Application Layer)

### PermissionObjectDto

```csharp
namespace AdminTemplate.Application.DTOs;

public record PermissionObjectDto(
    string Name,
    string DisplayName,
    IReadOnlyList<string> Functions
);
```

### PermissionDto

```csharp
namespace AdminTemplate.Application.DTOs;

public record PermissionDto(
    string ObjectName,
    string FunctionName
);
```

### UserDto

```csharp
namespace AdminTemplate.Application.DTOs;

public record UserDto(
    string Id,
    string FullName,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt
);
```

### RoleDto

```csharp
namespace AdminTemplate.Application.DTOs;

public record RoleDto(
    string Id,
    string Name,
    string? Description,
    int UserCount,
    int PermissionCount,
    DateTime CreatedAt
);
```

---

## Contract Stability Notes

- All interfaces above are **stable for Phase 1**. Implementations may be stubs that throw `NotImplementedException` for methods not yet needed.
- The `IPermissionProvider.GetAll()` method MUST be fully implemented in Phase 1 (required by the seeder).
- `IPermissionRepository.ExistsAsync`, `AddAsync`, and `AddRangeAsync` MUST be fully implemented in Phase 1 (required by the seeder).
- `IRoleRepository.GetByNameAsync` and `IUserRepository.GetByEmailAsync` MUST be fully implemented in Phase 1 (required by the seeder's idempotency checks).
- All other methods may be stubbed with `throw new NotImplementedException()` until their corresponding phase.
