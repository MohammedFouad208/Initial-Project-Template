# Data Model: PHASE 7 — Role Management

**Branch**: `007-role-management`  
**Date**: 2026-04-12  
**Source**: spec.md + research.md

---

## Entities

### ApplicationRole *(existing — no structural changes)*

**Location**: `AdminTemplate.Domain/Entities/ApplicationRole.cs`  
**Base class**: `IdentityRole<Guid>`  
**Status**: Exists from Phase 1. No schema changes required. No new EF migration needed.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `Guid` | PK, auto-generated | Inherited from `IdentityRole<Guid>` |
| `Name` | `string` | Required, unique (case-insensitive via `NormalizedName`) | Managed by Identity |
| `NormalizedName` | `string` | Unique index (managed by Identity) | Used for duplicate detection |
| `Description` | `string?` | MaxLength 500 | Added in Phase 1 |
| `CreatedAt` | `DateTime` | UTC, set on creation | Added in Phase 1 |

**Name length**: Maximum 256 characters (Identity default). The application enforces 100 characters via ViewModel validation (spec FR-012) before it reaches Identity.

---

### AspNetUserRoles *(existing Identity join table — read-only in this phase)*

Managed by Identity's `UserManager<ApplicationUser>`. Used in this phase to:
- Count assigned users per role (R-001 — `IRoleRepository.GetUserCountAsync`)
- Guard role deletion when count > 0 (R-002)

No direct EF entity is exposed. Access is through `UserManager.GetUsersInRoleAsync(roleName)`.

---

### RolePermission *(existing — read-only in this phase)*

**Location**: `AdminTemplate.Domain/Entities/RolePermission.cs`  
**Status**: Exists from Phase 4. Used in this phase only to count permissions per role (R-001).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `RoleId` | `Guid` | FK → AspNetRoles |
| `ObjectName` | `string` | e.g., `"Employee"` |
| `FunctionName` | `string` | e.g., `"Browse"` |

---

## Interface Changes (Additive Only)

### IPermissionRepository — add count method *(R-001)*

**Location**: `AdminTemplate.Domain/Interfaces/IPermissionRepository.cs`

Add one method:
```
Task<int> GetCountByRoleIdAsync(Guid roleId);
```

**Why**: Avoids materializing the full permission list just to get a count for the DataTable.

---

### IRoleRepository — add user count method *(R-001)*

**Location**: `AdminTemplate.Domain/Interfaces/IRoleRepository.cs`

Add one method:
```
Task<int> GetUserCountAsync(Guid roleId);
```

**Why**: Encapsulates the `UserManager.GetUsersInRoleAsync` count query inside the repository layer; avoids `UserManager` leaking into the service.

---

## DTOs

### RoleDto *(existing — no changes)*

**Location**: `AdminTemplate.Application/DTOs/RoleDto.cs`

```
RoleDto
├── Id              : string       (GUID as string for JSON/URL safety)
├── Name            : string
├── Description     : string?
├── UserCount       : int
├── PermissionCount : int
└── CreatedAt       : DateTime
```

---

### CreateRoleDto *(existing — no changes)*

**Location**: `AdminTemplate.Application/DTOs/CreateRoleDto.cs`

```
CreateRoleDto
├── Name        : string    (Required, trimmed before use)
└── Description : string?
```

---

### UpdateRoleDto *(existing — no changes)*

**Location**: `AdminTemplate.Application/DTOs/UpdateRoleDto.cs`

```
UpdateRoleDto
├── Name        : string    (Required, trimmed before use)
└── Description : string?
```

---

## ViewModels (New)

### CreateRoleViewModel

**Location**: `AdminTemplate.Web/ViewModels/Roles/CreateRoleViewModel.cs`

```
CreateRoleViewModel
├── Name        : string    [Required] [MaxLength(100)] [Display("Role Name")]
└── Description : string?   [MaxLength(500)] [Display("Description")]
```

---

### EditRoleViewModel

**Location**: `AdminTemplate.Web/ViewModels/Roles/EditRoleViewModel.cs`

```
EditRoleViewModel
├── Id          : string    (hidden field — route binding)
├── Name        : string    [Required] [MaxLength(100)] [Display("Role Name")]
└── Description : string?   [MaxLength(500)] [Display("Description")]
```

---

## DataTable Row Shape

The `RolesController.GetData` endpoint returns `DataTableResponse<RoleRowDto>` where `RoleRowDto` is a local record defined in the controller or a shared DTO:

```
RoleRowDto
├── Id              : string       (GUID as string)
├── Name            : string
├── Description     : string?
├── UserCount       : int
├── PermissionCount : int
└── CreatedAt       : DateTime
```

`RoleRowDto` is identical in shape to `RoleDto`. The controller can use `RoleDto` directly without defining a separate row DTO.
