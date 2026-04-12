# Research: Role-Permission Management (Phase 8)

**Branch**: `008-role-permission-management`  
**Produced by**: `/speckit.plan` — Phase 0

---

## Decision 1: Form Submission Strategy (Batch POST)

**Decision**: Standard HTML form POST using checkboxes with `name="selectedPermissions"` and `value="ObjectName|FunctionName"` (pipe-delimited composite key). Submitted via standard `<form>` POST → redirect (PRG pattern).

**Rationale**: The spec requires POST-redirect-GET with a TempData success alert. Standard form POST is the simplest approach. Pipe-delimited values produce a `List<string>` on the action parameter — zero custom model binding. CSRF protection is trivially provided by `[ValidateAntiForgeryToken]` on the POST action.

**Alternatives considered**:
- *AJAX JSON POST*: Would break the PRG pattern and require JS to render TempData alerts client-side; adds complexity for no benefit given the matrix is always fully included in the form.
- *Complex model binding (`permissions[i].ObjectName`)*: Works but requires a ViewModel with a nested list, and form rendering is more error-prone with a dynamic matrix size.

---

## Decision 2: ViewModel Shape for the Matrix

**Decision**: A single `RolePermissionsViewModel` carrying:
- `string RoleId` — for the hidden form field and breadcrumb link
- `string RoleName` — for the page header
- `IReadOnlyList<PermissionObjectDto> AllObjects` — sourced from `IPermissionProvider.GetAll()`, drives all rows and column headers
- `HashSet<string> AssignedKeys` — a pre-computed set of `"ObjectName|FunctionName"` strings for O(1) checkbox state lookup in Razor (`AssignedKeys.Contains("Employee|Create")`)

**Rationale**: Separating "available objects" (from JSON) and "assigned keys" (from DB) is clean and avoids a nested structure. The `HashSet<string>` lookup is fast and readable in Razor without LINQ per-cell.

**Alternatives considered**:
- *Nested ViewModel with `bool IsChecked` per cell*: Requires a 2D structure (`List<ObjectPermissionRow>` with `List<FunctionPermissionCell>`). More type-safe but harder to build and unnecessary given the small matrix size.
- *Dictionary lookup*: `Dictionary<string, HashSet<string>>` keyed by ObjectName → functions. Slightly faster lookup but adds Razor complexity for no gain given typical matrix size (<50 cells).

---

## Decision 3: Sticky First Column Implementation

**Decision**: CSS `position: sticky; inset-inline-start: 0` on the object name `<td>` and corresponding `<th>`. Background set to `var(--bg-card)` to prevent overlap bleed. This is a pure CSS solution scoped to the matrix table — no separate CSS file needed; it can live in a `<style>` block on the view or be added to `site.css` under a `.permission-matrix` selector.

**Rationale**: Logical property `inset-inline-start` ensures the sticky column works in both LTR and RTL mode automatically — consistent with the constitutional requirement to use logical properties throughout.

**Alternatives considered**:
- *JavaScript scroll handler*: Brittle, adds JS weight, and fights with the browser's native sticky behavior.
- *`left: 0` (physical property)*: Violates the constitutional constraint against direction-specific CSS properties in shared CSS.

---

## Decision 4: Column-Header Toggle Behavior

**Decision**: A click on a function column header checkbox toggles all `input[type=checkbox]` cells in that column to match the header checkbox state. Implemented with pure vanilla JS in the view's `@section Scripts` block. "Check All" / "Uncheck All" per row is similarly a button with a `data-row` attribute.

**Rationale**: Simple, no library needed. Keeps everything in one place (the view). This is purely view-layer JS — no server call, no state management.

**Alternatives considered**:
- *Alpine.js or a reactive framework*: Overkill for a simple checkbox grid; would introduce a dependency not in the project's stack.

---

## Decision 5: No New Infrastructure Required

**Decision**: Phase 8 requires **zero new repositories, services, or infrastructure classes**. All required service methods already exist:
- `IPermissionProvider.GetAll()` — provides all permission objects/functions from JSON ✅
- `IPermissionService.GetRolePermissionsAsync(roleId)` — returns existing assignments ✅
- `IPermissionService.SaveRolePermissionsAsync(roleId, permissions)` — replaces all permissions ✅
- `IRoleService.GetByIdAsync(id)` — provides role name for the page header ✅
- `HasPermissionAttribute` — authentication filter for the controller ✅

All are registered in DI in `InfrastructureServiceExtensions.cs`. The new controller simply wires them together.

**Rationale**: The full service and repository layer was correctly built in Phases 4 and 7. Phase 8 is a pure Web-layer feature (Controller + ViewModel + View).

---

## Decision 6: roleId Type Handling

**Decision**: The controller action uses `string roleId` (not `Guid`), consistent with ASP.NET Core Identity's use of `string` for role IDs throughout the project (`IRoleService.GetByIdAsync(string id)`). When passing to `IPermissionService`, parse to `Guid` with `Guid.TryParse` and return 404 if invalid.

**Rationale**: Matches the existing pattern in `RolesController` and all service interfaces. The "Manage Permissions" link in `Roles/Index.cshtml` already passes `row.id` as a path segment string.

---

## Summary: What Phase 8 Needs to Build

| Artifact | Type | Status |
|---|---|---|
| `RolePermissionsController` | New file | ❌ To create |
| `ViewModels/RolePermissions/RolePermissionsViewModel.cs` | New file | ❌ To create |
| `Views/RolePermissions/Index.cshtml` | New file | ❌ To create |
| `IPermissionService.GetRolePermissionsAsync` | Method | ✅ Already exists |
| `IPermissionService.SaveRolePermissionsAsync` | Method | ✅ Already exists |
| `IPermissionProvider.GetAll()` | Method | ✅ Already exists |
| `IRoleService.GetByIdAsync` | Method | ✅ Already exists |
| `HasPermissionAttribute` | Filter | ✅ Already exists |
| DI Registrations | Infrastructure | ✅ Already registered |
| "Manage Permissions" link in Roles/Index | Navigation | ✅ Already implemented |
