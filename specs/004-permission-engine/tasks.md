# Tasks: Permission Engine

**Input**: Design documents from `/specs/004-permission-engine/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: No test tasks — not requested in spec or user stories.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1–US4 as defined in spec.md)
- Exact file paths are included in all descriptions

---

## Phase 1: Setup

**Purpose**: No new project scaffolding required — solution structure, DI registrations, database, `permissions.json`, `JsonPermissionProvider`, `RolePermission` entity, and all repository scaffolding are in place from Phase 1. No migration is needed.

*(No setup tasks — proceed directly to Foundational.)*

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extend the two Domain interfaces and implement the two new repository methods that every user story depends on. These fulfil the data-access contract required by `PermissionService` before any story work can begin.

**⚠️ CRITICAL**: T001–T004 must all be complete before US1, US3, and US4 implementation can begin.

- [ ] T001 Add method `Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId)` to the interface in `AdminTemplate.Domain/Interfaces/IUserRepository.cs` — append after the existing `GetByEmailAsync` declaration; no other changes
- [ ] T002 [P] Add method `Task<bool> UserHasPermissionAsync(Guid userId, string objectName, string functionName)` to the interface in `AdminTemplate.Domain/Interfaces/IPermissionRepository.cs` — append after the existing `ExistsAsync` declaration; no other changes
- [ ] T003 [P] Implement `GetRoleNamesAsync(Guid userId)` in `AdminTemplate.Infrastructure/Repositories/UserRepository.cs` — call `_userManager.FindByIdAsync(userId.ToString())`; return empty `[]` if user is null; else call `_userManager.GetRolesAsync(user)` and return `roles.ToList().AsReadOnly()`
- [ ] T004 [P] Implement `UserHasPermissionAsync(Guid userId, string objectName, string functionName)` in `AdminTemplate.Infrastructure/Repositories/PermissionRepository.cs` — use a single EF Core LINQ query joining `_context.UserRoles` (AspNetUserRoles join table from `IdentityDbContext`) to `_context.RolePermissions` on `RoleId`; call `.AnyAsync(x => x.UserId == userId && x.ObjectName == objectName && x.FunctionName == functionName)`; return the bool result

**Checkpoint**: Foundation ready — `IUserRepository` and `IPermissionRepository` have full implementations for all methods. User story implementation can now begin.

---

## Phase 3: User Story 1 — Enforcing Permission-Based Access Control (Priority: P1) 🎯 MVP

**Goal**: Protect controller actions with a single declarative attribute. Authenticated users who lack the required permission see a branded 403 page. Unauthenticated users are redirected to login.

**Independent Test**: Add `[HasPermission("Employee", "Create")]` to a stub action on any controller. Log in as the seeded SuperAdmin → confirm access. Create a second user with no permissions → navigate to the same URL → confirm the styled 403 page renders with the admin sidebar and topnav.

### Implementation for User Story 1

- [ ] T005 [US1] Update `AdminTemplate.Application/Services/PermissionService.cs` — add `private const string SuperAdminRoleName = "SuperAdmin"`; update constructor to inject `IUserRepository _userRepository` and `IPermissionRepository _permissionRepository`; implement `UserHasPermissionAsync`: call `_userRepository.GetRoleNamesAsync(userId)` first — if result contains `SuperAdminRoleName` (case-insensitive) return `true` immediately; otherwise call and return `_permissionRepository.UserHasPermissionAsync(userId, objectName, functionName)`
- [ ] T006 [P] [US1] Create `AdminTemplate.Web/Filters/HasPermissionAttribute.cs` — declare `[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]`; implement `Attribute, IAsyncAuthorizationFilter`; constructor takes `(string objectName, string functionName)`; in `OnAuthorizationAsync`: if `user.Identity?.IsAuthenticated != true` set `context.Result = new ChallengeResult()` and return; parse `Guid userId` from `ClaimTypes.NameIdentifier` — if parse fails set `context.Result = new ForbidResult()` and return; resolve `IPermissionService` from `context.HttpContext.RequestServices.GetRequiredService<IPermissionService>()`; `await permissionService.UserHasPermissionAsync(userId, _objectName, _functionName)` — if false set `context.Result = new ViewResult { ViewName = "~/Views/Shared/Error403.cshtml", StatusCode = StatusCodes.Status403Forbidden }`
- [ ] T007 [P] [US1] Create `AdminTemplate.Web/Views/Shared/Error403.cshtml` — set `ViewData["Title"] = "Access Denied"` and `Layout = "_Layout"`; render a Bootstrap card centred in `container-fluid py-4`: Font Awesome lock icon `fa-lock fa-4x text-muted`, heading `403 — Access Denied`, muted paragraph explaining the restriction, and an `<a asp-controller="Dashboard" asp-action="Index">` button with `btn btn-primary`; no inline styles

**Checkpoint**: US1 complete — `[HasPermission]` attribute blocks unauthorized users with a branded 403 page; SuperAdmin passes through; unauthenticated users are redirected to login.

---

## Phase 4: User Story 2 — Loading Permissions from Configuration (Priority: P1)

**Goal**: The application loads all permission objects and functions from `permissions.json` at startup without code changes; this data is surfaced via `PermissionService.GetRolePermissionsAsync` so calling code can read what permissions a role currently holds.

**Independent Test**: Verify `JsonPermissionProvider` is already returning all 4 objects (Employee, User, Role, Report) at startup. Run the app and confirm it starts without errors. Then verify `DataSeeder` has correctly seeded all permissions for SuperAdmin by querying the `RolePermissions` table — expect 4+4+5+2 = 15 rows for the SuperAdmin role.

*(Note: `JsonPermissionProvider` is fully implemented from Phase 1. `DataSeeder` already iterates `IPermissionProvider.GetAll()`. No changes needed to those files. US2's remaining implementation task is completing `GetRolePermissionsAsync` to surface persisted data.)*

### Implementation for User Story 2

- [ ] T008 [US2] Implement `GetRolePermissionsAsync(Guid roleId)` in `AdminTemplate.Application/Services/PermissionService.cs` — call `await _permissionRepository.GetByRoleIdAsync(roleId)`; map each `RolePermission` entity to `new PermissionDto(entity.ObjectName, entity.FunctionName)`; return as `IReadOnlyList<PermissionDto>` via `.ToList().AsReadOnly()`; return an empty list (not null) if no permissions are assigned

**Checkpoint**: US2 complete — configuration loads at startup, startup fails loudly if `permissions.json` is missing, and `GetRolePermissionsAsync` correctly surfaces what permissions a role has been assigned.

---

## Phase 5: User Story 3 — Checking Permissions Programmatically in Views (Priority: P2)

**Goal**: Every Razor view in the app can conditionally show or hide UI elements (buttons, links) based on the current user's permissions with a single `PermissionService.CurrentUserHasPermissionAsync(User, object, function)` call — no boilerplate per page.

**Independent Test**: In any existing admin view (e.g., `Dashboard/Index.cshtml`), add a conditional block `@if (await PermissionService.CurrentUserHasPermissionAsync(User, "Employee", "Export")) { <span>Export visible</span> }`. Log in as SuperAdmin → span visible. Log in as a user with no permissions → span absent. Remove the test markup when done.

### Implementation for User Story 3

- [ ] T009 [US3] Create `AdminTemplate.Application/Services/PermissionServiceExtensions.cs` — static class in namespace `AdminTemplate.Application.Services`; single public static async method `CurrentUserHasPermissionAsync(this IPermissionService service, ClaimsPrincipal user, string objectName, string functionName) : Task<bool>`; return `false` if `user.Identity?.IsAuthenticated != true`; parse `Guid userId` from `user.FindFirstValue(ClaimTypes.NameIdentifier)` — return `false` if parse fails; otherwise `return await service.UserHasPermissionAsync(userId, objectName, functionName)`; add `using System.Security.Claims`
- [ ] T010 [P] [US3] Update `AdminTemplate.Web/Views/_ViewImports.cshtml` — append two lines: `@inject AdminTemplate.Application.Interfaces.IPermissionService PermissionService` and `@using AdminTemplate.Application.Services`; leave the three existing lines (`@using AdminTemplate.Web`, `@using AdminTemplate.Web.Models`, `@addTagHelper`) unchanged

**Checkpoint**: US3 complete — all Razor views can call `await PermissionService.CurrentUserHasPermissionAsync(User, "ObjectName", "FunctionName")` without any per-page setup.

---

## Phase 6: User Story 4 — Assigning and Persisting Permissions to Roles (Priority: P2)

**Goal**: A batch of object-function pairs can be saved for a role and will be persisted correctly in the database, replacing any previous assignment. Changes take effect immediately on the user's next request.

**Independent Test**: Using a temporary test action or debug code, call `PermissionService.SaveRolePermissionsAsync(roleId, new[] { new PermissionDto("Employee", "Browse") })` for a non-SuperAdmin role. Verify the `RolePermissions` table has exactly one row for that role. Call again with a different set — verify the first row is gone and the new rows exist. Log in as a user with that role → the enforced permission matches.

### Implementation for User Story 4

- [ ] T011 [US4] Implement `SaveRolePermissionsAsync(Guid roleId, IEnumerable<PermissionDto> permissions)` in `AdminTemplate.Application/Services/PermissionService.cs` — call `await _permissionRepository.DeleteByRoleIdAsync(roleId)` first (removes all existing entries for that role); then build a sequence of `RolePermission` entities from the `permissions` parameter setting `RoleId`, `ObjectName`, `FunctionName`, and `CreatedAt = DateTime.UtcNow`; call `await _permissionRepository.AddRangeAsync(entities)`; passing an empty enumerable must result in all permissions being removed with no inserts

**Checkpoint**: US4 complete — `SaveRolePermissionsAsync` replaces all permissions for a role atomically; Phase 8 Role-Permission Matrix page can now be built using this method.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verify the complete permission engine builds cleanly and end-to-end behaviour matches spec acceptance criteria.

- [ ] T012 [P] Build the full solution (`AdminTemplate.sln`) and confirm 0 build errors and 0 warnings — pay attention to any `NotImplementedException` remaining in `PermissionService`; fix any compile errors before proceeding to T013
- [ ] T013 Manual smoke test following quickstart.md Step 8: (1) start the app; (2) log in as seeded SuperAdmin; (3) navigate to any URL decorated with `[HasPermission("Employee","Create")]` — confirm access; (4) create a second user via the register page with no roles assigned; (5) log in as that user; (6) navigate to the same URL — confirm `Error403.cshtml` is rendered with the admin sidebar visible and HTTP status is 403; (7) confirm that navigating to the same URL while logged out redirects to the Login page instead of showing 403

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: No dependencies — can start immediately. **BLOCKS all user stories.**
- **US1 (Phase 3)**: Depends on T001–T004 (Foundational). No dependency on US2, US3, or US4.
- **US2 (Phase 4)**: Depends on T001–T004 (Foundational). No dependency on US1, US3, or US4.
- **US3 (Phase 5)**: Depends on T001–T004 (Foundational). No dependency on US1, US2, or US4.
- **US4 (Phase 6)**: Depends on T001–T004 (Foundational). No dependency on US1, US2, or US3.
- **Polish (Phase 7)**: Depends on all user story phases being complete.

### User Story Dependencies

- **US1 (P1)**: Can start immediately after Foundational — no dependency on US2, US3, or US4
- **US2 (P1)**: Can start immediately after Foundational — no dependency on US1, US3, or US4. (`JsonPermissionProvider` and `DataSeeder` already work; only T008 is needed.)
- **US3 (P2)**: Can start immediately after Foundational — no dependency on US1, US2, or US4
- **US4 (P2)**: Can start immediately after Foundational — no dependency on US1, US2, or US3

### Within Each User Story

- US1: T005 (service) before T006 (filter) because filter constructs its error result calling the live service *at runtime*, but T006 can be written and compiled in parallel since it calls `IPermissionService` (interface already declared in Phase 1)
- US3: T009 (extension method) before T010 (_ViewImports) so compilation succeeds after the inject line is added
- US4: T011 has no internal ordering constraints (single task)
- Polish: T012 (build) before T013 (smoke test)

---

## Parallel Execution Examples

### Execute Foundational (Phase 2) as a batch

```text
Start simultaneously:
  T001 — Extend IUserRepository
  T002 — Extend IPermissionRepository

