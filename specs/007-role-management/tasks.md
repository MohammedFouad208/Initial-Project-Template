---
description: "Task list for PHASE 7 — Role Management"
---

# Tasks: PHASE 7 — Role Management

**Input**: Design documents from `/specs/007-role-management/`  
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/ ✅ quickstart.md ✅  
**Branch**: `007-role-management`  
**Tests**: Not requested — tasks follow implementation-only workflow per spec assumptions.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Additive interface extensions required before any repository or service stub can compile. Must complete before any user story work begins.

- [x] T001 [P] Add `Task<int> GetCountByRoleIdAsync(Guid roleId)` method signature to `AdminTemplate.Domain/Interfaces/IPermissionRepository.cs`
- [x] T002 [P] Add `Task<int> GetUserCountAsync(Guid roleId)` method signature to `AdminTemplate.Domain/Interfaces/IRoleRepository.cs`
- [x] T003 [P] Update `Task<(IReadOnlyList<RoleDto> Items, int TotalCount)> GetPagedAsync(...)` signature in `AdminTemplate.Application/Interfaces/IRoleService.cs` to add `string sortColumn = "name"` and `string sortDirection = "asc"` optional parameters
- [x] T004 [P] Update `Task<(IReadOnlyList<ApplicationRole> Items, int TotalCount)> GetPagedAsync(...)` signature in `AdminTemplate.Domain/Interfaces/IRoleRepository.cs` to add `string sortColumn = "name"` and `string sortDirection = "asc"` optional parameters

**Checkpoint**: Solution builds with updated interface signatures. No implementations exist yet — the four concrete stubs will fail to build until Phases 2 tasks are done.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Repository and service concrete implementations that every user story depends on. The `RolesController` skeleton must also exist before any user story action can be added.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T005 [P] Implement `PermissionRepository.GetCountByRoleIdAsync` — `return await _context.RolePermissions.CountAsync(rp => rp.RoleId == roleId)` in `AdminTemplate.Infrastructure/Repositories/PermissionRepository.cs`
- [x] T006 [P] Implement `RoleRepository.GetByIdAsync` stub — `return await _roleManager.FindByIdAsync(id.ToString())` in `AdminTemplate.Infrastructure/Repositories/RoleRepository.cs`
- [x] T007 [P] Implement `RoleRepository.GetAllAsync` stub — `return await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync()` in `AdminTemplate.Infrastructure/Repositories/RoleRepository.cs`
- [x] T008 Implement `RoleRepository.GetPagedAsync` stub — apply `Contains` search on `Name` and `Description`, switch on `sortColumn`/`sortDirection` for `OrderBy`, apply `Skip`/`Take`, return `(items, total)` tuple in `AdminTemplate.Infrastructure/Repositories/RoleRepository.cs`
- [x] T009 Implement `RoleRepository.UpdateAsync` stub — `await _roleManager.UpdateAsync(role)` in `AdminTemplate.Infrastructure/Repositories/RoleRepository.cs`
- [x] T010 Implement `RoleRepository.DeleteAsync` stub — `FindByIdAsync` then `_roleManager.DeleteAsync(role)` in `AdminTemplate.Infrastructure/Repositories/RoleRepository.cs`
- [x] T011 Implement `RoleRepository.GetUserCountAsync` (new) — `FindByIdAsync(roleId)`, then `_userManager.GetUsersInRoleAsync(role.Name)`, return `users.Count` in `AdminTemplate.Infrastructure/Repositories/RoleRepository.cs`
- [x] T012 Implement `RoleService.GetByIdAsync` stub — `Guid.TryParse`, call `_roleRepository.GetByIdAsync`, call `_roleRepository.GetUserCountAsync` and `_permissionRepository.GetCountByRoleIdAsync`, map to `RoleDto` in `AdminTemplate.Application/Services/RoleService.cs`
- [x] T013 Implement `RoleService.GetAllAsync` stub — call `_roleRepository.GetAllAsync`, iterate roles fetching counts, return `List<RoleDto>` in `AdminTemplate.Application/Services/RoleService.cs`
- [x] T014 Implement `RoleService.GetPagedAsync` stub — call `_roleRepository.GetPagedAsync(pageIndex, pageSize, searchTerm, sortColumn, sortDirection)`, iterate page results fetching counts via `GetUserCountAsync` and `GetCountByRoleIdAsync`, return `(dtos, total)` in `AdminTemplate.Application/Services/RoleService.cs`
- [x] T015 Implement `RoleService.CreateAsync` stub — instantiate `new ApplicationRole(dto.Name.Trim()) { Description = dto.Description?.Trim() }`, call `_roleManager.CreateAsync(role)`, return `IdentityResult` in `AdminTemplate.Application/Services/RoleService.cs`
- [x] T016 Implement `RoleService.UpdateAsync` stub — `Guid.TryParse`, `_roleRepository.GetByIdAsync`, set `Name = dto.Name.Trim()` and `Description = dto.Description?.Trim()`, call `_roleManager.UpdateAsync(role)`, return `IdentityResult.Failed` with `NotFound` code if role is null in `AdminTemplate.Application/Services/RoleService.cs`
- [x] T017 Implement `RoleService.DeleteAsync` stub — `Guid.TryParse`, `_roleRepository.GetByIdAsync`, guard SuperAdmin role with `IdentityResult.Failed(ProtectedRole)`, guard `HasUsersAsync` with `IdentityResult.Failed(RoleHasUsers)`, call `_roleRepository.DeleteAsync`, return `IdentityResult.Success` in `AdminTemplate.Application/Services/RoleService.cs`
- [x] T018 Create `RolesController` class with `[Authorize]` attribute and constructor injection of `IRoleService` and `IPermissionService` — no action methods yet in `AdminTemplate.Web/Controllers/RolesController.cs`

