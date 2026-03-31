# Data Model: Foundation & Project Scaffold

**Branch**: `001-foundation-scaffold`
**Date**: 2026-03-29
**Source**: spec.md (Key Entities section) + research.md decisions R-002, R-003, R-004

---

## Entities

### BaseEntity

Abstract base class for all non-Identity domain entities.

| Property | Type | Constraints | Notes |
|----------|------|-------------|-------|
| `Id` | `Guid` | NOT NULL, PK | Initialized to `Guid.NewGuid()` at construction |
| `CreatedAt` | `DateTime` | NOT NULL | UTC; initialized to `DateTime.UtcNow` at construction |

**Inheritors in this phase**: `RolePermission`
**NOT used by**: `ApplicationUser`, `ApplicationRole` — these inherit from Identity base classes instead.

---

### ApplicationUser

Extends `IdentityUser<string>`. Maps to the existing `AspNetUsers` table; EF Core adds the extra columns via migration.

| Property | Type | Constraints | Notes |
|----------|------|-------------|-------|
| `Id` (inherited) | `string` | NOT NULL, PK | Identity default |
| `UserName` (inherited) | `string` | NOT NULL, unique | Used as login name (email) |
| `Email` (inherited) | `string` | NOT NULL, unique | Primary contact |
| `PasswordHash` (inherited) | `string` | NOT NULL | Managed by Identity |
| `FullName` | `string` | NOT NULL, max 200 | Display name for UI |
| `IsActive` | `bool` | NOT NULL, default `true` | Soft-disable without deleting |
| `CreatedAt` | `DateTime` | NOT NULL | UTC; set at creation |

**Relationships**:
- Many-to-many with `ApplicationRole` via `AspNetUserRoles` (Identity-managed join table)

**Validation rules**:
- `FullName` must not be empty or whitespace
- `IsActive` defaults to `true` on new users
- `CreatedAt` is set once at creation and never updated

**State transitions**:
```
Active (IsActive=true) ──→ Deactivated (IsActive=false)
Deactivated            ──→ Active (reactivation supported)
```

---

### ApplicationRole

Extends `IdentityRole<string>`. Maps to `AspNetRoles`; EF Core adds the extra columns via migration.

| Property | Type | Constraints | Notes |
|----------|------|-------------|-------|
| `Id` (inherited) | `string` | NOT NULL, PK | Identity default |
| `Name` (inherited) | `string` | NOT NULL, unique | Role display name (e.g., "SuperAdmin") |
| `NormalizedName` (inherited) | `string` | NOT NULL, unique | Upper-cased for lookups |
| `Description` | `string` | NULLABLE, max 500 | Human-readable purpose of the role |
| `CreatedAt` | `DateTime` | NOT NULL | UTC; set at creation |

**Relationships**:
- Many-to-many with `ApplicationUser` via `AspNetUserRoles`
- One-to-many with `RolePermission` (a role has many permission assignments)

**Validation rules**:
- `Name` must be unique across roles
- `Description` is optional
- Cannot be deleted while any user is assigned to the role (enforced at service layer)

---

### RolePermission

Inherits `BaseEntity`. Maps to the custom `RolePermissions` table (not part of Identity schema).

| Property | Type | Constraints | Notes |
|----------|------|-------------|-------|
| `Id` (from BaseEntity) | `Guid` | NOT NULL, PK | App-generated |
| `CreatedAt` (from BaseEntity) | `DateTime` | NOT NULL | UTC |
| `RoleId` | `string` | NOT NULL, FK → `AspNetRoles.Id` | Cascade delete from role |
| `ObjectName` | `string` | NOT NULL, max 100 | Matches `Name` in `permissions.json` (e.g., "Employee") |
| `FunctionName` | `string` | NOT NULL, max 100 | Matches function in `permissions.json` (e.g., "Create") |

**Unique constraint**: `(RoleId, ObjectName, FunctionName)` — a role cannot have duplicate permission entries for the same object-function pair.

**Relationships**:
- Many-to-one with `ApplicationRole` (navigated via `RoleId`)
- No direct navigation to `ApplicationUser` — user permissions are resolved through their roles

**Validation rules**:
- `ObjectName` and `FunctionName` must be non-empty strings matching a valid entry in `permissions.json`
- Pair `(RoleId, ObjectName, FunctionName)` must be unique (duplicate enforcement at DB constraint + service layer)

---

## Database Schema Summary

```
AspNetUsers
  Id                    string  PK
  UserName              string  UNIQUE NOT NULL
  Email                 string  UNIQUE NOT NULL
  EmailConfirmed        bit
  PasswordHash          string
  ...                   (other Identity columns)
  FullName              nvarchar(200)  NOT NULL
  IsActive              bit            NOT NULL  DEFAULT 1
  CreatedAt             datetime2      NOT NULL

AspNetRoles
  Id                    string  PK
  Name                  string  UNIQUE NOT NULL
  NormalizedName        string  UNIQUE NOT NULL
  ConcurrencyStamp      string
  Description           nvarchar(500)  NULL
  CreatedAt             datetime2      NOT NULL

AspNetUserRoles
  UserId                string  PK, FK → AspNetUsers.Id
  RoleId                string  PK, FK → AspNetRoles.Id

RolePermissions
  Id                    uniqueidentifier  PK
  RoleId                string            NOT NULL  FK → AspNetRoles.Id  ON DELETE CASCADE
  ObjectName            nvarchar(100)     NOT NULL
  FunctionName          nvarchar(100)     NOT NULL
  CreatedAt             datetime2         NOT NULL
  UNIQUE (RoleId, ObjectName, FunctionName)
```

---

## EF Core Configuration Notes

- `ApplicationDbContext` inherits from `IdentityDbContext<ApplicationUser, ApplicationRole, string>`.
- `RolePermission` entity configured via Fluent API in `OnModelCreating`:
  - `HasKey(rp => rp.Id)`
  - `HasOne<ApplicationRole>().WithMany().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade)`
  - `HasIndex(rp => new { rp.RoleId, rp.ObjectName, rp.FunctionName }).IsUnique()`
- `ApplicationUser` extra properties mapped by convention (EF Core detects inherited properties automatically).
- `ApplicationRole` extra properties mapped by convention.
- No explicit table name overrides — Identity defaults are used (`AspNetUsers`, `AspNetRoles`, etc.).

---

## permissions.json Reference (Seed Source)

The seeder reads the following structure from `AdminTemplate.Web/Config/permissions.json`:

```json
{
  "PermissionObjects": [
    { "Name": "Employee", "DisplayName": "Employees",  "Functions": ["Browse","Create","Update","Delete","Export"] },
    { "Name": "User",     "DisplayName": "Users",      "Functions": ["Browse","Create","Update","Delete"] },
    { "Name": "Role",     "DisplayName": "Roles",      "Functions": ["Browse","Create","Update","Delete","AssignPermissions"] },
    { "Name": "Report",   "DisplayName": "Reports",    "Functions": ["Browse","Export"] }
  ]
}
```

Total permission rows seeded for SuperAdmin: **16** (5 + 4 + 5 + 2).
