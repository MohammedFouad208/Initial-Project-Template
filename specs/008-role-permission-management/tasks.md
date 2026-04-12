# Tasks: Role-Permission Management

**Input**: Design documents from `specs/008-role-permission-management/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Not requested — no test tasks included.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no blocking dependencies)
- **[Story]**: Which user story this task belongs to ([US1], [US2], [US3])
- Exact file paths are included in every task description

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the directory structure and shared ViewModel foundation that all user stories depend on.

- [X] T001 Create directory `AdminTemplate.Web/ViewModels/RolePermissions/`
- [X] T002 Create directory `AdminTemplate.Web/Views/RolePermissions/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The `RolePermissionsViewModel` and the controller skeleton must exist before any view can be implemented. All user stories depend on these.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 Create `AdminTemplate.Web/ViewModels/RolePermissions/RolePermissionsViewModel.cs` with properties: `string RoleId`, `string RoleName`, `IReadOnlyList<PermissionObjectDto> AllObjects`, `HashSet<string> AssignedKeys` — add `using AdminTemplate.Application.DTOs;` and `using AdminTemplate.Application.Providers;`
- [X] T004 Create `AdminTemplate.Web/Controllers/RolePermissionsController.cs` with class scaffold: `[Authorize]` + constructor injecting `IRoleService`, `IPermissionService`, `IPermissionProvider` — include `using` directives for Application interfaces, Filters, and Web.ViewModels.RolePermissions — leave action bodies empty for now

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 — Assign Permissions to a Role via Matrix UI (Priority: P1) 🎯 MVP

**Goal**: A Super Admin can open the permission matrix for any role, see all objects/functions from `permissions.json` as a checkbox grid with current assignments pre-checked, modify selections, and save. The save batch-replaces all `RolePermission` records for the role, then redirects back with a success alert.

**Independent Test**: Open the matrix for any role, toggle several checkboxes, click Save, then re-open — confirm the pre-checked state matches exactly what was saved.

### Implementation for User Story 1

- [X] T005 [US1] Implement `RolePermissionsController.Index(string roleId)` GET action in `AdminTemplate.Web/Controllers/RolePermissionsController.cs`: parse roleId to Guid (404 if invalid), call `IRoleService.GetByIdAsync` (404 if null), call `IPermissionProvider.GetAll()`, call `IPermissionService.GetRolePermissionsAsync(guid)`, build `AssignedKeys` HashSet using `"ObjectName|FunctionName"` pattern, return `View(new RolePermissionsViewModel { ... })`

- [X] T006 [US1] Implement `RolePermissionsController.Save(string roleId, List<string>? selectedPermissions)` POST action in `AdminTemplate.Web/Controllers/RolePermissionsController.cs`: add `[HttpPost]`, `[ValidateAntiForgeryToken]`, `[HasPermission("Role", "AssignPermissions")]` attributes; parse roleId to Guid (404 if invalid); split each `selectedPermissions` item on `'|'` to produce `IEnumerable<PermissionDto>`; call `IPermissionService.SaveRolePermissionsAsync(guid, permissions)`; set `TempData["Success"]` and redirect to `Index`; wrap in try/catch — on exception set `TempData["Error"]` and redirect back

- [X] T007 [US1] Apply `[HasPermission("Role", "AssignPermissions")]` to the `Index` GET action in `AdminTemplate.Web/Controllers/RolePermissionsController.cs` (filter already exists at `AdminTemplate.Web/Filters/HasPermissionAttribute.cs`)

- [X] T008 [US1] Create `AdminTemplate.Web/Views/RolePermissions/Index.cshtml`: set `Layout = "_Layout"` and `ViewData["Title"]`; add `.page-header` block with `<h1>Manage Permissions: @Model.RoleName</h1>` and breadcrumb (Home → Roles → Manage Permissions); render `TempData["Success"]` as `.alert.alert-success` with `border-inline-start: 4px solid var(--success)` and `TempData["Error"]` as `.alert.alert-danger` with `border-inline-start: 4px solid var(--danger)`

- [X] T009 [US1] Add the permission matrix `<form method="post" asp-action="Save">` to `AdminTemplate.Web/Views/RolePermissions/Index.cshtml`: include `@Html.AntiForgeryToken()`, hidden `<input name="roleId" value="@Model.RoleId" />`; build a `.card` wrapping a `.table-responsive`; construct `<table class="table table-bordered permission-matrix">` with `<thead>` column headers from `AllObjects` and functions (deduplicated across all objects), and `<tbody>` rows — one per `PermissionObjectDto` in `Model.AllObjects` — each row shows `DisplayName` in the first `<td>` plus one `<td>` per function containing a `.form-check-input` checkbox with `name="selectedPermissions"` and `value="@obj.Name|@func"`, checked if `Model.AssignedKeys.Contains($"{obj.Name}|{func}")`

