---
description: "Task list for Phase 9 â€” Polish, Guards & Seed Data"
---

# Tasks: Polish, Guards & Seed Data

**Input**: Design documents from `/specs/009-polish-guards-seed/`
**Prerequisites**: plan.md âœ…, spec.md âœ…, research.md âœ…, data-model.md âœ…, contracts/js-toast-api.md âœ…

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Maps to user story (US1â€“US5)
- All paths relative to repository root

---

## Phase 1: Setup â€” Design Token & CSS Foundations

**Purpose**: Add the CSS utility classes used by all subsequent tasks. Must be done first so error pages and breadcrumb fixes have the classes available.

- [X] T001 [P] Add `.error-icon-circle`, `.danger-circle`, `.warning-circle`, `.info-circle` and `.breadcrumb-link` CSS utility classes to `AdminTemplate.Web/wwwroot/css/site.css`

**Checkpoint**: CSS classes exist â€” error page and breadcrumb tasks can proceed.

---

## Phase 2: Foundational â€” Layout & JS Infrastructure

**Purpose**: Fix the layout file and add the global JS API that all other phases depend on. `_Layout.cshtml` is the root template; its forbidden-pattern violations must be resolved before any view work begins. The `AppToast` function must exist in `site.js` before toast wiring is added to controllers.

**âš ï¸ CRITICAL**: These tasks block all view changes and toast integration.

- [X] T002 Fix `AdminTemplate.Web/Views/Shared/_Layout.cshtml`: replace `@Html.Partial("_Sidebar")` with `<partial name="_Sidebar" />` and `@Html.Partial("_Topnav")` with `<partial name="_Topnav" />`
- [X] T003 Add toast container div and TempData toast trigger script to `AdminTemplate.Web/Views/Shared/_Layout.cshtml` per the `contracts/js-toast-api.md` contract
- [X] T004 Add `AppToast` IIFE namespace with `show(message, type)` method to end of `AdminTemplate.Web/wwwroot/js/site.js` per the `contracts/js-toast-api.md` contract
- [X] T005 [P] Add Bootstrap 5 `highlight`/`unhighlight` adapter (jQuery Validate + `.is-valid`/`.is-invalid`) to `AdminTemplate.Web/Views/Shared/_ValidationScriptsPartial.cshtml` per the research.md Decision 2

**Checkpoint**: Layout is constitutionally compliant; `AppToast.show()` is available globally; form validation adapter is ready.

---

## Phase 3: User Story 1 â€” Developer Onboarding (Priority: P1) ðŸŽ¯ MVP

**Goal**: A fresh clone can be running with seeded data in under 10 minutes by following the README.

**Independent Test**: Follow the README from scratch on a clean machine; verify the app starts, all three roles exist in the DB, and SuperAdmin login works.

### Implementation for User Story 1

- [X] T006 [P] Add `Seed:AdminEmail`, `Seed:AdminPassword`, `Seed:ViewerEmail`, `Seed:ViewerPassword` seed credential keys and full `Smtp` placeholder section to `AdminTemplate.Web/appsettings.json` per data-model.md
- [X] T007 Extend `AdminTemplate.Infrastructure/Seed/DataSeeder.cs` with `SeedDemoRolesAsync()` method: idempotently create Admin role + seed Browse/Create/Update permissions for all objects from `IPermissionProvider`; create `admin@admintemplate.local` user from config; assign to Admin role
- [X] T008 [P] Extend `AdminTemplate.Infrastructure/Seed/DataSeeder.cs` `SeedDemoRolesAsync()`: idempotently create Viewer role + seed Browse-only permissions for all objects; create `viewer@admintemplate.local` user from config; assign to Viewer role (can be authored in same sitting as T007, but is a separate logical block)
- [X] T009 Call `SeedDemoRolesAsync()` from `SeedAsync()` in `AdminTemplate.Infrastructure/Seed/DataSeeder.cs` after existing SuperAdmin seed logic
- [X] T010 Write `README.md` at repository root with sections: Prerequisites, Setup (connection string), Database (PM Console `Update-Database`), Seed Data (auto-run note + default credentials table for all 3 users), Run, Configuration (SMTP + user secrets guidance)

**Checkpoint**: App clones and runs; three roles and three seed users present; README guides the process end-to-end.

---

## Phase 4: User Story 2 â€” Styled Error Pages (Priority: P2)

**Goal**: Unauthorized access and missing routes show branded, design-system-compliant error pages with navigation buttons.

**Independent Test**: Log in as Viewer user; navigate directly to `/Users/Create`; verify the 403 page shows a lock icon in a danger-colored circle with a back button. Navigate to `/does-not-exist`; verify the 404 page renders correctly.

### Implementation for User Story 2

