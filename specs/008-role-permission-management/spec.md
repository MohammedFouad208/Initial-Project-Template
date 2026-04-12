# Feature Specification: Role-Permission Management

**Feature Branch**: `008-role-permission-management`  
**Created**: April 12, 2026  
**Status**: Draft  
**Input**: User description: "PHASE 8 — Role-Permission Management"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Assign Permissions to a Role via Matrix UI (Priority: P1)

A Super Admin navigates to the Role-Permission Management page for a specific role. They see a matrix grid where rows represent permission objects (e.g., Employee, User, Role, Report) and columns represent functions (e.g., Browse, Create, Update, Delete, Export). The admin checks or unchecks individual permissions, then saves. The saved configuration takes effect immediately on the next request made by any user holding that role.

**Why this priority**: This is the core deliverable of Phase 8. Without this, roles have no way to control access granularly. All other stories depend on this working reliably.

**Independent Test**: Can be fully tested by opening the matrix for a role, toggling several checkboxes, saving, then re-opening the matrix to confirm the checked state was persisted correctly.

**Acceptance Scenarios**:

1. **Given** a Super Admin is on the Role-Permission matrix page for the "Admin" role, **When** they check "Employee → Create" and "Employee → Update" and click Save, **Then** the system stores those permissions for the Admin role and shows a success alert.
2. **Given** a Super Admin has saved permissions for a role, **When** they navigate away and return to the same role's matrix page, **Then** all previously saved checkboxes are pre-checked and unsaved ones remain unchecked.
3. **Given** permission objects and functions are defined in `permissions.json`, **When** the matrix page loads, **Then** the grid reflects the exact objects and functions from that file — no hardcoded rows or columns.
4. **Given** a role currently has 3 permissions, **When** the admin unchecks all and saves, **Then** all permissions for that role are removed and the role now has 0 permissions.

---

### User Story 2 - Navigate to Role-Permission Matrix from Roles List (Priority: P2)

A Super Admin views the Roles list page and sees a "Manage Permissions" action button next to each role. Clicking it navigates them directly to the Role-Permission matrix for that role, with the page header showing the role name.

**Why this priority**: Without navigation from the Roles list, the matrix page is unreachable through normal UI flow. This story completes the end-to-end workflow from Phase 7 to Phase 8.

**Independent Test**: Can be fully tested by clicking "Manage Permissions" on any role in the Roles list and confirming the matrix page opens with that role's name in the heading.

**Acceptance Scenarios**:

1. **Given** the Super Admin is on the Roles index page, **When** they click the "Manage Permissions" button for a role, **Then** they are taken to the Role-Permission matrix page scoped to that specific role.
2. **Given** the matrix page is open, **When** the page renders, **Then** the page header displays the role's name (e.g., "Manage Permissions: Admin Role").

---

### User Story 3 - Bulk Check/Uncheck Permissions per Row and Column (Priority: P3)

When viewing the permission matrix, a Super Admin can check all functions for a given object in a single click ("Check All" per row), uncheck all for a row, or toggle all checkboxes in a given function column. This speeds up common admin workflows like granting full access to one module or revoking all Create permissions across objects.

**Why this priority**: P3 because the feature is fully functional without bulk-toggle helpers — they are a usability enhancement for matrices with many rows and functions.

**Independent Test**: Can be fully tested by clicking "Check All" for the "Employee" row and confirming all function checkboxes for that row become checked without affecting other rows.

**Acceptance Scenarios**:

1. **Given** the matrix is loaded, **When** the admin clicks "Check All" for the Employee row, **Then** all function checkboxes in that row are checked.
2. **Given** all functions in a row are checked, **When** the admin clicks "Uncheck All" for that row, **Then** all checkboxes in that row are unchecked.
3. **Given** the matrix is loaded, **When** the admin clicks the column header toggle for "Delete", **Then** the "Delete" checkbox is toggled for every object row simultaneously.

---

### Edge Cases