- [X] T010 [US1] Add Save and Back buttons below the form in `AdminTemplate.Web/Views/RolePermissions/Index.cshtml`: `<button type="submit" class="btn btn-primary">Save Permissions</button>` (site.js will inject `.btn-loading` on submit) and `<a asp-controller="Roles" asp-action="Index" class="btn btn-outline-secondary ms-2">Back to Roles</a>`

- [X] T011 [US1] Add sticky first-column CSS to `AdminTemplate.Web/Views/RolePermissions/Index.cshtml` in a `<style>` block: `.permission-matrix td:first-child, .permission-matrix th:first-child { position: sticky; inset-inline-start: 0; background: var(--bg-card); z-index: 1; border-inline-end: 2px solid var(--border-color); }` — no hardcoded colors, use only CSS custom properties

**Checkpoint**: User Story 1 is now fully functional and independently testable. A Super Admin can open `/RolePermissions/{roleId}`, see the matrix, check/uncheck cells, and Save — verified by reopening and confirming pre-checked state matches the saved state.

---

## Phase 4: User Story 2 — Navigate to Role-Permission Matrix from Roles List (Priority: P2)

**Goal**: The "Manage Permissions" button already exists in `Views/Roles/Index.cshtml` (confirmed in research). This story validates the full navigation flow works end-to-end and the page header correctly shows the role name. The one task here registers the controller route so the existing link resolves correctly.

**Independent Test**: Click "Manage Permissions" for any role in the Roles list and confirm the matrix page opens with that role's name in the `<h1>` heading.

### Implementation for User Story 2

- [X] T012 [US2] Verify the route for `RolePermissionsController` resolves the existing link in `AdminTemplate.Web/Views/Roles/Index.cshtml` (which uses `href="/RolePermissions/" + row.id`): confirm the controller uses the default MVC routing convention `[controller]/[action]` — add `[Route("[controller]")]` and `[Route("{roleId}")]` on `Index` if needed to match `/RolePermissions/{roleId}` in `AdminTemplate.Web/Controllers/RolePermissionsController.cs`

- [X] T013 [P] [US2] Confirm the breadcrumb in `AdminTemplate.Web/Views/RolePermissions/Index.cshtml` links back to Roles Index via `<a asp-controller="Roles" asp-action="Index">` — no hardcoded URLs; confirm the `<h1>` text is `"Manage Permissions: @Model.RoleName"` (role name comes from `IRoleService.GetByIdAsync`)

**Checkpoint**: User Stories 1 and 2 both work. Navigating from Roles list → Manage Permissions → Save → redirect back to matrix is the complete verified flow.

---

## Phase 5: User Story 3 — Bulk Check/Uncheck Permissions per Row and Column (Priority: P3)

**Goal**: "Check All" and "Uncheck All" buttons per object row, and a column-header toggle checkbox per function column, all implemented as pure vanilla JS in the view's `@section Scripts` block.

**Independent Test**: Click "Check All" for the Employee row — all checkboxes in that row become checked without affecting other rows. Click a column header checkbox for "Delete" — the Delete column across all rows toggles.

### Implementation for User Story 3

- [X] T014 [US3] Add a "Check All" button per object row to `AdminTemplate.Web/Views/RolePermissions/Index.cshtml`: inside the first `<td>` of each `<tr>`, add `<button type="button" class="btn btn-outline-secondary btn-sm ms-2 check-all-btn" data-object="@obj.Name">All</button>` and `<button type="button" class="btn btn-outline-secondary btn-sm check-none-btn" data-object="@obj.Name">None</button>`

- [X] T015 [US3] Add column-header toggle checkboxes to `<thead>` in `AdminTemplate.Web/Views/RolePermissions/Index.cshtml`: inside each function `<th>`, add a `.form-check-input` with `class="col-toggle"` and `data-func="@func"` below the function label text

- [X] T016 [US3] Add `@section Scripts` to `AdminTemplate.Web/Views/RolePermissions/Index.cshtml` with vanilla JS: (a) `.check-all-btn` click handler — selects all `input[type=checkbox][data-object="{obj}"]` and sets `checked = true`; (b) `.check-none-btn` click handler — sets same selection to `checked = false`; (c) `.col-toggle` change handler — selects all `input[type=checkbox][data-func="{func}"]` in tbody and sets `checked` to match the header checkbox state — use `data-object` and `data-func` attributes on each cell checkbox to enable these selectors

- [X] T017 [P] [US3] Add `data-object="@obj.Name"` and `data-func="@func"` attributes to every cell `<input type="checkbox">` in the matrix tbody in `AdminTemplate.Web/Views/RolePermissions/Index.cshtml` — these are required by the JS selectors added in T016

