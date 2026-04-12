---
description: "Task list for PHASE 6 — User Management"
---

# Tasks: PHASE 6 — User Management

**Input**: Design documents from `/specs/006-user-management/`  
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/ ✅ quickstart.md ✅  
**Branch**: `006-user-management`  
**Tests**: Not requested — tasks follow implementation-only workflow per spec assumptions.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Shared models used by the DataTables server-side protocol. Required before any controller action can compile. No existing Phase 5 equivalents were found in the repository.

- [X] T001 [P] Create `DataTableRequest` model with Draw, Start, Length, Search, SortColumn, SortDirection properties in `AdminTemplate.Web/Models/DataTableRequest.cs`
- [X] T002 [P] Create `DataTableResponse<T>` generic model with Draw, RecordsTotal, RecordsFiltered, Data properties in `AdminTemplate.Web/Models/DataTableResponse.cs`

**Checkpoint**: Both models compile — controller can now reference them.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The `UsersController` skeleton must exist before any user story action can be implemented. All story phases add actions to this file.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 Create `UsersController` class with `[Authorize]` attribute and constructor injection of `IUserService`, `IRoleService`, `IPermissionService` — no action methods yet in `AdminTemplate.Web/Controllers/UsersController.cs`

**Checkpoint**: Solution builds. `UsersController` is registered via convention routing.

---

## Phase 3: User Story 1 — Browse & Search Users (Priority: P1) 🎯 MVP

**Goal**: A Super Admin navigates to `/Users` and sees a live server-side DataTable with columns for name (initials avatar), email, roles badges, status badge, and actions. Search and column-sort work without page reload.

**Independent Test**: With seeded SuperAdmin, navigate to `/Users`. Confirm DataTable renders, search box filters results server-side, status column shows `.badge-soft-success`/`.badge-soft-danger` badges, and role column shows role name badges. No Create/Edit operations needed.

### Implementation for User Story 1

- [X] T004 [P] [US1] Complete `UserRepository.GetPagedAsync` stub — apply `Contains` search on `FullName` and `Email`, `OrderBy(FullName)`, `Skip`/`Take`, return `(items, total)` tuple in `AdminTemplate.Infrastructure/Repositories/UserRepository.cs`
- [X] T005 [P] [US1] Complete `UserRepository.GetAllAsync` stub — return all users `OrderBy(FullName).ToListAsync()` for interface compliance in `AdminTemplate.Infrastructure/Repositories/UserRepository.cs`
- [X] T006 [US1] Complete `UserService.GetPagedAsync` — apply search on `_userManager.Users`, paginate, iterate results calling `_userManager.GetRolesAsync(u)` per user, map each to `UserDto` in `AdminTemplate.Application/Services/UserService.cs`
- [X] T007 [US1] Add `Index` action (GET /Users) to `UsersController` — set `ViewBag.CanCreate`, `ViewBag.CanUpdate`, `ViewBag.CanDelete` from `IPermissionService.UserHasPermissionAsync("User", ...)`, return `Views/Users/Index.cshtml` in `AdminTemplate.Web/Controllers/UsersController.cs`
- [X] T008 [US1] Add `GetData` action (GET /Users/GetData) to `UsersController` — bind `[FromQuery] DataTableRequest`, call `IUserService.GetPagedAsync(start/length, search)`, return `Json(new DataTableResponse<UserDto>{...})` in `AdminTemplate.Web/Controllers/UsersController.cs`
- [X] T009 [US1] Create `Views/Users/Index.cshtml` — `.page-header` with `<h1>Users</h1>` plus breadcrumb, `.card.table-card` wrapper, `<table id="usersTable">`, `AppDataTable.init({tableId, ajaxUrl, columns with avatar/roles/status renderers, permissions from ViewBag, createUrl, editUrl, objectName:'User'})` in `@section Scripts` in `AdminTemplate.Web/Views/Users/Index.cshtml`
- [X] T010 [US1] Add Users nav link to sidebar — `<li class="nav-item">` with `asp-controller="Users"`, `fa-solid fa-users` icon, `.nav-label` span, active class when controller == "Users" in `AdminTemplate.Web/Views/Shared/_Sidebar.cshtml`

