# Implementation Plan: Permission Engine

**Branch**: `004-permission-engine` | **Date**: 2026-04-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-permission-engine/spec.md`

## Summary

Implement the JSON-driven permission engine for the ASP.NET Core admin template. All scaffolding (entities, interfaces, repositories, JSON provider, DI registrations, `permissions.json`) was laid down in Phase 1. Phase 4 fills in the three remaining gaps: (1) completing the `PermissionService` implementation; (2) creating the `[HasPermission]` authorization filter attribute that enforces object+function access control on controller actions; and (3) exposing permission checking to Razor views for conditional UI rendering. No new database migration is required.

## Technical Context

**Language/Version**: C# 12 / ASP.NET Core MVC .NET 8  
**Primary Dependencies**: ASP.NET Core Identity (built-in — `UserManager<ApplicationUser>`), Entity Framework Core 8.x, Bootstrap 5.3 (CDN)  
**Storage**: SQL Server 2019+ via EF Core / `ApplicationDbContext` (already provisioned)  
**Testing**: Manual browser verification (no automated test runner configured)  
**Target Platform**: Web browser — desktop admin interface  
**Project Type**: ASP.NET Core MVC web application  
**Performance Goals**: Permission check on every protected request must not introduce perceptible latency — achieved via a single-query SQL JOIN in `PermissionRepository.UserHasPermissionAsync`  
**Constraints**: No `dotnet ef` CLI; no inline styles; no business logic in controllers; no hardcoded role/object/function names except the `"SuperAdmin"` bypass constant in `PermissionService`  
**Scale/Scope**: ~10 protected controllers across the full nine-phase template; 4 permission objects × up to 5 functions = 20 possible assignments per role

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Clean Architecture — no cross-layer leakage | ✅ PASS | `HasPermissionAttribute` (Web) resolves `IPermissionService` (Application interface) via DI — no direct Infrastructure reference. `PermissionService` (Application) depends only on `IUserRepository` and `IPermissionRepository` (Domain interfaces). |
| II. Dependency Inversion — Web depends on Application abstractions | ✅ PASS | Filter injects `IPermissionService`, not `PermissionService` directly. `IPermissionProvider` already registered as singleton in Infrastructure extensions. |
| III. Thin Controllers — no business logic | ✅ PASS | Permission logic lives entirely in `PermissionService` and `HasPermissionAttribute`. Controllers will simply carry `[HasPermission(...)]` attributes. |
| IV. Repository + Unit of Work — all data via repositories | ✅ PASS | `PermissionService` calls `IPermissionRepository` and `IUserRepository`. No raw `DbContext` usage in Application or Web layers. |
| V. JSON-Driven Permissions — no hardcoded objects/functions | ✅ PASS | `permissions.json` is the sole source of object/function definitions. `PermissionService` uses only the names passed to it as string arguments — no compile-time constants for object or function names. The only constant is `"SuperAdmin"` (a role name, not a permission object/function). |
| VI. Generic DataTable — no per-page init | ✅ PASS | No DataTable used in Phase 4. |
| VII. Permission-Aware UI | ✅ PASS | `@inject IPermissionService` + `CurrentUserHasPermissionAsync` extension enables permission-aware conditional rendering in all views. |
| Security — `[Authorize]` + `[HasPermission]` on admin routes | ✅ PASS | `HasPermissionAttribute` challenges unauthenticated users (covers the Authorize concern). Existing `FallbackPolicy` in `Program.cs` also requires authentication globally. |
| Security — CSRF on all POSTs | ✅ PASS | Phase 4 adds no POST actions; `SaveRolePermissionsAsync` is a service method called by Phase 8 controller. CSRF requirement will be verified at Phase 8. |
| No inline styles | ✅ PASS | `Error403.cshtml` uses Bootstrap utility classes and references `_Layout.cshtml`; no inline style attributes. |
| Naming — Controllers plural noun | N/A | No new controllers in this phase. |
| Migrations via PM Console only | ✅ PASS | No migration needed; `RolePermissions` table already exists from Phase 1. |

**Result**: All applicable gates pass. Proceeding to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
AdminTemplate.Domain/
└── Interfaces/
    ├── IUserRepository.cs           MODIFY — add GetRoleNamesAsync(Guid userId)
    └── IPermissionRepository.cs     MODIFY — add UserHasPermissionAsync(Guid, string, string)

AdminTemplate.Application/
└── Services/
    ├── PermissionService.cs         MODIFY — implement all 3 methods; add SuperAdmin constant
    └── PermissionServiceExtensions.cs   NEW — CurrentUserHasPermissionAsync extension method

AdminTemplate.Infrastructure/
└── Repositories/
    ├── UserRepository.cs            MODIFY — implement GetRoleNamesAsync
    └── PermissionRepository.cs      MODIFY — implement UserHasPermissionAsync

AdminTemplate.Web/
├── Filters/
│   └── HasPermissionAttribute.cs    NEW — IAsyncAuthorizationFilter
└── Views/
    ├── Shared/
    │   └── Error403.cshtml          NEW — styled 403 Access Denied view
    └── _ViewImports.cshtml          MODIFY — add @inject IPermissionService + @using for extension method
```

