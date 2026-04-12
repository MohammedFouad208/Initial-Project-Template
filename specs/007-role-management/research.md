# Research: PHASE 7 — Role Management

**Phase**: 0 — Pre-Design Research  
**Branch**: `007-role-management`  
**Date**: 2026-04-12  
**Purpose**: Resolve all unknowns from the Technical Context before Phase 1 design

---

## R-001 — Computing UserCount and PermissionCount in GetPagedAsync

**Question**: `RoleDto` exposes `UserCount` and `PermissionCount`. `GetPagedAsync` in `RoleService` must supply these. What is the most efficient approach given that `RoleService` has `RoleManager<ApplicationRole>` but not `ApplicationDbContext` directly?

**Decision**: Inject `IPermissionRepository` (already registered in DI) into `RoleService` for the permission count, and use `UserManager<ApplicationUser>` (also available in the `RoleRepository`) for the user count. The `RoleRepository.GetPagedAsync` implementation queries `AspNetRoles` via `RoleManager.Roles` (EF queryable), then for each page of results performs two secondary queries: one to `ApplicationDbContext.RolePermissions.Count(rp => rp.RoleId == role.Id)` and one to `UserManager.GetUsersInRoleAsync(role.Name).Count`. Since pages are at most 25 rows, N+1 is acceptable at this scale.

**For the service layer** (`RoleService.GetPagedAsync`): call `IRoleRepository.GetPagedAsync` to retrieve the `ApplicationRole` page, then — for each role — call `IRoleRepository.HasUsersAsync` count variant and `IPermissionRepository.GetByRoleIdAsync` to compute counts. Alternatively, the clean solution is to add a dedicated `GetCountByRoleIdAsync(Guid roleId)` method to `IPermissionRepository` so no list is materialized just for a count.

**Decision (final)**: Add `Task<int> GetCountByRoleIdAsync(Guid roleId)` to `IPermissionRepository` and its implementation. Add `Task<int> GetUserCountAsync(Guid roleId)` to `IRoleRepository` and its implementation (using `UserManager.GetUsersInRoleAsync`). This keeps count logic in the repository layer and avoids materializing full lists.

**Rationale**: Keeps `ApplicationDbContext` in Infrastructure, avoids cross-layer leakage, and uses existing abstractions. Performance is adequate for admin panels with role counts up to 1,000.

**Alternatives considered**:
- Raw SQL via `ApplicationDbContext` in the service — violates Constitution Principle IV (no `DbContext` outside Infrastructure).
- Single EF projection query joining `AspNetUserRoles` and `RolePermissions` — possible but requires exposing a complex DTO from the repository, overcomplicating the contract for a low-frequency admin page.

---

## R-002 — SuperAdmin Role Delete Guard

**Question**: The spec requires the SuperAdmin role to be undeletable through the UI. The `DataSeeder` uses the string constant `"SuperAdmin"` for this role. Where and how should the guard be implemented?

**Decision**: Implement the guard in `RoleService.DeleteAsync`. Before calling `IRoleRepository.DeleteAsync`, check if the role name equals `"SuperAdmin"` (case-insensitive). If so, return an `IdentityResult.Failed(new IdentityError { Code = "ProtectedRole", Description = "The SuperAdmin role cannot be deleted." })`. Use the same constant (`SuperAdminRoleName = "SuperAdmin"`) already in use in `PermissionService` and `DataSeeder`.

**Rationale**: The guard belongs in the Application service (business rule layer), not the repository (data access), and not in the controller (too thin). Returning `IdentityResult.Failed` is consistent with the existing error-handling pattern used by `CreateAsync` and `UpdateAsync`, which the controller already knows how to process.

**Alternatives considered**:
- Controller-level check — violates Constitution Principle III (no business logic in controllers).
- Database constraint or trigger — overkill, non-portable, not declared in the migration plan.
- Hard-coding the guard in the UI (hiding Delete for the SuperAdmin row) — security by obscurity; must be enforced at the service layer.

---

## R-003 — Duplicate Role Name Validation

**Question**: `RoleManager<ApplicationRole>` normalizes role names. When a duplicate is submitted, `CreateAsync` returns `IdentityResult.Failed` with code `DuplicateRoleName`. Should `RoleService` add a pre-check or rely on Identity's own error?

**Decision**: Rely entirely on `RoleManager`'s built-in duplicate detection. `RoleService.CreateAsync` calls `_roleManager.CreateAsync(role)` and returns the `IdentityResult` directly. If the name is a duplicate, Identity returns `DuplicateRoleName` error. The controller maps `IdentityResult.Errors` to `ModelState` — identical to the approach used in `UserService` and `AccountService`. No pre-lookup before the create is needed.

**Rationale**: Avoids a redundant database round-trip. Identity normalizes the name before creating, so the uniqueness check is atomic and race-condition-safe. Pre-checks introduce a TOCTOU window.

**Alternatives considered**:
- `await _roleManager.FindByNameAsync(dto.Name)` before create — adds one extra query; still subject to race condition; rejected.
- Custom `UniqueAttribute` on the ViewModel — client-side only; security-unaware.