**Checkpoint**: `/Users` loads the DataTable. Server-side search, sort, and pagination work. Status and role columns render with `.badge-soft-*` classes and initials avatar.

---

## Phase 4: User Story 2 — Create a New User (Priority: P2)

**Goal**: An admin clicks Create, fills in full name, email, password (with confirmation), selects one or more roles, sets active state, submits — new user appears in the DataTable. Duplicate email and weak password show inline validation errors without data loss.

**Independent Test**: Navigate to `/Users/Create`. Submit with valid data and confirm redirect to list with new user row. Submit with a duplicate email and confirm validation error is shown. Submit with weak password and confirm per-field error.

### Implementation for User Story 2

- [X] T011 [P] [US2] Add `bool IsActive = true` default parameter to `CreateUserDto` positional record in `AdminTemplate.Application/DTOs/CreateUserDto.cs`
- [X] T012 [P] [US2] Create `CreateUserViewModel` with `[Required]`, `[EmailAddress]`, `[MinLength(8)]`, `[Compare(nameof(Password))]` DataAnnotations and `List<RoleDto> AvailableRoles` (not bound on POST) in `AdminTemplate.Web/ViewModels/Users/CreateUserViewModel.cs`
- [X] T013 [US2] Complete `UserService.CreateAsync` — instantiate `ApplicationUser` from `dto`, call `_userManager.CreateAsync(user, dto.Password)`, on success call `_userManager.AddToRolesAsync(user, dto.Roles)` when roles non-empty, return `IdentityResult` in `AdminTemplate.Application/Services/UserService.cs`
- [X] T014 [US2] Add `Create GET` action to `UsersController` — load `IRoleService.GetAllAsync()`, populate `CreateUserViewModel.AvailableRoles`, default `IsActive = true`, return view in `AdminTemplate.Web/Controllers/UsersController.cs`
- [X] T015 [US2] Add `Create POST` action with `[ValidateAntiForgeryToken]` to `UsersController` — check `ModelState.IsValid`, map ViewModel to `CreateUserDto`, call `IUserService.CreateAsync`, loop `result.Errors` into `ModelState.AddModelError`, on success set `TempData["Success"]` and redirect to `Index` in `AdminTemplate.Web/Controllers/UsersController.cs`
- [X] T016 [US2] Create `Views/Users/Create.cshtml` — `.page-header`, `.card` wrapper with `.card-body`, each field in `.field-icon-wrap` with matching `fa-solid` icon, Bootstrap `select[multiple]` for roles, `.form-check.form-switch` for IsActive, `.alert.alert-danger` summary div with `border-inline-start:4px solid var(--danger)`, `.btn.btn-primary.btn-lg` submit with btn-loading support, `<partial name="_ValidationScriptsPartial" />` in `AdminTemplate.Web/Views/Users/Create.cshtml`

**Checkpoint**: Create form saves valid users. Duplicate email and password complexity failures show validation errors without losing form data. New user appears in the DataTable after redirect.

---

## Phase 5: User Story 3 — Edit an Existing User (Priority: P3)

**Goal**: An admin clicks Edit on a DataTable row, sees a pre-populated form (no password field), updates name/email/roles/active state, saves — the DataTable row reflects the changes. Editing to a duplicate email shows a validation error.

**Independent Test**: Click Edit on any seeded user, change the full name, save, and confirm the updated name appears in the list row. Verify no password field is visible on the form.

### Implementation for User Story 3

