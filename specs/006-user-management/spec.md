# Feature Specification: PHASE 6 — User Management

**Feature Branch**: `006-user-management`  
**Created**: 2026-04-09  
**Status**: Draft  
**Input**: User description: "PHASE 6 - User Management"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse & Search Users (Priority: P1)

An administrator opens the Users section and sees a paginated, searchable list of all system users. Each row shows the user's avatar (initials circle), full name, email address, assigned roles as soft badges, and an Active/Inactive status badge. The list loads data server-side, so large user bases are handled without performance issues.

**Why this priority**: Visibility into existing users is the foundation of every other user management action. Without a working list, Create, Edit, and Deactivate have no entry point.

**Independent Test**: Navigate to `/Users`. The DataTable renders with server-side pagination, the search box filters results in real time, and each row shows name, email, role badges, and status. No CRUD operations needed to validate this story.

**Acceptance Scenarios**:

1. **Given** a logged-in Super Admin, **When** they navigate to `/Users`, **Then** a paginated DataTable loads showing all users with columns: avatar/name, email, roles, status, and actions.
2. **Given** the Users list is displayed, **When** the admin types in the search box, **Then** the table refreshes server-side and shows only matching users without a full page reload.
3. **Given** the Users list is displayed, **When** the admin clicks a column header, **Then** the table re-sorts server-side by that column.
4. **Given** a user with role "Admin" exists, **When** the list renders, **Then** that user's role column displays a soft-colored badge for each assigned role.
5. **Given** a user with `IsActive = false` exists, **When** the list renders, **Then** their status column shows a danger-colored soft badge labeled "Inactive".

---

### User Story 2 - Create a New User (Priority: P2)

An administrator fills out a form to create a new system user, specifying full name, email address, a temporary password, one or more role assignments, and an initial active/inactive state. On successful submission the user is persisted and the admin is returned to the Users list with a success indication.

**Why this priority**: Creating users is the primary write operation of the module. A working create flow delivers the first end-to-end value after the list view.

**Independent Test**: Navigate to `/Users/Create`, complete the form with valid data, submit, and confirm the new user appears in the list.

**Acceptance Scenarios**:

1. **Given** the Create User form, **When** the admin submits valid data (unique email, strong password, at least one role), **Then** the new user is saved and the admin is redirected to the Users list.
2. **Given** the Create User form, **When** the admin submits a duplicate email address, **Then** a validation error is shown and no user is created.
3. **Given** the Create User form, **When** the password does not meet complexity rules (min 8 chars, uppercase, digit, special character), **Then** inline validation errors are displayed per field.
4. **Given** the Create User form, **When** the admin selects multiple roles, **Then** all selected roles are saved against the new user.
5. **Given** a successful create, **When** the admin is redirected to the list, **Then** the new user appears in the table.

---

### User Story 3 - Edit an Existing User (Priority: P3)

An administrator opens the Edit page for an existing user to update their full name, email, role assignments, or active state. The password field is absent on the edit form. Changes are saved and the admin is returned to the Users list.

**Why this priority**: Edit follows naturally from Create and covers the ongoing lifecycle of user records. Password is intentionally excluded to avoid accidental resets.

**Independent Test**: From the Users list, click Edit on any user, change the full name, save, and confirm the updated name appears in the list.

**Acceptance Scenarios**:

1. **Given** an existing user, **When** the admin opens Edit and changes the full name, **Then** the updated name is saved and reflected in the list.
2. **Given** the Edit User form, **When** it loads, **Then** no password field is visible.
3. **Given** the Edit User form, **When** the admin changes role assignments, **Then** the updated role set replaces the previous one upon saving.
4. **Given** the Edit User form, **When** the admin submits a duplicate email belonging to another user, **Then** a validation error is shown and no changes are saved.
5. **Given** a successful edit, **When** the admin is redirected to the list, **Then** the row reflects the updated data.

---

### User Story 4 - Activate / Deactivate a User (Priority: P4)

An administrator can toggle a user's active state directly from the Users list without navigating away. The status badge updates to reflect the new state. Deactivated users remain in the system (soft delete) but cannot log in.

**Why this priority**: Soft deactivation is a safety mechanism. It is lower priority than CRUD but critical before the module is considered complete.

**Independent Test**: From the Users list, click the status badge/toggle for an active user, confirm the badge switches to "Inactive", and verify the user cannot log into the application.

**Acceptance Scenarios**:

1. **Given** an active user row, **When** the admin clicks the Deactivate action, **Then** the user's `IsActive` flag is set to false and the status badge changes to the danger-colored "Inactive" badge without a full page reload.
2. **Given** an inactive user row, **When** the admin clicks the Activate action, **Then** `IsActive` is set to true and the status badge changes to the success-colored "Active" badge.
3. **Given** a deactivated user, **When** they attempt to log in, **Then** they are rejected with an appropriate message and cannot access the admin area.
4. **Given** a deactivated user, **When** viewed in the Users list, **Then** their record is still visible with all their data intact.

---

### User Story 5 - Permission-Gated Access (Priority: P5)

The Users module enforces role-based permissions at every entry point. A user without the "User → Browse" permission cannot access the list. Without "Create", the Create button is hidden and direct URL access returns a 403 page. Same rules apply for Update and Delete.

**Why this priority**: Security enforcement is a cross-cutting concern. It completes the module and ensures it integrates correctly with the Permission Engine (Phase 4).

**Independent Test**: Log in as a role with only "User → Browse" permission. Confirm the Create button is not visible. Navigate directly to `/Users/Create` and confirm a 403 page is returned.