**Checkpoint**: All three user stories are functional. The full matrix UX — individual checks, row bulk controls, and column bulk controls — is complete.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final hardening — ensure all design system rules are met, RTL is correct, and the solution builds cleanly.

- [X] T018 [P] Verify all CSS in `AdminTemplate.Web/Views/RolePermissions/Index.cshtml` uses only CSS custom properties (`var(--primary)`, `var(--border-color)`, etc.) — no hardcoded hex values or direction-specific properties (`margin-left`, `padding-right`) — use logical properties (`margin-inline-start`, `padding-inline-end`) throughout per design system constitution

- [X] T019 [P] Verify `AdminTemplate.Web/Controllers/RolePermissionsController.cs` has `[Authorize]` on the class, `[ValidateAntiForgeryToken]` on the POST Save action, and `[HasPermission("Role", "AssignPermissions")]` on both GET Index and POST Save actions

- [X] T020 Confirm solution builds with 0 errors: `dotnet build AdminTemplate.sln` from repo root; resolve any missing `using` directives or namespace mismatches in `RolePermissionsController.cs` and `RolePermissionsViewModel.cs`

- [X] T021 Run the end-to-end verification from `specs/008-role-permission-management/quickstart.md`: log in as SuperAdmin → Roles list → Manage Permissions → toggle checkboxes → Save → confirm success alert → re-open matrix → confirm saved state

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **BLOCKS all user stories**
- **User Story 1 (Phase 3)**: Depends on Phase 2 — core controller + view
- **User Story 2 (Phase 4)**: Depends on Phase 3 (controller and view must exist for route to resolve)
- **User Story 3 (Phase 5)**: Depends on Phase 3 (matrix HTML must exist for JS selectors)
- **Polish (Phase 6)**: Depends on Phases 3–5 all complete

### User Story Dependencies

- **US1** depends only on the Foundational phase — matrix read/write is self-contained
- **US2** depends on US1 — the route must exist before navigation can be verified
- **US3** depends on US1 — the matrix HTML checkboxes must exist before JS selectors target them

### Within Each User Story

- Controller action before view (T005/T006/T007 before T008–T011)
- View HTML structure before JS behavior (T008–T010 before T016–T017)
- Checkbox attributes (T017) must be in place before the JS handlers (T016) are wired

### Parallel Opportunities

- T001 and T002 (directory creation) can run in parallel
- T003 and T004 (ViewModel + controller scaffold) can run in parallel
- T012 and T013 (US2 route + breadcrumb verification) can run in parallel
- T017 (adding data attributes) and T014/T015 (adding buttons/header checkboxes) can run in parallel
- T018 and T019 (polish checks) can run in parallel

---

## Parallel Example: User Story 1

```
# Core actions (T005, T006, T007 are sequential — build on same file)
T005 → Implement GET Index action
T006 → Implement POST Save action
T007 → Apply HasPermission to GET action

# View tasks (T008 sets structure; T009 adds form; T010/T011 add buttons/CSS)
T008 → Page header + alert rendering
T009 → Matrix form + checkbox grid        (depends on T008)
T010 → Submit + Back buttons              (can follow T009)
T011 → Sticky column CSS                  (can run with T009)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete **Phase 1**: Create directories
2. Complete **Phase 2**: ViewModel + controller scaffold (CRITICAL — blocks all stories)
3. Complete **Phase 3**: US1 — controller actions + matrix view
4. **STOP and VALIDATE**: Log in as SuperAdmin, open `/RolePermissions/{roleId}`, check/uncheck, Save — confirm persistence
5. Continue with Phase 4 (US2) once US1 is verified

### Suggested MVP Scope

**Phases 1–3 = MVP** (User Story 1 only): the matrix loads correctly, permissions save and reload correctly, and the CSRF-protected POST works. US2 (navigation) and US3 (bulk toggles) are enhancements that can follow.

### What Does NOT Need Building

The following are already fully implemented and require **zero changes**:
- `AdminTemplate.Application.Services.PermissionService` — `GetRolePermissionsAsync`, `SaveRolePermissionsAsync`
- `AdminTemplate.Infrastructure.Repositories.PermissionRepository` — `DeleteByRoleIdAsync`, `AddRangeAsync`
- `AdminTemplate.Infrastructure.Providers.JsonPermissionProvider` — reads `permissions.json`
- `AdminTemplate.Infrastructure.Extensions.InfrastructureServiceExtensions` — all DI registrations
- `AdminTemplate.Web.Filters.HasPermissionAttribute` — authorization filter
- `AdminTemplate.Web.Views.Roles.Index.cshtml` — "Manage Permissions" link already present