**Structure Decision**: ASP.NET Core MVC multi-project clean architecture (already established). Phase 4 adds one new file to Web (`HasPermissionAttribute.cs`, `Error403.cshtml`) and one to Application (`PermissionServiceExtensions.cs`). All other changes are additive modifications to existing files.

## Complexity Tracking

No constitution violations to justify.

## Post-Design Constitution Check

*Re-evaluated after Phase 1 design artifacts (`data-model.md`, `contracts/`, `quickstart.md`) are complete.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Clean Architecture | ✅ PASS | `HasPermissionAttribute` (Web) → `IPermissionService` (Application) → `IUserRepository` / `IPermissionRepository` (Domain) → `UserRepository` / `PermissionRepository` (Infrastructure). No layer skip. |
| II. Dependency Inversion | ✅ PASS | Filter resolves `IPermissionService` interface (Application) from DI — never references `PermissionService` concretely. `PermissionService` never references Infrastructure types directly. |
| III. Thin Controllers | ✅ PASS | Controllers carry `[HasPermission]` as a declarative attribute. All permission logic is in the filter and service. |
| IV. Repository + Unit of Work | ✅ PASS | `SaveRolePermissionsAsync` uses `DeleteByRoleIdAsync` then `AddRangeAsync` — both operations call `SaveChangesAsync` in separate steps via existing repository methods. Self-contained per call. |
| V. JSON-Driven Permissions | ✅ PASS | `permissions.json` contains all objects and functions. `PermissionService` uses string arguments passed at call site — no compile-time permission object/function constants. `"SuperAdmin"` is a role name constant, not a permission definition — outside the scope of V. |
| VI. Generic DataTable | ✅ PASS | No DataTable usage in Phase 4. |
| VII. Permission-Aware UI | ✅ PASS | `CurrentUserHasPermissionAsync` extension + `@inject` in `_ViewImports.cshtml` satisfies this gate. Usage shown in `quickstart.md` Step 7 and `contracts/permission-service-api.md`. |
| Security — Filter handles auth/authz correctly | ✅ PASS | `ChallengeResult` for unauthenticated; `ViewResult(403)` for authorized-but-denied. Confirmed in `contracts/has-permission-attribute.md`. |
| No inline styles | ✅ PASS | `Error403.cshtml` uses only Bootstrap utility classes (`text-muted`, `btn-primary`, `py-5`, etc.) and Font Awesome icon class. No `style=` attributes. |
| Deny-by-default for unknown permissions | ✅ PASS | `PermissionServiceExtensions.CurrentUserHasPermissionAsync` returns `false` on unauthenticated, missing claim, or no matching row. `PermissionRepository.UserHasPermissionAsync` returns `false` via `AnyAsync` if no matching row exists. |

**Result**: All gates pass post-design. Ready for `/speckit.tasks`.
