# Implementation Plan: Generic DataTable Solution

**Branch**: `005-generic-datatable` | **Date**: 2026-04-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-generic-datatable/spec.md`

## Summary

A single reusable JavaScript module (`datatable.js`) and a server-side C# helper
(`DataTableHelper`) that together enable any management page to render a fully-functional,
server-side, permission-aware DataTable with one `AppDataTable.init(config)` call and
one controller action. Permissions for Create / Edit / Delete are resolved server-side
and passed as a config object; the JS layer enforces them at render time by omitting
buttons from the DOM. Delete confirmation uses a Bootstrap 5 modal with CSRF token
injection. All states (loading, empty, error) match the established design system tokens.

## Technical Context

**Language/Version**: C# 12 / .NET 8 + JavaScript ES6+ (no transpile step)
**Primary Dependencies**: DataTables.net 1.13+, jQuery 3.x (DataTables peer dep), Bootstrap 5.3.3, ASP.NET Core MVC 8
**Storage**: N/A — `DataTableHelper` accepts `IQueryable<T>` from calling controllers; no new storage introduced
**Testing**: No dedicated test project in this repository; acceptance verified manually per spec scenarios
**Target Platform**: Web (Kestrel/IIS), Windows Server 2019+ / Windows 11
**Project Type**: admin panel module within ASP.NET Core MVC web application
**Performance Goals**: Search request in flight within 400 ms of last keystroke; delete-to-row-removed within 1 s under normal network conditions
**Constraints**: CSRF token required on every delete POST; buttons absent from DOM (not CSS-hidden) when permission is false; no `window.confirm()`; RTL parity via CSS logical properties; design system tokens only
**Scale/Scope**: Single-tenant admin panel, ~10–50 concurrent users; no horizontal scaling concerns

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design below.*

| Principle | Status | Notes |
|---|---|---|
| I. Clean Architecture | ✅ PASS | DTOs (`DataTableRequest`, `DataTableResponse<T>`) in `Application/DTOs/`. Static helper in `Web/Helpers/`. JS + Razor in Web layer only. |
| II. Dependency Inversion | ✅ PASS | No new interfaces needed. `DataTableHelper` is a static utility consumed by controllers. |
| III. Thin Controllers | ✅ PASS | `DataTableHelper.GetDataTableResponse<T>()` extracts all pagination/sort/filter logic OUT of controllers. Controllers pass `IQueryable<T>` and receive a DTO. |
| IV. Repository + Unit of Work | ✅ PASS | Not modified. Calling controllers continue to use Application services backed by repositories. |
| V. JSON-Driven Permissions | ✅ PASS | Permissions resolved server-side via existing permission system; serialised into a JS config object on the page. No hardcoding. |
| VI. Generic DataTable Solution | ✅ PASS | This feature IS the delivery of Principle VI. |
| VII. Permission-Aware UI | ✅ PASS | `datatable.js` omits Create / Edit / Delete buttons from the DOM when the corresponding permission flag is `false`. |
| VIII. Design System First | ✅ PASS | All buttons, card wrappers, and state messages use established component classes and `var()` CSS tokens. No hardcoded values. |

**Gate result: ALL PASS — proceed to Phase 0.**

*Post-design re-check: pending Phase 1 completion (see bottom of this file).*

## Project Structure

### Documentation (this feature)

```text
specs/005-generic-datatable/
├── plan.md                         # This file
├── research.md                     # Phase 0 output
├── data-model.md                   # Phase 1 output
├── quickstart.md                   # Phase 1 output
├── contracts/
│   ├── datatable-ajax.md           # HTTP request/response contract
│   └── datatable-js-api.md         # AppDataTable.init() JavaScript API contract
└── tasks.md                        # Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
AdminTemplate.Application/
└── DTOs/
    ├── DataTableRequest.cs          # Server-side request parameters DTO
    └── DataTableResponse.cs         # Generic server-side response DTO

AdminTemplate.Web/
├── wwwroot/js/
│   └── datatable.js                 # AppDataTable JS module (new file)
├── Views/Shared/
│   └── _DeleteConfirmModal.cshtml   # Shared Bootstrap 5 delete modal partial (new)
└── Helpers/
    └── DataTableHelper.cs           # Static helper: GetDataTableResponse<T>() (new)
```

**Structure Decision**: Two new DTOs in the Application layer (the correct home for
shared data contracts). One static Web-tier helper (MVC-specific, processes HTTP
request parameters). One JS file and one Razor partial in the Web layer only. No new
controllers; every management page adds its own `GetData` action calling
`DataTableHelper.GetDataTableResponse<T>()`.

## Complexity Tracking

> No constitution violations — table not needed.

---

## Post-Design Constitution Re-check

*Filled after Phase 1 artifacts are complete.*

| Principle | Status | Notes |
|---|---|---|
| I. Clean Architecture | ✅ PASS | DTOs remain in Application; helper in Web/Helpers; contracts reflect correct layer ownership |
| II. Dependency Inversion | ✅ PASS | No added dependencies; `DataTableHelper` is a static utility with no DI requirement |
| III. Thin Controllers | ✅ PASS | `DataTableHelper` design confirmed: single-method static class, controller delegates entirely |
| IV. Repository + Unit of Work | ✅ PASS | Not touched by this feature |
| V. JSON-Driven Permissions | ✅ PASS | JS API contract specifies permissions come from a server-rendered config object |
| VI. Generic DataTable Solution | ✅ PASS | Contracts and data model confirm single-init-call design |
| VII. Permission-Aware UI | ✅ PASS | DOM-absent enforcement documented in JS API contract |
| VIII. Design System First | ✅ PASS | Quickstart and contracts explicitly reference design token classes; no colour literals |

**Gate result: ALL PASS — ready for `/speckit.tasks`.**