- What happens when a role currently has permissions for an object that was **removed** from `permissions.json`? The matrix renders only objects/functions from the JSON; orphaned DB records for removed objects are ignored in the UI but remain in storage until explicitly removed by a save.
- What happens if two admins open the matrix for the same role at the same time, each makes different changes, and both save? The second save wins — the system does a full replace of all permissions for that role on each save, so no partial merge occurs.
- What happens if a `roleId` is invalid or not found when opening the matrix? The controller returns a 404 Not Found response.
- What happens if the Save POST is submitted with an empty permissions list? All permissions for that role are removed — this is a valid "revoke all" action.
- What happens if `permissions.json` is missing or malformed at startup? The application should fail to start (or fall back gracefully), as the permission provider is registered at startup.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a permission matrix for a given role, where rows are permission objects and columns are permission functions, both sourced exclusively from `permissions.json`.
- **FR-002**: System MUST pre-check checkboxes that correspond to permissions already assigned to the selected role.
- **FR-003**: Users (Super Admin) MUST be able to check or uncheck any individual permission checkbox in the matrix.
- **FR-004**: System MUST provide a "Check All" and "Uncheck All" control per object row.
- **FR-005**: System MUST provide a column-level toggle control in the function column header.
- **FR-006**: System MUST save the full set of checked permissions via a batch POST that replaces all existing permissions for the role.
- **FR-007**: System MUST display a success alert after a successful save and a danger alert if the save fails.
- **FR-008**: The matrix page header MUST display the name of the role being edited.
- **FR-009**: The Roles list page MUST include a "Manage Permissions" action button per role that navigates to the matrix page for that role.
- **FR-010**: The Save button MUST enter a loading state while the POST request is in progress, preventing double-submission.
- **FR-011**: All permission-saving actions MUST be protected by anti-forgery token validation.
- **FR-012**: Access to the Role-Permission matrix page MUST be restricted to users with the appropriate permission (Role → AssignPermissions).

### Key Entities

- **RolePermission**: Represents a single granted permission for a role. Key attributes: `RoleId` (foreign key to a role), `ObjectName` (e.g., "Employee"), `FunctionName` (e.g., "Create"). A role's full permission set is the collection of all its `RolePermission` records.
- **PermissionObject**: A logical grouping of functions read from `permissions.json`. Has a `Name`, a `DisplayName`, and a list of `Functions`. Not persisted — only used for rendering the matrix columns and rows.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A Super Admin can open the permission matrix for any role and save a full permissions update in under 60 seconds.
- **SC-002**: After saving, permission changes take effect on the very next request made by a user holding that role — no cache invalidation step is required.
- **SC-003**: The matrix correctly reflects the current state of `permissions.json` at all times — adding or removing an object/function from the JSON is reflected on the matrix page without any code change.
- **SC-004**: Zero stale permission data is shown: reopening the matrix after saving always displays the exact state that was last saved.
- **SC-005**: The Save operation completes without error for a role with the maximum possible number of permissions (all objects × all functions checked).

## Assumptions

- The permission objects and functions that appear in the matrix are exclusively those defined in `/Config/permissions.json`. No UI is provided to add or remove objects/functions — that is done by editing the JSON file.
- Only Super Admins (or users with Role → AssignPermissions) can access the matrix page. Other roles are redirected to the 403 page.
- The Save action performs a full replacement of all permissions for the target role — it deletes all existing records for the role and inserts the newly checked set. Partial/differential updates are out of scope.
- The feature depends on Phase 7 (Role Management) being complete, as the "Manage Permissions" button is added to the Roles list page from that phase.
- The feature depends on Phase 4 (Permission Engine) being complete, as `IPermissionService`, `RolePermission` entity, and `HasPermissionAttribute` must already exist.
- RTL support is assumed by default — all layout and spacing in the matrix view must use CSS logical properties consistent with the established design system.
- The matrix is rendered server-side (Razor); checkbox state is loaded from the database on page load. No client-side-only state management is used.

- [Assumption about target users, e.g., "Users have stable internet connectivity"]
- [Assumption about scope boundaries, e.g., "Mobile support is out of scope for v1"]
- [Assumption about data/environment, e.g., "Existing authentication system will be reused"]
- [Dependency on existing system/service, e.g., "Requires access to the existing user profile API"]
