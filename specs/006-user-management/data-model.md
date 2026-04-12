# Data Model: PHASE 6 — User Management

**Branch**: `006-user-management`  
**Date**: 2026-04-09  
**Source**: spec.md + research.md  

---

## Entities

### ApplicationUser *(existing — additive change only)*

**Location**: `AdminTemplate.Domain/Entities/ApplicationUser.cs`  
**Base class**: `IdentityUser<Guid>`  
**Status**: Exists. No structural changes required for this phase.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `Guid` | PK, auto-generated | Inherited from `IdentityUser<Guid>` |
| `FullName` | `string` | Required, MaxLength 200 | Added in Phase 1 |
| `Email` | `string` | Required, unique (case-insensitive) | Managed by Identity; also used as `UserName` |
| `IsActive` | `bool` | Default `true` | Soft-delete flag; `false` = cannot log in |
| `CreatedAt` | `DateTime` | UTC, set on creation | Added in Phase 1 |
| `PasswordHash` | `string` | Managed by Identity | Never exposed in DTOs |
| *(Identity fields)* | — | Inherited | `LockoutEnd`, `LockoutEnabled`, `SecurityStamp`, etc. |

**Index**: Existing unique index on `NormalizedEmail` (managed by Identity).

---

### ApplicationRole *(existing — read-only in this phase)*

**Location**: `AdminTemplate.Domain/Entities/ApplicationRole.cs`  
**Base class**: `IdentityRole<Guid>`  
**Status**: Exists. Used in role multi-select on Create/Edit forms. No changes.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Name` | `string` | Display name (e.g., "Admin", "Viewer") |
| `Description` | `string?` | Added in Phase 1 |
| `CreatedAt` | `DateTime` | Added in Phase 1 |

---

### AspNetUserRoles *(existing Identity join table — no changes)*

Managed entirely by `UserManager<ApplicationUser>`. Role assignments are replaced by the service via `RemoveFromRolesAsync` + `AddToRolesAsync`. No direct EF entity is needed for this phase.

---

## DTOs

### UserDto *(existing — no changes required)*

**Location**: `AdminTemplate.Application/DTOs/UserDto.cs`

```
UserDto
├── Id           : string           (GUID as string for JSON/URL safety — R-005)
├── FullName     : string
├── Email        : string
├── IsActive     : bool
├── Roles        : IReadOnlyList<string>   (role names)
└── CreatedAt    : DateTime
```

---

### CreateUserDto *(existing — add IsActive field per R-006)*

**Location**: `AdminTemplate.Application/DTOs/CreateUserDto.cs`  
**Change**: Add `bool IsActive = true` optional parameter.

```
CreateUserDto
├── FullName     : string
├── Email        : string
├── Password     : string           (validated by Identity rules)
├── Roles        : IReadOnlyList<string>
└── IsActive     : bool             ← ADD (default: true)
```

---

### UpdateUserDto *(existing — no changes required)*

**Location**: `AdminTemplate.Application/DTOs/UpdateUserDto.cs`

```
UpdateUserDto
├── FullName     : string
├── Email        : string
├── IsActive     : bool
└── Roles        : IReadOnlyList<string>
```

---

## ViewModels *(new — AdminTemplate.Web/ViewModels/Users/)*

### CreateUserViewModel

Binds the Create User form. Validated via DataAnnotations and `ModelState`.

```
CreateUserViewModel
├── FullName         : string       [Required] [MaxLength(200)]
├── Email            : string       [Required] [EmailAddress]
├── Password         : string       [Required] [MinLength(8)]
├── ConfirmPassword  : string       [Required] [Compare("Password")]
├── SelectedRoles    : List<string> (multi-select; may be empty)
├── IsActive         : bool         [default: true]
└── AvailableRoles   : List<RoleDto>  (populated by controller, not bound on POST)
```

---

### EditUserViewModel

Binds the Edit User form. No password fields.

```
EditUserViewModel
├── Id               : string       [Required] (hidden field)
├── FullName         : string       [Required] [MaxLength(200)]
├── Email            : string       [Required] [EmailAddress]
├── IsActive         : bool
├── SelectedRoles    : List<string>
└── AvailableRoles   : List<RoleDto>  (populated by controller, not bound on POST)
```

---

## DataTable Request/Response *(new or reused from Phase 5)*

### DataTableRequest *(shared — AdminTemplate.Web/Models/DataTableRequest.cs)*

Flat DTO for DataTables.net server-side parameters (R-001).

```
DataTableRequest
├── Draw         : int      (echo back to DataTables for draw counter)
├── Start        : int      (offset — 0-based)
├── Length       : int      (page size)
├── Search       : string?  (search[value])
├── SortColumn   : int      (order[0][column])
└── SortDirection: string   ("asc" / "desc")
```

---

### DataTableResponse\<T\> *(shared — AdminTemplate.Web/Models/DataTableResponse.cs)*

```
DataTableResponse<T>
├── Draw            : int
├── RecordsTotal    : int
├── RecordsFiltered : int
└── Data            : IEnumerable<T>
```

---

## Validation Rules

| Rule | Where enforced |
|---|---|
| Email uniqueness | `UserService.CreateAsync` / `UpdateAsync` via `UserManager` |
| Password complexity (8 chars, upper, digit, special) | ASP.NET Core Identity options (Phase 1 config) |
| FullName required, MaxLength 200 | DataAnnotations on ViewModel + Domain entity |
| ConfirmPassword match | DataAnnotations `[Compare]` on ViewModel |
| Roles list valid (exist in DB) | `UserService` checks against RoleManager before assigning |

---

## State Transitions

```
User Created (IsActive = true)
    │
    ▼
Active ──[DeactivateAsync]──► Inactive (IsActive = false, cannot log in)
    ◄──[ActivateAsync]─────── Inactive
    │
    (no hard delete — record is permanent)
```

---

## Notes

- `ApplicationUser.Id` is `Guid` at the entity level but serialized as a lowercase GUID string in all JSON responses (R-005).
- No new EF Core migrations are required for this phase — all tables (`AspNetUsers`, `AspNetUserRoles`) exist from Phase 1.
- The `DataTableRequest` / `DataTableResponse<T>` models may already exist from Phase 5; if so, reuse them without modification.
