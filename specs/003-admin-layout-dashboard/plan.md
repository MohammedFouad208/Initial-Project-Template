# Implementation Plan: Admin Layout & Dashboard

**Branch**: `003-admin-layout-dashboard` | **Date**: 2026-04-02 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-admin-layout-dashboard/spec.md`

## Summary

Build the shared admin shell (sidebar + top navigation bar) used by all authenticated pages, backed by a `DashboardController` that serves a home page with three live stat cards (total users, total roles, active sessions). The existing default `_Layout.cshtml` is replaced with a full sidebar+topnav structure; CSS variables and stylesheet files are introduced to enforce the `#004D82` brand token. Responsive collapse behaviour on mobile is handled with Bootstrap 5 offcanvas and a small inline script.

## Technical Context

**Language/Version**: C# 12 / ASP.NET Core MVC .NET 8  
**Primary Dependencies**: Bootstrap 5.3 (CDN), Font Awesome 6.x (CDN), ASP.NET Core Identity (already wired)  
**Storage**: SQL Server via Entity Framework Core 8 / `ApplicationDbContext` (already provisioned in Phase 1)  
**Testing**: Manual browser verification (no automated test runner configured in this project)  
**Target Platform**: Web browser — desktop + mobile (responsive)  
**Project Type**: ASP.NET Core MVC web application  
**Performance Goals**: Page load < 2 s on a local development server; no explicit production SLA for this phase  
**Constraints**: No inline styles — all colour/spacing via CSS custom properties; no `dotnet ef` CLI; no per-page layout duplication  
**Scale/Scope**: Single shared layout file consumed by ~10 admin pages across the full nine-phase template

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Clean Architecture — no cross-layer leakage | ✅ PASS | `DashboardController` calls `IUserService`/`IRoleService` interfaces (Application layer); no raw DbContext in Web |
| II. Dependency Inversion — Web depends on Application abstractions | ✅ PASS | Stat counts fetched through existing `IUserService`/`IRoleService`; no Infrastructure reference from controller |
| III. Thin Controllers — no business logic | ✅ PASS | Controller queries counts via service, maps to ViewModel, returns View — nothing else |
| IV. Repository + Unit of Work | ✅ PASS | Phase 1 repositories already in place; this phase adds no new data-access paths |
| V. JSON-Driven Permissions | ✅ PASS | Sidebar nav items are static HTML in this phase; permission-based hiding deferred to Phase 9 per spec assumption |
| VI. Generic DataTable — no per-page init | ✅ PASS | No DataTable used in Phase 3; deferred to Phase 5 |
| VII. Permission-Aware UI | ✅ PASS | No per-user button hiding needed on layout shell; deferred to Phase 9 |
| Security — `[Authorize]` on admin routes | ✅ PASS | `DashboardController` will carry `[Authorize]`; fallback policy already set in `Program.cs` |
| Security — CSRF on all POSTs | ✅ PASS | Only GET actions in this phase (logout is a POST — will use `[ValidateAntiForgeryToken]`) |
| No inline styles | ✅ PASS | All styles will use CSS custom properties; `_AuthLayout.cshtml` has an inline block which is pre-existing (not touched by this phase) |
| Naming — ViewModels suffix `ViewModel` | ✅ PASS | Will use `DashboardViewModel` |
| Naming — Controllers plural noun | ✅ PASS | `DashboardController` — single noun is conventional for a single-page dashboard; no collection implied |

**Result**: All gates pass. Proceeding to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/003-admin-layout-dashboard/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command — NOT created here)
```

### Source Code (repository root)

```text
AdminTemplate.Web/
├── Controllers/
│   └── DashboardController.cs          # NEW — Index action, stat counts
├── ViewModels/
│   └── DashboardViewModel.cs           # NEW — TotalUsers, TotalRoles, ActiveSessions
├── Views/
│   ├── Dashboard/
│   │   └── Index.cshtml                # NEW — stat cards using DashboardViewModel
│   └── Shared/
│       ├── _Layout.cshtml              # REPLACE — full sidebar+topnav admin shell
│       ├── _Sidebar.cshtml             # NEW — collapsible sidebar partial
│       └── _Topnav.cshtml             # NEW — topnav partial (hamburger, bell, avatar)
└── wwwroot/
    ├── css/
    │   ├── site.css                    # REPLACE — CSS variables + base admin styles
    │   └── sidebar.css                 # NEW — sidebar-specific layout rules
    └── js/
        └── site.js                     # REPLACE — sidebar toggle script
```

**Structure Decision**: ASP.NET Core MVC single-project (Web layer). Layout partials follow the spec's prescribed structure using `_Sidebar.cshtml` and `_Topnav.cshtml` rendered via `@Html.Partial` inside `_Layout.cshtml`. All styles are file-based (no CDN CSS overrides inline).

## Complexity Tracking

No constitution violations to justify.

## Post-Design Constitution Check

*Re-evaluated after Phase 1 design artifacts (data-model.md, contracts/, quickstart.md) are complete.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Clean Architecture | ✅ PASS | `DashboardController` depends only on `IUserService`/`IRoleService` (Application layer); confirmed in quickstart Step 4 |
| II. Dependency Inversion | ✅ PASS | No Infrastructure type referenced in controller or ViewModel |
| III. Thin Controllers | ✅ PASS | `Index()` action: 3 service calls + ViewModel mapping + return View — nothing more |
| IV. Repository + Unit of Work | ✅ PASS | No new data access; existing repos serve the count queries |
| V. JSON-Driven Permissions | ✅ PASS | Static sidebar nav is explicitly scoped as a Phase 9 concern in the spec assumptions |
| VI. Generic DataTable | ✅ PASS | No DataTable in scope for this phase |
| VII. Permission-Aware UI | ✅ PASS | No per-user UI hiding; deferred to Phase 9 |
| No inline styles | ✅ PASS | All Phase 3 files use `var(--primary-color)` references; design confirms no inline style attributes |
| Security — Logout POST + CSRF | ✅ PASS | Contracts doc specifies `[ValidateAntiForgeryToken]` on POST `/Account/Logout` |
| CSS single-source-of-truth | ✅ PASS | Five CSS tokens defined in `site.css`; contracts/routes-and-css-tokens.md is the authoritative reference |

**Result**: All gates pass post-design. Ready for `/speckit.tasks`.