**Acceptance Scenarios**:

1. **Given** a user with "User → Browse" permission, **When** they visit `/Users`, **Then** the list is accessible but the Create button is not rendered.
2. **Given** a user without "User → Browse" permission, **When** they navigate to `/Users`, **Then** they are shown the custom 403 page.
3. **Given** a user with "User → Create" permission, **When** they view the Users list, **Then** the Create button is visible and functional.
4. **Given** a user without "User → Update" permission, **When** each table row renders, **Then** the Edit button is hidden for that user.
5. **Given** a user without "User → Delete", **When** they navigate directly to a delete endpoint, **Then** a 403 response is returned.

---

### Edge Cases

- What happens when an admin tries to deactivate their own account?
- What happens when the last Super Admin account is deactivated?
- How does the system handle submitting the Create form with a role that no longer exists?
- What happens when a user has no roles assigned — does the list row show an empty badge area or a placeholder?
- What happens when a user's email is changed to one already belonging to another user?
- How does the DataTable behave when there are zero users in the system (empty state)?
- What happens when the server-side DataTable endpoint returns an error — does the table show a retry option?

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Administrators with "User → Browse" permission MUST be able to view a paginated list of all application users, showing full name, email, assigned roles, and active/inactive status per row.
- **FR-002**: The users list MUST support server-side search, column-based sorting, and page-size selection without a full page reload.
- **FR-003**: Administrators with "User → Create" permission MUST be able to create a new user by providing full name, email, password, role assignments, and initial active state.
- **FR-004**: The system MUST enforce password complexity rules (minimum 8 characters, at least one uppercase letter, one digit, one special character) on user creation.
- **FR-005**: The system MUST reject user creation or update if the provided email address already belongs to another user account.
- **FR-006**: Administrators with "User → Update" permission MUST be able to edit a user's full name, email, role assignments, and active state. The edit form MUST NOT expose a password reset field.
- **FR-007**: Administrators with "User → Update" permission MUST be able to activate or deactivate a user without leaving the list view. Deactivation MUST be a soft delete — the record is retained.
- **FR-008**: A deactivated user MUST be prevented from logging into the application.
- **FR-009**: The system MUST allow assigning multiple roles to a single user via a multi-select control on the Create and Edit forms.
- **FR-010**: All Create, Edit, and Delete/Deactivate actions MUST be protected by CSRF tokens.
- **FR-011**: The Users list MUST hide the Create, Edit, and Delete/Deactivate action buttons for the current session user if they lack the corresponding permission, using the generic DataTable permission-aware rendering.
- **FR-012**: Accessing any restricted Users action without the required permission MUST return the custom 403 page from the design system.
- **FR-013**: Form validation errors MUST be displayed inline per field and in an error summary block with left-border danger styling, without losing the user's input.
- **FR-014**: The users list MUST display a user avatar composed of the user's initials in a circular badge, consistent with the topnav avatar pattern from the design system.
- **FR-015**: Status indicators in the users list MUST use soft-color pill badges: success-colored for "Active", danger-colored for "Inactive".

### Key Entities

- **ApplicationUser**: Represents a system user. Key attributes: full name, email address (identity credential), active/inactive state, account creation date, and the set of roles currently assigned to them.
- **ApplicationRole**: Represents a named role (e.g., Admin, Viewer). Users are assigned one or more roles, which determines their permission set.
- **RoleAssignment**: The many-to-many relationship between a user and the roles assigned to them. Modifying role assignments on Create/Edit replaces the full set for that user.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Administrators can complete the full Create User flow (form open → fill → submit → confirmation) in under 2 minutes.
- **SC-002**: The Users list returns the first page of results in under 1 second for datasets up to 10,000 users when server-side pagination is active.
- **SC-003**: Search results update within 500 milliseconds of the user stopping typing, with no perceptible lag on datasets up to 10,000 users.
- **SC-004**: 100% of Create, Edit, and Deactivate entry points are inaccessible (hidden from UI and blocked at server level) for users lacking the corresponding permission.
- **SC-005**: All form validation errors are surfaced to the user without losing previously entered data, achieving a first-attempt form completion rate of at least 90% for admins familiar with the rules.
- **SC-006**: The active/inactive toggle on the list updates the visible badge state without requiring a full page reload, completing the round-trip in under 1 second.
- **SC-007**: The Users module renders correctly and remains fully usable when the display direction is toggled between LTR and RTL without a page reload.

---

## Assumptions

- Phases 1–5 (Foundation, Auth, Admin Layout, Permission Engine, Generic DataTable) are fully implemented and operational before this phase begins.
- The `IPermissionService`, `HasPermissionAttribute`, and `AppDataTable` JS are available and functioning as specified in their respective phases.
- User passwords are managed by ASP.NET Core Identity. There is no requirement to display or migrate existing plaintext passwords.
- The Create form sets an initial password; future password changes are handled via the Forgot Password flow from Phase 2, not through this management module.
- An admin cannot delete a user record entirely from this UI — deactivation (soft delete) is the only removal action. Hard delete is out of scope.
- A user may be assigned zero roles (no roles), which is a valid state representing a user with no specific access beyond authentication.
- The "Manage Users" list is scoped to all non-SuperAdmin users by default; SuperAdmin management is considered an advanced scenario and is out of scope for this phase.
- Role options shown in the Create/Edit multi-select are loaded from the live roles list in the database, not hardcoded.
- Email addresses are treated as unique identifiers and are case-insensitive for uniqueness checks.
- An admin editing their own account follows the same rules as editing any other account (no special self-edit restrictions beyond what Identity enforces).