Then start simultaneously once T001 is done:
  T003 — Implement UserRepository.GetRoleNamesAsync

And once T002 is done:
  T004 — Implement PermissionRepository.UserHasPermissionAsync
```

### Execute after Foundational is complete — US1, US2, US3 in parallel

```text
Once T001–T004 are done, start all of these simultaneously:

  Thread A (US1):
    T005 — PermissionService.UserHasPermissionAsync
    T006 — HasPermissionAttribute.cs       [P] with T005
    T007 — Error403.cshtml                 [P] with T005, T006

  Thread B (US2):
    T008 — PermissionService.GetRolePermissionsAsync

  Thread C (US3):
    T009 — PermissionServiceExtensions.cs
    T010 — _ViewImports.cshtml             [P] with T009

  Thread D (US4):
    T011 — PermissionService.SaveRolePermissionsAsync
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 2: Foundational (T001–T004) — CRITICAL, blocks everything
2. Complete Phase 3: US1 (T005, T006, T007)
3. **STOP and VALIDATE**: Smoke test 403 enforcement with SuperAdmin vs limited user
4. The permission *engine* is live — all future phases that use `[HasPermission]` can now proceed

### Incremental Delivery

1. Foundational (T001–T004) → Repository layer complete
2. US1 (T005–T007) → Enforcement live → MVP! Any action can be protected immediately
3. US2 (T008) → `GetRolePermissionsAsync` complete → Phase 8 manager page unblocked
4. US3 (T009–T010) → View permission checks live → All management page UIs can hide/show buttons
5. US4 (T011) → `SaveRolePermissionsAsync` complete → Phase 8 can now persist permission assignments
6. Polish (T012–T013) → Build clean, smoke test confirmed