**Checkpoint**: Solution builds cleanly. `RolesController` is registered via convention routing. All `RoleService` and `RoleRepository` stubs are implemented. `RoleService.GetTotalCountAsync` already works (used by Dashboard); confirm it still compiles.

---

## Phase 3: User Story 1 — Browse & Search Roles (Priority: P1) 🎯 MVP

**Goal**: A Super Admin navigates to `/Roles` and sees a live server-side DataTable showing Role Name, Description, User Count, Permission Count, and Actions. Search by name/description and column-sort by Name, Description, or Created At work without a page reload.

**Independent Test**: With seeded SuperAdmin, navigate to `/Roles`. Confirm DataTable renders with the five columns, the search box filters results server-side, and User Count / Permission Count show correct figures. No Create/Edit/Delete operations needed.

### Implementation for User Story 1

- [x] T019 [US1] Add `Index` action (GET /Roles) to `RolesController` — resolve `canCreate`, `canUpdate`, `canDelete`, `canManagePermissions` via `IPermissionService.UserHasPermissionAsync("Role", ...)` using `User.FindFirstValue(ClaimTypes.NameIdentifier)`, assign to `ViewBag`, decorate with `[HasPermission("Role", "Browse")]`, return `Views/Roles/Index.cshtml` in `AdminTemplate.Web/Controllers/RolesController.cs`
- [x] T020 [US1] Add `GetData` action (GET /Roles/GetData) to `RolesController` — bind `[FromQuery] DataTableRequest`, map `SortColumn` index to column name string via `string[] columnMap = ["name","description","userCount","permissionCount","createdAt"]`, compute `pageIndex = Start / Length`, call `IRoleService.GetPagedAsync(pageIndex, Length, Search, sortCol, SortDirection)`, return `Json(new DataTableResponse<RoleDto>{...})`, decorate with `[HasPermission("Role", "Browse")]` in `AdminTemplate.Web/Controllers/RolesController.cs`
- [x] T021 [US1] Create `Views/Roles/Index.cshtml` — `.page-header` with `<h1>Roles</h1>` and breadcrumb, `.card.table-card` wrapper, `<table id="rolesTable">`, `AppDataTable.init({tableId: '#rolesTable', ajaxUrl: '/Roles/GetData', columns: [name, description, {userCount, orderable:false}, {permissionCount, orderable:false}, actions-render-fn], permissions: {canCreate, canUpdate, canDelete} from ViewBag, createUrl: '/Roles/Create', editUrl: '/Roles/Edit', deleteUrl: '/Roles/Delete', objectName:'Role'})` with the actions column custom render function including the "Manage Permissions" `.btn-outline-primary` + `fa-key` button (shown when `canManagePermissions`), all rendered in `@section Scripts` in `AdminTemplate.Web/Views/Roles/Index.cshtml`
- [x] T022 [US1] Add Roles nav link to sidebar — `<li class="nav-item">` with `asp-controller="Roles"`, `fa-solid fa-shield-halved` icon, `<span>Roles</span>` label, active class when controller == "Roles", `d-none` class when user lacks `Role → Browse` permission in `AdminTemplate.Web/Views/Shared/_Sidebar.cshtml`

**Checkpoint**: `/Roles` loads the DataTable. Server-side search filters by name and description. Sort by Name, Description, and Created At works. User Count and Permission Count columns show correct values. The "Manage Permissions" column button is visible for SuperAdmin.