---

## R-004 — "Manage Permissions" Button in the DataTable

**Question**: The spec requires a "Manage Permissions" button per row in the Roles DataTable. The generic `AppDataTable.init` config supports `editUrl` and `deleteUrl`. How should a third custom action URL be added without modifying the generic `datatable.js` from Phase 5?

**Decision**: Use the `AppDataTable.init` **`extraActions`** config key (if Phase 5 supports it) or the **column `render` function** override. Since the generic `datatable.js` config object already supports a `columns` array where individual columns can supply a custom `render` function, define the Actions column as a custom `render` column in the `Roles/Index.cshtml` script block. This column renders the Edit, Delete, and Manage Permissions buttons together using the row's `id` to build the URL `/RolePermissions/{id}`.

**Specifically**: In `AppDataTable.init`, pass a custom `actionsRenderer` function or define the actions column inline with:
```js
{
  data: 'id',
  title: 'Actions',
  orderable: false,
  render: function(id) {
    return `
      <a href="/Roles/Edit/${id}" class="btn btn-outline-secondary btn-sm">
        <i class="fa fa-pencil"></i>
      </a>
      ${canDelete ? `<button class="btn btn-outline-danger btn-sm dt-delete" data-id="${id}">
        <i class="fa fa-trash"></i>
      </button>` : ''}
      ${canManagePermissions ? `<a href="/RolePermissions/${id}" class="btn btn-outline-primary btn-sm">
        <i class="fa fa-key"></i>
      </a>` : ''}
    `;
  }
}
```
The `canManagePermissions` flag is resolved server-side from `IPermissionService.UserHasPermissionAsync(userId, "Role", "AssignPermissions")` and serialized into the page.

**Rationale**: Keeps `datatable.js` unmodified (Constitution Principle VI — no per-page DataTable modifications). The render function is the official DataTables.net extensibility mechanism. No changes to generic infrastructure.

**Alternatives considered**:
- Adding an `extraButtons` array to `AppDataTable.init` signature — requires modifying `datatable.js`; violates the "generic, unmodified" constraint.
- Server rendering the button in the JSON response — leaks UI concerns into the data layer; rejected.

---

## R-005 — Delete POST: Return Pattern for Guard Failure

**Question**: When delete is blocked (role has users, or is SuperAdmin), the spec requires a styled `.alert.alert-danger` — not a raw exception. The delete action is a POST from the DataTable delete modal. What response format should the controller return?

**Decision**: The delete action returns JSON, consistent with the generic DataTable delete flow from Phase 5. On success: `{ "success": true }`. On guard failure: `{ "success": false, "message": "Cannot delete this role because it has assigned users." }`. The Phase 5 `datatable.js` delete handler already checks `response.success` and shows the Bootstrap modal with the message if false. No extra view is needed.

If Phase 5's delete handler does not natively support `success: false` with a message display, the `Roles/Index.cshtml` script block adds a one-time override after `AppDataTable.init` to intercept the response.

**Rationale**: Consistent with the existing DataTable delete pattern. Returns a machine-readable response for the JS handler. Avoids redirect-on-error (which would lose the user's list context). Styled presentation is the JS layer's responsibility.

**Alternatives considered**:
- Redirect to Index with TempData error — loses the DataTable state (current page, search); poor UX.
- HTTP 400 with body — works but less explicit than `{ success: false }` for client-side handling.
- Razor partial reload — requires a server round-trip for a simple error message; overkill.

---

## R-006 — RoleRepository.GetPagedAsync: Sorting Strategy

**Question**: The Roles list DataTable supports server-side sorting by column index. What columns are sortable, and how does the repository translate a column index to an EF `OrderBy`?

**Decision**: Define the column order in the `RolesController.GetData` action (matching the order defined in `AppDataTable.init` columns config). Map index to field name using a local array: `["name", "description", "userCount", "permissionCount", "createdAt"]`. Pass the resolved field name (as a string) to `IRoleRepository.GetPagedAsync` as a `sortColumn` parameter (replacing the integer index). The repository implementation uses a `switch` on the field name to apply the correct `OrderBy`.

`UserCount` and `PermissionCount` are computed post-query (not EF columns), so sorting by them requires materializing the page first and then sorting in memory — or accepting that these columns are orderable client-side only. Since server-side sort by computed counts is unusual in admin panels of this scale, **sorting by `userCount` and `permissionCount` is not implemented server-side**; those columns are marked `orderable: false` in the DataTable config.

**Rationale**: Avoids complex subquery joins for count-based ordering. Name, description, and creation date are sufficient sortable columns for an admin role list. Consistent with the pattern established in `UsersController.GetData` for Phase 6.

**Alternatives considered**:
- EF `GroupJoin` to compute counts in a single query with `OrderBy` — possible but produces complex SQL; not worth the complexity at this scale.
- Changing `sortColumn` to accept a field name string from the client — adds XSS/injection risk if not sanitized; the index-to-name server-side mapping is the safe pattern.
