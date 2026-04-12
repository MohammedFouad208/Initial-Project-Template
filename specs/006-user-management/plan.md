# Implementation Plan: PHASE 6 — User Management

**Branch**: `006-user-management` | **Date**: 2026-04-11 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/006-user-management/spec.md`

## Summary

Build the full User Management module on top of the existing `ApplicationUser` entity, `IUserService` interface, and `HasPermissionAttribute` infrastructure. The module exposes a server-side DataTable list of users (with initials avatars, role badges, and status badges), a Create form (with password + multi-role assignment), an Edit form (no password), and an inline Activate/Deactivate toggle. All CRUD actions are gated by `[HasPermission("User", "...")]`. The `UserService` and `UserRepository` stubs from Phase 1 are completed in this phase; no new EF migrations are needed.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8  
**Primary Dependencies**: ASP.NET Core MVC 8, ASP.NET Core Identity (built-in), Entity Framework Core 8, Bootstrap 5.3, DataTables.net 1.13+, Font Awesome 6.7+  
**Storage**: SQL Server 2019+ via EF Core — existing `AspNetUsers` and `AspNetUserRoles` tables; no new migrations required  
**Testing**: Manual integration testing against the running application (no automated test project in scope per constitution)  
**Target Platform**: ASP.NET Core 8 web server, modern browser (Chrome/Edge/Firefox latest)  
**Project Type**: Web application — MVC module within an existing multi-project clean-architecture solution  
**Performance Goals**: First page of users list < 1 second for up to 10,000 rows; activate/deactivate round-trip < 1 second  
**Constraints**: CSRF on all POST actions; permission gate on every action; no hard delete; no direct `DbContext` in Application or Web layers; no hardcoded colors; CSS logical properties only for RTL support  
**Scale/Scope**: Single module — 1 controller, 3 views, 2 ViewModels, completion of 2 service/repository stubs, 2 shared DataTable models

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|---|---|---|
| I. Clean Architecture — Domain → Application → Infrastructure → Web | ✅ PASS | `UsersController` depends on `IUserService` (Application). `UserService` depends on `UserManager<ApplicationUser>` (Infrastructure) via constructor injection. No cross-layer leakage. |
| II. Dependency Inversion — Web depends on Application abstractions | ✅ PASS | Controller receives `IUserService`, `IRoleService`, `IPermissionService` — all Application interfaces. |
| III. Thin Controllers — no business logic | ✅ PASS | Controllers only bind input, call service methods, and return views. All validation, role assignment logic, and deactivation logic lives in `UserService`. |
| IV. Repository + Unit of Work — no direct DbContext in App/Web | ✅ PASS | `UserService` uses `UserManager<ApplicationUser>` (Identity abstraction) + `IUserRepository`. No `ApplicationDbContext` reference outside Infrastructure. |
| V. JSON-Driven Permissions — no hardcoded objects/functions | ✅ PASS | `[HasPermission("User", "Browse")]` etc. reference the `"User"` object name from `permissions.json`. No permission string is duplicated in code beyond the attribute parameters. |
| VI. Generic DataTable — no per-page initialization | ✅ PASS | `Views/Users/Index.cshtml` calls `AppDataTable.init({...})` once. No direct DataTables.net jQuery init on the page. |
| VII. Permission-Aware UI — buttons hidden by permission | ✅ PASS | `canCreate`, `canUpdate`, `canDelete` are serialized from `IPermissionService` and passed to `AppDataTable.init` config. |
| VIII. Design System First — no hardcoded colors, logical properties only | ✅ PASS | All views use `var(--...)` tokens, `.badge-soft-*`, `.btn-primary`, `.page-header`, `.field-icon-wrap`; no `margin-left`/`padding-right` in new CSS. |
| Naming conventions | ✅ PASS | `UsersController`, `CreateUserViewModel`, `EditUserViewModel`, `IUserService`, `UserService` — all follow established convention. |
| Forbidden patterns | ✅ PASS | No `dotnet ef` CLI, no `@Html.Partial()`, no `d-flex` on `<body>`, no inline styles. |

**POST-DESIGN RE-CHECK**: All gates pass after reviewing `data-model.md`. No violations detected. No complexity justifications required.

---

## Project Structure

### Documentation (this feature)

```text
specs/006-user-management/
├── plan.md              ← this file
├── research.md          ← Phase 0 output (complete)
├── data-model.md        ← Phase 1 output (complete)
├── quickstart.md        ← Phase 1 output
├── contracts/
│   └── users-controller.md   ← Phase 1 output
└── tasks.md             ← Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
AdminTemplate.Domain/
└── (no changes — ApplicationUser, IUserRepository already exist)

AdminTemplate.Application/
├── DTOs/
│   └── CreateUserDto.cs           ← ADD IsActive field
└── Services/
    └── UserService.cs             ← COMPLETE all NotImplementedException stubs

AdminTemplate.Infrastructure/
└── Repositories/
    └── UserRepository.cs          ← COMPLETE all NotImplementedException stubs

AdminTemplate.Web/
├── Controllers/
│   └── UsersController.cs         ← NEW
├── ViewModels/
│   └── Users/
│       ├── CreateUserViewModel.cs ← NEW
│       └── EditUserViewModel.cs   ← NEW
├── Models/
│   ├── DataTableRequest.cs        ← NEW (or reuse from Phase 5 if exists)
│   └── DataTableResponse.cs       ← NEW (or reuse from Phase 5 if exists)
└── Views/
    └── Users/
        ├── Index.cshtml           ← NEW
        ├── Create.cshtml          ← NEW
        └── Edit.cshtml            ← NEW
```

**Structure Decision**: Clean Architecture multi-project layout. MVC artifacts are in `AdminTemplate.Web`; service completion is in `Application` + `Infrastructure`. No new projects and no new EF migrations required.

---

## Complexity Tracking

*No Constitution violations detected. All established patterns are followed.*