---

## Phase 4: User Story 2 — Create a New Role (Priority: P2)

**Goal**: An admin clicks Create, fills in the role name and optional description, submits — the new role appears in the DataTable with User Count 0 and Permission Count 0. Duplicate name and empty name show inline validation errors without data loss.

**Independent Test**: Navigate to `/Roles/Create`. Submit with a unique name and confirm redirect to the list with the new role row. Submit again with the same name and confirm the duplicate-name validation error appears inline.

### Implementation for User Story 2

- [x] T023 [P] [US2] Create `CreateRoleViewModel` with `[Required]`, `[MaxLength(100)]` on `Name` and `[MaxLength(500)]` on `Description?` DataAnnotations in `AdminTemplate.Web/ViewModels/Roles/CreateRoleViewModel.cs`
- [x] T024 [US2] Add `Create GET` action (GET /Roles/Create) with `[HasPermission("Role", "Create")]` to `RolesController` — return `Views/Roles/Create.cshtml` with empty `CreateRoleViewModel` in `AdminTemplate.Web/Controllers/RolesController.cs`
- [x] T025 [US2] Add `Create POST` action with `[ValidateAntiForgeryToken]` and `[HasPermission("Role", "Create")]` to `RolesController` — check `ModelState.IsValid`, map ViewModel to `CreateRoleDto` (trim `Name`), call `IRoleService.CreateAsync(dto)`, loop `result.Errors` into `ModelState.AddModelError("Name", ...)`, on success redirect to `Index` in `AdminTemplate.Web/Controllers/RolesController.cs`
- [x] T026 [US2] Create `Views/Roles/Create.cshtml` — `.page-header` with `<h1>Create Role</h1>` and breadcrumb, `.card` wrapper with `.card-body`, `Name` field in `.mb-3` with `.form-label` and `.form-control` (no icon wrap needed for roles), `Description` as `<textarea class="form-control">`, `.alert.alert-danger` summary div with `border-inline-start: 4px solid var(--danger)` for validation errors, `.btn.btn-primary` submit with btn-loading support, `.btn.btn-outline-secondary` cancel link to `/Roles`, `<partial name="_ValidationScriptsPartial" />` in `AdminTemplate.Web/Views/Roles/Create.cshtml`

**Checkpoint**: Create form saves valid roles. Duplicate name and empty name show inline validation errors without losing entered data. New role appears in the DataTable with User Count 0 after redirect.

---

## Phase 5: User Story 3 — Edit an Existing Role (Priority: P3)

**Goal**: An admin clicks Edit on a DataTable row, sees a pre-populated form with the role's name and description, updates either field, saves — the DataTable row reflects the changes. Editing to a duplicate name shows a validation error.

**Independent Test**: Click Edit on any seeded role, change the description, save, and confirm the updated description appears in the list row.

### Implementation for User Story 3

- [x] T027 [P] [US3] Create `EditRoleViewModel` with `Id` (string, hidden binding), `Name` (`[Required]`, `[MaxLength(100)]`), and `Description?` (`[MaxLength(500)]`) in `AdminTemplate.Web/ViewModels/Roles/EditRoleViewModel.cs`
- [x] T028 [US3] Add `Edit GET` action (GET /Roles/Edit/{id}) with `[HasPermission("Role", "Update")]` to `RolesController` — call `IRoleService.GetByIdAsync(id)`, redirect to Index if null, map `RoleDto` to `EditRoleViewModel`, return view in `AdminTemplate.Web/Controllers/RolesController.cs`
- [x] T029 [US3] Add `Edit POST` action with `[ValidateAntiForgeryToken]` and `[HasPermission("Role", "Update")]` to `RolesController` — check `ModelState.IsValid`, map ViewModel to `UpdateRoleDto` (trim `Name`), call `IRoleService.UpdateAsync(id, dto)`, loop `result.Errors` into `ModelState.AddModelError("Name", ...)`, on success redirect to `Index` in `AdminTemplate.Web/Controllers/RolesController.cs`
- [x] T030 [US3] Create `Views/Roles/Edit.cshtml` — same design patterns as `Create.cshtml`, hidden `<input asp-for="Id" />`, Name and Description fields pre-filled from model, `.alert.alert-danger` for validation errors with `border-inline-start`, `.btn.btn-primary` submit, `.btn.btn-outline-secondary` cancel link, `<partial name="_ValidationScriptsPartial" />` in `AdminTemplate.Web/Views/Roles/Edit.cshtml`

**Checkpoint**: Edit form pre-populates name and description. Saving updates both fields. Editing to a duplicate name shows a validation error without losing form data. Updated row is reflected immediately in the list.

