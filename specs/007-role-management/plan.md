# Implementation Plan: PHASE 7 — Role Management

**Branch**: `007-role-management` | **Date**: 2026-04-12 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-role-management/spec.md`

## Summary

Build the full Role Management module on top of the existing `ApplicationRole` entity, `IRoleService` interface, `IRoleRepository` interface, and `HasPermissionAttribute` infrastructure. The module exposes a server-side DataTable list of roles (with user count and permission count columns), a Create form, an Edit form, and a guarded Delete flow that blocks deletion when users are assigned. A "Manage Permissions" button per row navigates to the Phase 8 Role Permissions page. All CRUD actions are gated by `[HasPermission("Role", "...")]`. The `RoleService` and `RoleRepository` stubs from Phase 1 are completed in this phase; no new EF migrations are required.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8
**Primary Dependencies**: ASP.NET Core MVC 8, ASP.NET Core Identity (built-in), Entity Framework Core 8, Bootstrap 5.3, DataTables.net 1.13+, Font Awesome 6.7+
**Storage**: SQL Server 2019+ via EF Core — existing `AspNetRoles`, `AspNetUserRoles`, and `RolePermissions` tables; no new migrations required
**Testing**: Manual integration testing against the running application (no automated test project in scope per constitution)
**Target Platform**: ASP.NET Core 8 web server, modern browser (Chrome/Edge/Firefox latest)
**Project Type**: Web application — MVC module within an existing multi-project clean-architecture solution
**Performance Goals**: First page of roles list < 1 second for up to 1,000 roles; delete guard check < 500 ms
**Constraints**: CSRF on all POST actions; permission gate on every action; no direct `DbContext` in Application or Web layers; no hardcoded colors; CSS logical properties only; SuperAdmin role is undeletable
**Scale/Scope**: Single module — 1 controller, 3 views, 2 ViewModels, completion of 2 service/repository stubs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|---|---|---|
| I. Clean Architecture — Domain → Application → Infrastructure → Web | PASS | `RolesController` depends on `IRoleService` (Application). `RoleService` depends on `RoleManager<ApplicationRole>` (Infrastructure) via constructor injection. No cross-layer leakage. |
| II. Dependency Inversion — Web depends on Application abstractions | PASS | Controller receives `IRoleService` and `IPermissionService` — all Application interfaces. |
| III. Thin Controllers — no business logic | PASS | Controllers only bind input, call service methods, and return views. Duplicate-name check and delete guard live in `RoleService`. |
| IV. Repository + Unit of Work — no direct DbContext in App/Web | PASS | `RoleService` uses `IRoleRepository`. `RoleRepository` uses `RoleManager<ApplicationRole>` and `UserManager<ApplicationUser>`. No `ApplicationDbContext` reference outside Infrastructure. |
| V. JSON-Driven Permissions — no hardcoded objects/functions | PASS | `[HasPermission("Role", "Browse")]` etc. reference the `"Role"` object name from `permissions.json`. No permission string is duplicated beyond the attribute parameters. |
| VI. Generic DataTable — no per-page initialization | PASS | `Views/Roles/Index.cshtml` calls `AppDataTable.init({...})` once. No direct DataTables.net jQuery init on the page. |
| VII. Permission-Aware UI — buttons hidden by permission | PASS | `canCreate`, `canUpdate`, `canDelete`, `canManagePermissions` are serialized from `IPermissionService` and passed to `AppDataTable.init` config. |
| VIII. Design System First — no hardcoded colors, logical properties only | PASS | All views use `var(--...)` tokens, `.badge-soft-*`, `.btn-primary`, `.btn-outline-primary`, `.btn-outline-danger`, `.page-header`, `.field-icon-wrap`; no `margin-left`/`padding-right` in new CSS. |
| Naming conventions | PASS | `RolesController`, `CreateRoleViewModel`, `EditRoleViewModel`, `IRoleService`, `RoleService` — all follow established convention. |
| Forbidden patterns | PASS | No `dotnet ef` CLI, no `@Html.Partial()`, no `d-flex` on `<body>`, no inline styles. |

**POST-DESIGN RE-CHECK**: All gates pass after reviewing `data-model.md`. No violations detected. No complexity justifications required.

---

## Project Structure

### Documentation (this feature)

```
specs/007-role-management/
├── plan.md          ← this file
├── research.md      ← Phase 0 output (complete)
├── data-model.md    ← Phase 1 output (complete)
├── quickstart.md    ← Phase 1 output
├── contracts/
│   └── roles-controller.md   ← Phase 1 output
└── tasks.md         ← Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```
AdminTemplate.Domain/
└── (no changes — ApplicationRole, IRoleRepository already exist)

AdminTemplate.Application/
└── Services/
    └── RoleService.cs          ← COMPLETE all NotImplementedException stubs

AdminTemplate.Infrastructure/
└── Repositories/
    └── RoleRepository.cs       ← COMPLETE all NotImplementedException stubs

AdminTemplate.Web/
├── Controllers/
│   └── RolesController.cs      ← NEW
├── ViewModels/
│   └── Roles/
│       ├── CreateRoleViewModel.cs  ← NEW
│       └── EditRoleViewModel.cs    ← NEW
└── Views/
    └── Roles/
        ├── Index.cshtml        ← NEW
        ├── Create.cshtml       ← NEW
        └── Edit.cshtml         ← NEW
```

**Structure Decision**: Clean Architecture multi-project layout. MVC artifacts are in `AdminTemplate.Web`; service completion is in `Application` + `Infrastructure`. No new projects and no new EF migrations required. `DataTableRequest.cs`, `DataTableResponse.cs`, and `_DeleteConfirmModal.cshtml` already exist from Phase 5/6 and are reused as-is.

---

## Complexity Tracking

*No Constitution violations detected. All established patterns are followed.*