- [X] T017 [P] [US3] Complete `UserRepository.GetByIdAsync` stub — `await _userManager.FindByIdAsync(id.ToString())` in `AdminTemplate.Infrastructure/Repositories/UserRepository.cs`
- [X] T018 [P] [US3] Complete `UserRepository.UpdateAsync` stub — `await _userManager.UpdateAsync(user)` (discards result; caller handles errors) in `AdminTemplate.Infrastructure/Repositories/UserRepository.cs`
- [X] T019 [P] [US3] Create `EditUserViewModel` — Id (hidden, `[Required]`), FullName, Email, IsActive, `List<string> SelectedRoles`, `List<RoleDto> AvailableRoles` (not bound on POST) with DataAnnotations in `AdminTemplate.Web/ViewModels/Users/EditUserViewModel.cs`
- [X] T020 [US3] Complete `UserService.GetByIdAsync` — `_userManager.FindByIdAsync(id)`, if null return null; call `_userManager.GetRolesAsync(user)`, map to `UserDto` in `AdminTemplate.Application/Services/UserService.cs`
- [X] T021 [US3] Complete `UserService.UpdateAsync` — find user by id; set `FullName`, `Email`, `UserName`, `NormalizedEmail`, `NormalizedUserName`, `IsActive`; call `_userManager.UpdateAsync`; then `RemoveFromRolesAsync(user, currentRoles)` + `AddToRolesAsync(user, dto.Roles)`; return `IdentityResult.Success` in `AdminTemplate.Application/Services/UserService.cs`
- [X] T022 [US3] Add `Edit GET` action to `UsersController` — call `IUserService.GetByIdAsync(id)`, redirect to Index if null, map `UserDto` to `EditUserViewModel`, set `SelectedRoles` from `UserDto.Roles`, load `AvailableRoles` via `IRoleService.GetAllAsync()`, return view in `AdminTemplate.Web/Controllers/UsersController.cs`
- [X] T023 [US3] Add `Edit POST` action with `[ValidateAntiForgeryToken]` to `UsersController` — check `ModelState.IsValid`, build `UpdateUserDto` from ViewModel, call `IUserService.UpdateAsync(id, dto)`, loop `result.Errors` into `ModelState`, reload `AvailableRoles` on failure, on success set `TempData["Success"]` and redirect to `Index` in `AdminTemplate.Web/Controllers/UsersController.cs`
- [X] T024 [US3] Create `Views/Users/Edit.cshtml` — same design patterns as `Create.cshtml`, hidden `<input asp-for="Id" />`, no password/confirm-password fields, pre-selected roles from `Model.SelectedRoles`, `<partial name="_ValidationScriptsPartial" />` in `AdminTemplate.Web/Views/Users/Edit.cshtml`

**Checkpoint**: Edit form pre-populates all fields. Saving updates name, email, roles, and active state. Password field is absent. Duplicate email shows validation error.

---

## Phase 6: User Story 4 — Activate / Deactivate a User (Priority: P4)

**Goal**: On the Users list, each row has an Activate/Deactivate button. Clicking it posts to `/Users/ToggleActive` via AJAX and updates the status badge in-place without a page reload. A deactivated user cannot log in.

**Independent Test**: Click the Deactivate button on an active user row. Confirm the status badge changes to `.badge-soft-danger` "Inactive" without a page reload. Attempt to log in as that user and confirm the account is rejected.

### Implementation for User Story 4

- [X] T025 [US4] Complete `UserService.SetActiveAsync` — `_userManager.FindByIdAsync(id)`, return `IdentityResult.Failed` if null, set `user.IsActive = isActive`, return `await _userManager.UpdateAsync(user)` in `AdminTemplate.Application/Services/UserService.cs`
- [X] T026 [US4] Add `ToggleActive POST` action with `[ValidateAntiForgeryToken]` to `UsersController` — accept `string id` and `bool isActive` from form body, call `IUserService.SetActiveAsync`, return `Json(new { success = true })` on success or `Json(new { success = false, error = result.Errors.First().Description })` on failure in `AdminTemplate.Web/Controllers/UsersController.cs`
- [X] T027 [US4] Add per-row activate/deactivate action to `Views/Users/Index.cshtml` — render a toggle button in the actions column with `data-id` and `data-active` attributes; on click, `fetch` a POST to `/Users/ToggleActive` with CSRF token and form body; on success swap the row's status badge class between `.badge-soft-success` and `.badge-soft-danger` and update button state in `AdminTemplate.Web/Views/Users/Index.cshtml`