---

## Phase 6: User Story 4 — Delete a Role (Priority: P4)

**Goal**: An admin clicks Delete on a DataTable row, confirms the Bootstrap modal, and the role is removed (if it has no users) or a styled danger error is shown inline in the modal (if it has users or is SuperAdmin). The DataTable refreshes after a successful delete.

**Independent Test**: Create a throwaway role with no users, click Delete, confirm the modal, and verify the role no longer appears in the list. Then attempt to delete the "Admin" role (which has assigned users) and verify the error message appears in the modal without the role being deleted.

### Implementation for User Story 4

- [x] T031 [US4] Add `Delete POST` action (POST /Roles/Delete/{id}) with `[ValidateAntiForgeryToken]` and `[HasPermission("Role", "Delete")]` to `RolesController` — call `IRoleService.DeleteAsync(id)`, return `Json(new { success = true })` on `IdentityResult.Succeeded`, return `Json(new { success = false, message = result.Errors.First().Description })` on failure in `AdminTemplate.Web/Controllers/RolesController.cs`
- [x] T032 [US4] Verify `Views/Roles/Index.cshtml` delete flow — confirm `AppDataTable.init` delete handler uses `_DeleteConfirmModal.cshtml` pattern (or equivalent inline modal), sends anti-forgery token via request header, handles `{ success: false, message }` response by displaying the message inside the modal's danger alert area rather than closing the modal in `AdminTemplate.Web/Views/Roles/Index.cshtml`

**Checkpoint**: Deleting an unassigned role removes it and refreshes the DataTable. Deleting a role with assigned users shows the guard error message inside the confirmation modal. Deleting SuperAdmin shows the protection message. The modal closes only on a successful delete.

---

## Phase 7: User Story 5 — Navigate to Manage Permissions (Priority: P5)

**Goal**: The "Manage Permissions" button in each role row links to `/RolePermissions/{id}`. The button is hidden when the session user lacks `Role → AssignPermissions`. The `canManagePermissions` flag is already being resolved in the `Index` action (done in Phase 3), so this phase only verifies the render function and permission flag are wired correctly.

**Independent Test**: From the Roles list as SuperAdmin, click the "Manage Permissions" button on any role. Confirm the browser navigates to `/RolePermissions/{id}`. Log in as a role without `Role → AssignPermissions` and confirm the button is absent from the list.

### Implementation for User Story 5

- [x] T033 [US5] Verify the actions column render function in `Views/Roles/Index.cshtml` — confirm the "Manage Permissions" `<a href="/RolePermissions/${id}">` button uses `.btn.btn-outline-primary.btn-sm` with `<i class="fa fa-key"></i>`, is conditionally rendered only when `canManagePermissions` is `true` (already serialized from `ViewBag.CanManagePermissions` in the `Index` action from T019), and the route produces the correct URL pattern `/RolePermissions/{id}` in `AdminTemplate.Web/Views/Roles/Index.cshtml`

**Checkpoint**: SuperAdmin sees the "Manage Permissions" button on every row. Navigating it routes to `/RolePermissions/{id}`. A user without `Role → AssignPermissions` sees no button. The `canCreate`/`canUpdate`/`canDelete` buttons in the toolbar and actions column also hide correctly per the relevant permission flags.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Security hardening and design system compliance audit across all new files.

- [x] T034 [P] Audit all `RolesController` POST actions (`Create`, `Edit`, `Delete`) — confirm each has `[ValidateAntiForgeryToken]` attribute, and that the corresponding form/fetch call sends the anti-forgery token in `AdminTemplate.Web/Controllers/RolesController.cs` and `AdminTemplate.Web/Views/Roles/`
- [x] T035 [P] Design system audit — verify all new views use only `var(--*)` CSS custom properties (no hardcoded hex values), `.page-header` pattern, `.card` wrappers, `.form-label`/`.form-control` conventions, `.alert.alert-danger` with `border-inline-start: 4px solid var(--danger)`, `.btn-primary`/`.btn-outline-*` buttons, and CSS logical properties — no `margin-left`/`padding-right` anywhere in `AdminTemplate.Web/Views/Roles/`
- [x] T036 Run through `quickstart.md` verification checklist end-to-end — confirm all steps produce a compiling solution, the Roles list loads with correct counts, Create/Edit/Delete all work correctly, RTL toggle is verified on the Roles views, and the "Manage Permissions" button routes correctly

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — first independently testable increment
- **US2 (Phase 4)**: Depends on Phase 2 — independently testable; does not require US1
- **US3 (Phase 5)**: Depends on Phase 2 + US2 pattern (ViewModels mirror Create) — does not require US1
- **US4 (Phase 6)**: Depends on US1 (Phase 3) — the Delete action is triggered from `Index.cshtml`
- **US5 (Phase 7)**: Depends on US1 (Phase 3) — verifies the render function already added in T021
- **Polish (Final Phase)**: Depends on all user stories