- [X] T011 Redesign `AdminTemplate.Web/Views/Shared/Error403.cshtml`: replace generic card with `.page-header` section, `<div class="error-icon-circle danger-circle">` containing `<i class="fa-solid fa-lock"></i>`, descriptive paragraph, and `<a class="btn btn-outline-secondary">` back-to-dashboard button; use only design-token classes (depends on T001)
- [X] T012 Create `AdminTemplate.Web/Controllers/ErrorController.cs` with an `[AllowAnonymous]` `NotFound()` GET action that returns `View("NotFound")`
- [X] T013 Add `app.UseStatusCodePagesWithRedirects("/Error/NotFound")` to the middleware pipeline in `AdminTemplate.Web/Program.cs`, placed before `app.UseRouting()`
- [X] T014 Create `AdminTemplate.Web/Views/Error/NotFound.cshtml`: `.page-header` section, `<div class="error-icon-circle warning-circle">` containing `<i class="fa-solid fa-circle-question"></i>`, descriptive paragraph, and `.btn.btn-outline-secondary` home button; use only design-token classes (depends on T001)

**Checkpoint**: 403 and 404 error pages render within the admin layout; icons are in soft-color circles; navigation buttons work; RTL layout is preserved.

---

## Phase 5: User Story 3 â€” Permission-Aware Sidebar (Priority: P2)

**Goal**: All sidebar nav links are gated by the current user's permissions using `d-none` class; DOM structure is never altered.

**Independent Test**: Log in as Admin user (Browse Users + Roles only); verify Users and Roles links are visible; verify Role Permissions link has `d-none`; verify sidebar dimensions are identical to SuperAdmin view.

### Implementation for User Story 3

- [X] T015 Add `canAssignPermissions` permission check to `AdminTemplate.Web/Views/Shared/_Sidebar.cshtml` using `PermissionService.UserHasPermissionAsync(sidebarUserId, "Role", "AssignPermissions")`; apply `@(canAssignPermissions ? "" : "d-none")` to the Role Permissions nav link `<a>` element

**Checkpoint**: All three sidebar management links are permission-gated with `d-none`; SuperAdmin sees all links; Viewer without permissions sees none; sidebar layout is structurally stable.

---

## Phase 6: User Story 4 â€” Real-Time Form Validation (Priority: P3)

**Goal**: All Create and Edit forms show `.is-valid`/`.is-invalid` class feedback in real time; submit buttons enter a loading state on submission.

**Independent Test**: Open the Create User form; submit empty; verify fields show red `.is-invalid` border; correct a field; verify it transitions to green `.is-valid`; submit the completed form and verify the submit button enters spinner state.

### Implementation for User Story 4

- [X] T016 [P] [US4] Verify `@section Scripts { <partial name="_ValidationScriptsPartial" /> }` is present in `AdminTemplate.Web/Views/Users/Create.cshtml` and `AdminTemplate.Web/Views/Users/Edit.cshtml`; add if missing (depends on T005)
- [X] T017 [P] [US4] Verify `@section Scripts { <partial name="_ValidationScriptsPartial" /> }` is present in `AdminTemplate.Web/Views/Roles/Create.cshtml` and `AdminTemplate.Web/Views/Roles/Edit.cshtml`; add if missing (depends on T005)
- [X] T018 [P] [US4] Verify `@section Scripts { <partial name="_ValidationScriptsPartial" /> }` is present in `AdminTemplate.Web/Views/RolePermissions/Index.cshtml`; add if missing (depends on T005)

**Checkpoint**: All Create and Edit forms across Users, Roles, and RolePermissions display real-time validation feedback and submit-button loading states.

---

## Phase 7: User Story 5 â€” Toast Notification System (Priority: P3)

**Goal**: User actions that redirect (save permissions, create/edit/delete) surface a success or danger toast via TempData; `AppToast.show()` is also available for direct JS calls.

**Independent Test**: Save role permissions; verify a green success toast appears bottom-right and auto-dismisses after ~4 seconds. Delete a role that has users; verify a danger toast appears with an error message.

### Implementation for User Story 5

- [X] T019 [US5] Add `TempData["ToastMessage"]` and `TempData["ToastType"] = "success"` to the successful redirect in `AdminTemplate.Web/Controllers/RolePermissionsController.cs` `Save` action (depends on T003, T004)
- [X] T020 [P] [US5] Add `TempData["ToastMessage"]` and `TempData["ToastType"]` to success and failure redirects in `AdminTemplate.Web/Controllers/UsersController.cs` (`Create`, `Edit`, `ToggleActive` POST actions) (depends on T003, T004)
- [X] T021 [P] [US5] Add `TempData["ToastMessage"]` and `TempData["ToastType"]` to success and failure redirects in `AdminTemplate.Web/Controllers/RolesController.cs` (`Create`, `Edit`, `Delete` POST actions) (depends on T003, T004)

**Checkpoint**: All management CRUD actions surface a toast notification. Toast uses design-token colors and auto-dismisses. Works identically in LTR and RTL layouts.

---

## Phase 8: Polish & Design System Audit

**Purpose**: Final constitutional compliance sweep â€” resolve all remaining forbidden-pattern violations found during audit.