**Checkpoint**: Activate/deactivate AJAX round-trip completes < 1 second. Badge updates in-place. Deactivated user is blocked at login.

---

## Phase 7: User Story 5 — Permission-Gated Access (Priority: P5)

**Goal**: The module enforces "User" object permissions at every entry point. Users lacking Browse cannot reach the list. The Create/Edit/Deactivate buttons are hidden when the session user lacks the corresponding permission. The sidebar nav link is hidden when Browse is absent.

**Independent Test**: Log in as a role with only "User → Browse". Confirm Create button is absent from the DataTable toolbar. Navigate directly to `/Users/Create` and confirm the custom `Error403.cshtml` page is returned.

### Implementation for User Story 5

- [X] T028 [P] [US5] Add `[HasPermission("User", "Browse")]` to `Index` and `GetData` actions, `[HasPermission("User", "Create")]` to both `Create` actions, `[HasPermission("User", "Update")]` to both `Edit` actions and `ToggleActive` in `AdminTemplate.Web/Controllers/UsersController.cs`
- [X] T029 [P] [US5] Verify `UsersController.Index` resolves `canCreate`, `canUpdate`, `canDelete` via `IPermissionService.UserHasPermissionAsync("User", "Create/Update/Delete")` and assigns to `ViewBag`; verify `Index.cshtml` passes them as `permissions: { canCreate: @Json.Serialize(ViewBag.CanCreate), canUpdate: @Json.Serialize(ViewBag.CanUpdate), canDelete: false }` in `AppDataTable.init` config in `AdminTemplate.Web/Controllers/UsersController.cs` and `AdminTemplate.Web/Views/Users/Index.cshtml`
- [X] T030 [US5] Update Users nav item in `_Sidebar.cshtml` — resolve `IPermissionService` via `Context.RequestServices`, add `d-none` CSS class to the `<li>` when user lacks "User → Browse" permission in `AdminTemplate.Web/Views/Shared/_Sidebar.cshtml`

**Checkpoint**: All permission gates verified. Browse-only users see the list but no Create button. Users without Browse see the 403 page. Sidebar link hides for users without Browse permission.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Security hardening and design system compliance audit across all new files.

- [X] T031 [P] Audit all `UsersController` POST actions (`Create`, `Edit`, `ToggleActive`) — confirm each has `[ValidateAntiForgeryToken]` attribute and CSRF token is present in the corresponding form/fetch call in `AdminTemplate.Web/Controllers/UsersController.cs` and `AdminTemplate.Web/Views/Users/`
- [X] T032 [P] Design system audit — verify all new views use only `var(--*)` CSS custom properties (no hardcoded hex values), use `.badge-soft-success/danger` for status, `.field-icon-wrap` for inputs, `.page-header` pattern, and CSS logical properties (`margin-inline-start`, `padding-inline-end`) — no `margin-left`/`padding-right` in `AdminTemplate.Web/Views/Users/`
- [X] T033 Run through `quickstart.md` verification checklist end-to-end — confirm all 15 items pass including RTL toggle, zero-user empty state, and server error retry state

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — first independently testable increment
- **US2 (Phase 4)**: Depends on Phase 2 — independently testable; does not require US1
- **US3 (Phase 5)**: Depends on Phase 2 — independently testable; does not require US1 or US2
- **US4 (Phase 6)**: Depends on US1 (Phase 3) — adds to `Index.cshtml` and requires the list to exist
- **US5 (Phase 7)**: Depends on US1–US4 — permission attributes must be applied to existing actions
- **Polish (Final Phase)**: Depends on all user stories