### User Story Dependencies

| Story | Depends On | Notes |
|---|---|---|
| US1 — Browse | Phase 2 only | Independent MVP |
| US2 — Create | Phase 2 only | Independent from US1 |
| US3 — Edit | Phase 2 only | `EditRoleViewModel` pattern mirrors `CreateRoleViewModel` |
| US4 — Delete | US1 (shares Index.cshtml) | Delete POST is triggered from the DataTable |
| US5 — Manage Permissions nav | US1 (render function in Index.cshtml) | Verifies T021 render function |

### Parallel Opportunities Per Story

```
# Phase 1 — all interface changes are independent files or independent method additions:
T001: IPermissionRepository.cs — add GetCountByRoleIdAsync
T002: IRoleRepository.cs — add GetUserCountAsync
T003: IRoleService.cs — update GetPagedAsync signature
T004: IRoleRepository.cs — update GetPagedAsync signature (same file as T002; do after T002)

# Phase 2 — repository stubs in same file must be sequential; service stubs are independent:
T005: PermissionRepository.cs (independent file)
T006–T011: RoleRepository.cs stubs (same file — sequential within file)
T012–T017: RoleService.cs stubs (same file — sequential within file)
T018: RolesController.cs skeleton (independent)

# US2 — ViewModel and controller action are independent:
T023: CreateRoleViewModel.cs (new file)
T024+T025: RolesController.cs Create actions (after T023)
T026: Create.cshtml (after T023)

# US3 — ViewModel and controller actions are independent:
T027: EditRoleViewModel.cs (new file)
T028+T029: RolesController.cs Edit actions (after T027)
T030: Edit.cshtml (after T027)

# Polish — both audit tasks are independent:
T034: CSRF audit
T035: Design system audit
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete **Phase 1**: Interface signature updates
2. Complete **Phase 2**: All repository + service stubs + `RolesController` skeleton
3. Complete **Phase 3** (US1): `Index` + `GetData` actions → `Index.cshtml` → Sidebar link
4. **STOP and VALIDATE**: Navigate to `/Roles` as SuperAdmin — DataTable loads, search works, User Count and Permission Count show correct values
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → all infrastructure in place
2. **US1 done** → Roles list is live *(MVP!)*
3. **US2 done** → Admins can create new roles
4. **US3 done** → Admins can edit existing roles
5. **US4 done** → Admins can delete unassigned roles with guard messaging
6. **US5 done** → "Manage Permissions" navigation bridge to Phase 8 is in place
7. Polish → audit pass, RTL verified, ready for Phase 8 (Role-Permission Matrix)

---

## Task Count Summary

| Phase | Tasks | User Story |
|---|---|---|
| Phase 1 — Setup | 4 | — |
| Phase 2 — Foundational | 14 | — |
| Phase 3 | 4 | US1 Browse & Search |
| Phase 4 | 4 | US2 Create Role |
| Phase 5 | 4 | US3 Edit Role |
| Phase 6 | 2 | US4 Delete Role |
| Phase 7 | 1 | US5 Manage Permissions nav |
| Final Phase — Polish | 3 | — |
| **Total** | **36** | |

### Parallel Opportunities Identified

- Phase 1: T001 and T002 are independent files (parallel)
- Phase 2: T005 (PermissionRepository) is independent from the RoleRepository and service work
- Phase 2: T018 (controller skeleton) is independent from all service/repo work
- US2: T023 (ViewModel) and T024+T025 (controller) and T026 (view) are parallelizable after T023
- US3: T027 (ViewModel) and T028+T029 (controller) and T030 (view) are parallelizable after T027
- Final Phase: T034 and T035 are independent audit tasks (parallel)

### Independent Test Criteria Per Story

| Story | Independent Test |
|---|---|
| US1 | Navigate to `/Roles` — DataTable renders with all 5 columns, search and sort work, counts are correct |
| US2 | Submit Create form with valid data — new role in list with count 0; submit duplicate — inline error |
| US3 | Edit any role's description — updated value appears in list row |
| US4 | Delete unassigned role — row removed; delete role with users — guard message shown in modal |
| US5 | Click "Manage Permissions" — navigates to `/RolePermissions/{id}`; button absent without permission |
