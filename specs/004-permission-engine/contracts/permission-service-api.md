# Contract: Permission Service API

**Feature Branch**: `004-permission-engine`  
**Date**: 2026-04-06  
**Scope**: Internal service interfaces exposed by the Application layer to the Web layer.

---

## `IPermissionService`

**Namespace**: `AdminTemplate.Application.Interfaces`  
**Registration**: `services.AddScoped<IPermissionService, PermissionService>()` (already in `InfrastructureServiceExtensions`)

### Methods

#### `GetRolePermissionsAsync`
```
Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(Guid roleId)
```
Returns all object+function pairs currently assigned to the specified role.  
Returns an empty list if no permissions are assigned.  
Never returns `null`.

#### `UserHasPermissionAsync`
```
Task<bool> UserHasPermissionAsync(Guid userId, string objectName, string functionName)
```
Returns `true` if the user's role(s) grant the specified permission, or if the user is a SuperAdmin.  
Returns `false` if:
- The user has no roles.
- None of the user's roles have the specified permission.
- The `objectName` or `functionName` is not found in the loaded configuration.  

Never throws for a valid `Guid userId`; returns `false` on all failure paths.

#### `SaveRolePermissionsAsync`
```
Task SaveRolePermissionsAsync(Guid roleId, IEnumerable<PermissionDto> permissions)
```
Replaces all permissions for the given role with the provided list.  
Passing an empty enumerable removes all permissions from the role.  
Uses a delete-then-insert strategy within a single `SaveChangesAsync` call (atomically via EF Core's Unit of Work).

---

## `IPermissionProvider`

**Namespace**: `AdminTemplate.Application.Providers`  
**Registration**: `services.AddSingleton<IPermissionProvider, JsonPermissionProvider>()` (already registered)

### Methods

#### `GetAll`
```
IReadOnlyList<PermissionObjectDto> GetAll()
```
Returns all permission objects and their functions loaded from `permissions.json` at startup.  
Synchronous; the file is read once on construction and cached in memory.  
Throws `FileNotFoundException` if `permissions.json` is missing at startup.

---

## Extension Method: `PermissionServiceExtensions`

**Namespace**: `AdminTemplate.Application.Services`  
**Location**: `AdminTemplate.Application/Services/PermissionServiceExtensions.cs`

```csharp
public static Task<bool> CurrentUserHasPermissionAsync(
    this IPermissionService service,
    ClaimsPrincipal user,
    string objectName,
    string functionName)
```

Extracts `Guid userId` from `user.FindFirstValue(ClaimTypes.NameIdentifier)`.  
Returns `false` (deny-default) if the user is unauthenticated, or if the NameIdentifier claim is absent or cannot be parsed as a `Guid`.

---

## DTOs

### `PermissionDto`
```csharp
public record PermissionDto(string ObjectName, string FunctionName);
```
Immutable value object. Used as input to `SaveRolePermissionsAsync` and as output from `GetRolePermissionsAsync`.

### `PermissionObjectDto`
```csharp
public record PermissionObjectDto(string Name, string DisplayName, IReadOnlyList<string> Functions);
```
Returned by `IPermissionProvider.GetAll()`. Represents a permission object definition from `permissions.json`.