### User Story Dependencies

| Story | Depends On | Notes |
|---|---|---|
| US1 — Browse | Phase 2 only | Independent MVP |
| US2 — Create | Phase 2 only | Independent from US1 |
| US3 — Edit | Phase 2 + US2 (CreateUserDto update) | `EditUserViewModel` pattern mirrors CreateUserViewModel |
| US4 — Toggle | US1 (shares Index.cshtml) | Adds to existing Index view |
| US5 — Permissions | All other stories | Cross-cutting — applied after actions exist |

### Parallel Opportunities Per Story

```
# Phase 1 — both models are independent files:
T001: DataTableRequest.cs
T002: DataTableResponse.cs

# US1 — repository stubs are independent files from service stub:
T004: UserRepository.GetPagedAsync
T005: UserRepository.GetAllAsync

# US2 — DTO change and ViewModel are independent:
T011: CreateUserDto.cs (add IsActive)
T012: CreateUserViewModel.cs (new file)

# US3 — repository stubs, service stubs, and ViewModel are independent:
T017: UserRepository.GetByIdAsync
T018: UserRepository.UpdateAsync
T019: EditUserViewModel.cs

# US5 — attribute application and flag verification are independent:
T028: [HasPermission] attributes
T029: canCreate/canUpdate/canDelete flag verification

# Polish — audit tasks are independent:
T031: CSRF audit
T032: Design system audit
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete **Phase 1**: DataTableRequest + DataTableResponse
2. Complete **Phase 2**: UsersController skeleton
3. Complete **Phase 3** (US1): Repository stubs → UserService.GetPagedAsync → Index + GetData actions → Index.cshtml → Sidebar link
4. **STOP and VALIDATE**: Navigate to `/Users` as SuperAdmin — DataTable loads, search works, badges render correctly
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → skeleton in place
2. **US1 done** → Users list is live *(MVP!)*
3. **US2 done** → Admins can create new users
4. **US3 done** → Admins can edit existing users
5. **US4 done** → Admins can activate/deactivate inline
6. **US5 done** → Module is fully permission-secured
7. Polish → audit pass, RTL verified, ready for Phase 7 (Role Management)

---

## Task Count Summary

| Phase | Tasks | User Story |
|---|---|---|
| Phase 1: Setup | 2 | — |
| Phase 2: Foundational | 1 | — |
| Phase 3 | 7 | US1 — Browse & Search |
| Phase 4 | 6 | US2 — Create |
| Phase 5 | 8 | US3 — Edit |
| Phase 6 | 3 | US4 — Activate/Deactivate |
| Phase 7 | 3 | US5 — Permission Gates |
| Polish | 3 | — |
| **Total** | **33** | |

### Parallel Opportunities

- **Phase 1**: T001 ∥ T002
- **US1**: T004 ∥ T005 (repository stubs)
- **US2**: T011 ∥ T012 (DTO + ViewModel)
- **US3**: T017 ∥ T018 ∥ T019 (repository stubs + ViewModel)
- **US5**: T028 ∥ T029 (attribute application + flag check)
- **Polish**: T031 ∥ T032 (CSRF audit + design system audit)

### Independent Test Criteria Per Story

| Story | Independent Test |
|---|---|
| US1 | Navigate to `/Users` — DataTable renders with server-side pagination, search, sort |
| US2 | Submit Create form with valid data — user appears in list; duplicate email shows error |
| US3 | Edit seeded user full name — updated name visible in list; no password field present |
| US4 | Click Deactivate — badge changes in-place; deactivated user blocked at login |
| US5 | Browse-only role — Create button absent; direct `/Users/Create` returns 403 |

### Suggested MVP Scope

**User Story 1 only** (7 tasks after setup): delivers a fully functional, read-only server-side DataTable that any admin can use to browse and search users immediately.