- [X] T022 [P] Fix all breadcrumb anchor inline styles in `AdminTemplate.Web/Views/Users/Create.cshtml` and `AdminTemplate.Web/Views/Users/Edit.cshtml`: replace `style="color:var(--primary)"` with `class="breadcrumb-link"` (depends on T001)
- [X] T023 [P] Fix all breadcrumb anchor inline styles in `AdminTemplate.Web/Views/Roles/Create.cshtml` and `AdminTemplate.Web/Views/Roles/Edit.cshtml`: replace `style="color:var(--primary)"` with `class="breadcrumb-link"` (depends on T001)
- [X] T024 Walk all `.cshtml` files in `AdminTemplate.Web/Views/` and verify: no `@Html.Partial()` calls remain; no hardcoded hex/named colors in view files; all breadcrumb links use `.breadcrumb-link`; all form views include `_ValidationScriptsPartial`; all error pages use icon-in-circle pattern; toast container present in `_Layout.cshtml`
- [X] T025 Walk `AdminTemplate.Web/wwwroot/css/site.css` and `sidebar.css` and verify: no `margin-left`, `padding-right`, `left:`, `right:` direction-specific properties; all use logical property equivalents

**Checkpoint**: Zero design system violations remain. App builds with 0 errors. All pages pass the design system audit.

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (CSS Setup - T001)
    â””â”€â”€ Phase 2 (Layout + JS - T002â€“T005)  â† Foundational blocker
            â”œâ”€â”€ Phase 3 (US1: Seed + README - T006â€“T010)   â† Can start with Phase 2
            â”œâ”€â”€ Phase 4 (US2: Error Pages - T011â€“T014)     â† Needs T001, T002
            â”œâ”€â”€ Phase 5 (US3: Sidebar - T015)              â† Can start after Phase 2
            â”œâ”€â”€ Phase 6 (US4: Validation - T016â€“T018)      â† Needs T005
            â”œâ”€â”€ Phase 7 (US5: Toast - T019â€“T021)           â† Needs T003, T004
            â””â”€â”€ Phase 8 (Audit - T022â€“T025)                â† Final; needs T001, T002, all prior
```

### User Story Independence

- **US1 (T006â€“T010)**: Fully independent â€” only touches Infrastructure/Seed and README; can run in parallel with Phases 4â€“7 after Phase 2 is done
- **US2 (T011â€“T014)**: Needs T001 (CSS) and T002 (layout forbidden-pattern fix); independent of US1/US3/US4/US5
- **US3 (T015)**: Single file change; needs Phase 2 complete; independent of all other stories
- **US4 (T016â€“T018)**: Needs T005 (validation adapter); independent of US1/US2/US3/US5
- **US5 (T019â€“T021)**: Needs T003 + T004 (toast container + `AppToast`); independent of US1/US2/US3/US4

### Parallel Opportunities Per Phase

**Phase 1**: T001 is a single task; no parallelism needed.

**Phase 2**: T002 and T005 touch different files â€” can run in parallel. T003 and T004 touch different files â€” can run in parallel.

**Phase 3 (US1)**:
```
T006 (appsettings.json)  â† parallel with T007/T008
T007 (Admin seed)        â† sequential: T007 then T008 
T008 (Viewer seed)       â† depends on T007 pattern being established
T009 (call method)       â† after T007+T008
T010 (README)            â† parallel with T006â€“T009
```

**Phase 4 (US2)**:
```
T011 (Error403 redesign)   â† needs T001; parallel with T012/T013/T014
T012 (ErrorController)     â† parallel with T011/T013/T014
T013 (Program.cs 404 MW)   â† parallel with T011/T012; before T014 verify
T014 (NotFound view)       â† needs T001; parallel with T011/T012
```

**Phases 6 & 7**: All `[P]`-marked tasks within each phase can run simultaneously as they touch different files.

---

## Implementation Summary

| Phase | US | Tasks | Files Touched |
|---|---|---|---|
| 1 Setup | â€” | T001 | `site.css` |
| 2 Foundation | â€” | T002â€“T005 | `_Layout.cshtml`, `site.js`, `_ValidationScriptsPartial.cshtml` |
| 3 US1 | Developer Onboarding | T006â€“T010 | `appsettings.json`, `DataSeeder.cs`, `README.md` |
| 4 US2 | Error Pages | T011â€“T014 | `Error403.cshtml`, `ErrorController.cs`, `Program.cs`, `Views/Error/NotFound.cshtml` |
| 5 US3 | Sidebar Gating | T015 | `_Sidebar.cshtml` |
| 6 US4 | Form Validation | T016â€“T018 | `Views/Users/*.cshtml`, `Views/Roles/*.cshtml`, `Views/RolePermissions/Index.cshtml` |
| 7 US5 | Toast Notifications | T019â€“T021 | `RolePermissionsController.cs`, `UsersController.cs`, `RolesController.cs` |
| 8 Polish | Audit | T022â€“T025 | `Views/**/*.cshtml`, `site.css`, `sidebar.css` |

**Total tasks**: 25  
**Parallelizable tasks**: 14 (marked [P])  
**New files**: 3 (`ErrorController.cs`, `Views/Error/NotFound.cshtml`, `README.md`)  
**Modified files**: ~18  
**New EF migrations**: 0  
**New NuGet packages**: 0  

### MVP Scope (US1 alone)
Implement Phases 1â€“3 (T001â€“T010) to deliver a clone-and-run template with correct seed data and README. All other user stories layer cleanly on top.
