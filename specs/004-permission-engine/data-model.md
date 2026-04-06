# Data Model: Permission Engine

**Feature Branch**: `004-permission-engine`  
**Date**: 2026-04-06

---

## New Entities

Phase 4 introduces **no new database entities** and **no new migrations**. The `RolePermissions` table and its full schema (composite unique index, FK with CASCADE DELETE) were provisioned in Phase 1. The `permissions.json` configuration file already exists at `AdminTemplate.Web/Config/permissions.json`.

---

## Interface Extensions

Phase 4 extends two existing Domain interfaces to support the permission engine's query requirements.

### `IUserRepository` — new method

**Location**: `AdminTemplate.Domain/Interfaces/IUserRepository.cs`

| Method Signature | Return Type | Description |
|-----------------|-------------|-------------|
| `GetRoleNamesAsync(Guid userId)` | `Task<IReadOnlyList<string>>` | Returns the names of all roles assigned to a user. Used by `PermissionService` for the SuperAdmin bypass check. |

**Implementation note**: `UserRepository` implements this by calling `UserManager<ApplicationUser>.FindByIdAsync(userId.ToString())` then `UserManager<ApplicationUser>.GetRolesAsync(user)`. Returns an empty list if the user is not found.

---

### `IPermissionRepository` — new method

**Location**: `AdminTemplate.Domain/Interfaces/IPermissionRepository.cs`

| Method Signature | Return Type | Description |
|-----------------|-------------|-------------|
| `UserHasPermissionAsync(Guid userId, string objectName, string functionName)` | `Task<bool>` | Checks in a single SQL JOIN whether any of the user's roles grant the specified object+function. |

**SQL logic** (executed in `PermissionRepository` via EF Core):
```
AspNetUserRoles ⟕ RolePermissions ON UserRoles.RoleId = RolePermissions.RoleId
WHERE UserRoles.UserId = @userId
  AND RolePermissions.ObjectName = @objectName
  AND RolePermissions.FunctionName = @functionName
```

This avoids N+1 queries (one per role) and is the hot path for every protected page request.

---

## New Application Layer Types

### `PermissionServiceExtensions`

**Location**: `AdminTemplate.Application/Services/PermissionServiceExtensions.cs`

A static extension class on `IPermissionService` that extracts the current user's ID from a `ClaimsPrincipal` and delegates to `UserHasPermissionAsync`.

| Method Signature | Return Type | Description |
|-----------------|-------------|-------------|
| `CurrentUserHasPermissionAsync(this IPermissionService, ClaimsPrincipal user, string objectName, string functionName)` | `Task<bool>` | Parses the `NameIdentifier` claim to a `Guid` and calls `UserHasPermissionAsync`. Returns `false` if unauthenticated or if the claim is missing/malformed. |

**Usage in Razor views** (once `@inject` is declared in `_ViewImports.cshtml`):
```razor
@if (await PermissionService.CurrentUserHasPermissionAsync(User, "Employee", "Export"))
{
    <button class="btn btn-sm btn-outline-primary">Export</button>
}
```

---

## Existing Entities Used

| Entity | Source Project | Role in this Phase |
|--------|----------------|--------------------|
| `ApplicationUser` | `AdminTemplate.Domain` | Source of user ID passed to permission checks |
| `ApplicationRole` | `AdminTemplate.Domain` | Role FK target in `RolePermissions`; role name used for SuperAdmin bypass |
| `RolePermission` | `AdminTemplate.Domain` | Persisted object+function assignment for a role — read and written by `PermissionService` |

---

## Configuration

| File | Location | Role |
|------|----------|------|
| `permissions.json` | `AdminTemplate.Web/Config/permissions.json` | Source of truth for all available permission objects and their functions; loaded at startup by `JsonPermissionProvider` (already implemented) |

The JSON structure (unchanged from Phase 1):
```json
{
  "PermissionObjects": [
    { "Name": "Employee", "DisplayName": "Employees", "Functions": ["Browse","Create","Update","Delete","Export"] },
    { "Name": "User",     "DisplayName": "Users",     "Functions": ["Browse","Create","Update","Delete"] },
    { "Name": "Role",     "DisplayName": "Roles",     "Functions": ["Browse","Create","Update","Delete","AssignPermissions"] },
    { "Name": "Report",   "DisplayName": "Reports",   "Functions": ["Browse","Export"] }
  ]
}
```

---

## State Transitions: Role Permissions

The `SaveRolePermissionsAsync` operation implements a **replace-all** strategy:

```
Current state: RolePermissions for RoleId X = { (Employee, Browse), (Employee, Create) }

Operation: SaveRolePermissionsAsync(X, [(Employee, Browse), (Employee, Export)])

Step 1 — Delete: DELETE FROM RolePermissions WHERE RoleId = X
Step 2 — Insert: INSERT (X, Employee, Browse), (X, Employee, Export)

Final state: { (Employee, Browse), (Employee, Export) }
```

No partial update or merge strategy is used. This ensures no orphaned entries remain after each save.
