# Data Model: Polish, Guards & Seed Data

**Feature**: `009-polish-guards-seed`  
**Generated**: 2026-04-12 (Phase 1)

---

## Entity Changes

**No new entities are introduced in this phase.** All entities from previous phases remain unchanged:

| Entity | Source Phase | Change |
|---|---|---|
| `ApplicationUser` | Phase 1 | None |
| `ApplicationRole` | Phase 1 | None |
| `RolePermission` | Phase 4 | None |

---

## Seed Data Records

Phase 9 extends `DataSeeder.cs` to produce the following records idempotently. All records are inserted only if they do not already exist.

### Roles

| Role Name | Description | Created By Phase |
|---|---|---|
| `SuperAdmin` | Full system access | Phase 1 (existing) |
| `Admin` | Browse + Create + Update on all objects | **Phase 9 (new)** |
| `Viewer` | Browse only on all objects | **Phase 9 (new)** |

### Users

| Email | Full Name | Role | Password | Created By Phase |
|---|---|---|---|---|
| `superadmin@admintemplate.local` | Super Administrator | SuperAdmin | `Admin@1234!` | Phase 1 (existing) |
| `admin@admintemplate.local` | Demo Admin | Admin | `Admin@1234!` | **Phase 9 (new)** |
| `viewer@admintemplate.local` | Demo Viewer | Viewer | `Viewer@1234!` | **Phase 9 (new)** |

### Permission Sets (derived from `permissions.json` at seed time)

Objects defined in current `permissions.json`: Employee, User, Role, Report

**Admin role permissions** (Browse, Create, Update per object):

| Object | Functions Assigned |
|---|---|
| Employee | Browse, Create, Update |
| User | Browse, Create, Update |
| Role | Browse, Create, Update |
| Report | Browse |

**Viewer role permissions** (Browse only):

| Object | Functions Assigned |
|---|---|
| Employee | Browse |
| User | Browse |
| Role | Browse |
| Report | Browse |

> **Note**: Permission sets are not hardcoded. The seeder reads all objects from `IPermissionProvider` and filters to the whitelist `["Browse", "Create", "Update"]` (Admin) or `["Browse"]` (Viewer). Adding objects to `permissions.json` automatically grants them correct access on next seed run.

---

## State Transitions

No state machine changes. Existing `IsActive` / `EmailConfirmed` flags on `ApplicationUser` remain unchanged in behavior.

---

## Configuration Data

### appsettings.json additions

**New `Smtp` section** (shape only — no DB table):

| Key | Type | Description |
|---|---|---|
| `Smtp:Host` | string | SMTP server hostname |
| `Smtp:Port` | int | SMTP server port (typically 587) |
| `Smtp:UseSsl` | bool | Whether to use SSL/TLS |
| `Smtp:UserName` | string | SMTP authentication username |
| `Smtp:Password` | string | SMTP authentication password |
| `Smtp:FromAddress` | string | Sender email address |
| `Smtp:FromDisplayName` | string | Sender display name |

**New `Seed` keys** (for demo users):

| Key | Type | Description |
|---|---|---|
| `Seed:AdminEmail` | string | Email for demo Admin user |
| `Seed:AdminPassword` | string | Password for demo Admin user |
| `Seed:ViewerEmail` | string | Email for demo Viewer user |
| `Seed:ViewerPassword` | string | Password for demo Viewer user |
