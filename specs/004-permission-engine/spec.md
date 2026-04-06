# Feature Specification: Permission Engine

**Feature Branch**: `004-permission-engine`  
**Created**: 2026-04-06  
**Status**: Draft  
**Input**: User description: "PHASE 4 - Permission Engine"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Enforcing Permission-Based Access Control (Priority: P1)

A Super Admin has restricted a role so it can only browse Employees, but not create them. A user assigned that role navigates to the Create Employee page. The system must block the request and display a clear "Access Denied" page — not a generic error or a redirect to login.

**Why this priority**: This is the core safety guarantee of the entire permission system. All other stories depend on enforcement being correct and reliable.

**Independent Test**: Can be fully tested by decorating a controller action with the permission requirement, assigning a role that lacks that permission to a test user, logging in as that user, and attempting to reach the restricted action. The outcome delivers a verifiable 403 response.

**Acceptance Scenarios**:

1. **Given** a user whose role does not have the "Create" function for "Employee", **When** they navigate to the Create Employee page, **Then** they see a styled "Access Denied" page (not a blank error or redirect to login).
2. **Given** a user whose role has the "Create" function for "Employee", **When** they navigate to the Create Employee page, **Then** they can access it normally.
3. **Given** a user with no role assigned, **When** they attempt to access any permission-protected page, **Then** they are denied access and shown the Access Denied page.

---

### User Story 2 - Loading Permissions from Configuration (Priority: P1)

A developer adds a new business object (e.g., "Invoice") with its allowed functions to a configuration file. Without changing any compiled code, the system automatically recognises the new object and its functions at the next application start-up, and they become available for role assignment.

**Why this priority**: This is the foundational contract of the permission engine — permissions must never be hardcoded. Without this, all downstream role management and UI permission-awareness breaks.

**Independent Test**: Can be fully tested by adding a new object entry to the permissions configuration file, restarting the application, and verifying the new object and its functions are available when querying the loaded permission definitions.

**Acceptance Scenarios**:

1. **Given** a permissions configuration file listing objects and their functions, **When** the application starts, **Then** all defined objects and their functions are loaded and available in memory.
2. **Given** a new object entry added to the configuration file, **When** the application restarts, **Then** the new object and its functions appear alongside the existing ones — with no code changes required.
3. **Given** a malformed or missing configuration file, **When** the application starts, **Then** the system fails with a clear diagnostic error rather than silently loading zero permissions.

---

### User Story 3 - Checking Permissions Programmatically in Views (Priority: P2)

A developer building a management page wants to show an "Export" button only to users whose role grants the "Export" function for the "Employee" object. They need a way to check the current user's permissions directly in the page template without duplicating logic.

**Why this priority**: Permission-aware rendering prevents confusing UX where buttons appear but fail when clicked. This story enables all future management pages to hide actions the user cannot perform.

**Independent Test**: Can be fully tested by rendering a view that conditionally shows a button based on a permission check helper, logging in as users with and without the required permission, and confirming the button appears/disappears accordingly.

**Acceptance Scenarios**:

1. **Given** a view that checks whether the current user has "Employee → Export" permission, **When** a user with that permission views the page, **Then** the Export button is visible.
2. **Given** the same view, **When** a user without that permission views the page, **Then** the Export button is not rendered.
3. **Given** a permission check for a function that does not exist in the configuration, **When** evaluated, **Then** it returns false (deny by default).

---

### User Story 4 - Assigning and Persisting Permissions to Roles (Priority: P2)

A Super Admin wants to define what a particular role can do. They save a set of object-function combinations for a role. On the next request by a user holding that role, the newly assigned permissions take effect immediately.

**Why this priority**: Without persisting role permissions, the enforcement mechanism has no data to enforce. This story connects configuration-loaded definitions to runtime enforcement.

**Independent Test**: Can be fully tested by saving a set of permissions for a role, then making a request as a user with that role to a page requiring one of the newly granted permissions, and confirming access is granted.

**Acceptance Scenarios**:

1. **Given** a role with no permissions, **When** a batch of object-function pairs is saved for that role, **Then** those permissions are persisted and associated with the role.
2. **Given** permissions previously saved for a role, **When** those permissions are replaced with a new batch, **Then** only the new batch applies — no orphaned entries remain.
3. **Given** a user holding a role, **When** that role's permissions are updated, **Then** the user's access rights reflect the update on their very next request (no session restart required).

---

### Edge Cases

- What happens when a user holds multiple roles and only one of them grants a required permission? The user must be granted access (permissions from all roles are combined with OR logic).
- What happens when a permission check is performed for an object or function not defined in the configuration file? The system must deny access and not throw an unhandled exception.
- What happens when the permissions configuration file contains duplicate object or function names? The system must load without error, ignoring or merging duplicates predictably.
- What happens when a role is deleted while a user is actively logged in with that role? The user's next permission check must reflect that the role — and its permissions — no longer apply.
- What happens when permission enforcement is applied to an action that requires authentication but the user is unauthenticated? The user is redirected to the login page rather than shown the 403 page.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST load all permission objects and their allowed functions from an external configuration file at application start-up.
- **FR-002**: The system MUST NOT require a code change or recompilation to add, remove, or rename a permission object or function — only a configuration file update is needed.
- **FR-003**: Developers MUST be able to protect any page or action by declaring a required object-function permission pair directly on that action.
- **FR-004**: The system MUST evaluate whether the currently authenticated user's role(s) grant the required permission before allowing access to a protected action.
- **FR-005**: The system MUST display a styled, user-friendly "Access Denied" page (HTTP 403) when a logged-in user lacks the required permission.
- **FR-006**: The system MUST redirect an unauthenticated user to the login page rather than showing a 403 when they attempt to access a permission-protected resource.
- **FR-007**: The system MUST provide a way for page templates (views) to query whether the current user holds a specific object-function permission, enabling conditional rendering of UI elements.
- **FR-008**: The system MUST persist role-permission assignments (which object-function pairs a role holds) in the database so assignments survive application restarts.
- **FR-009**: The system MUST support replacing all permissions for a role in a single operation, leaving no stale entries after the update.
- **FR-010**: When a user holds more than one role, the system MUST grant access if at least one of those roles includes the required permission (OR semantics).
- **FR-011**: The system MUST deny access by default for any permission object or function not found in the loaded configuration, rather than allowing it.
- **FR-012**: All permission services MUST be registered so they are available via dependency injection across the application without direct coupling to the configuration-reading implementation.

### Key Entities

- **Permission Object**: A named business concern (e.g., "Employee", "Role", "Report") for which access control is defined. Loaded from configuration; not stored in the database.
- **Permission Function**: A named action within a Permission Object (e.g., "Browse", "Create", "Delete", "Export"). Loaded from configuration alongside its parent object.
- **Role Permission**: A persisted assignment linking a specific Role to a specific object-function pair. Stored in the database. Multiple entries form the complete permission set of a role.
- **Role**: An existing identity construct (from Phase 1) that groups users. Extended here by its set of Role Permission entries.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Applying a permission requirement to an action successfully blocks users whose role lacks that permission 100% of the time — no bypass is possible through direct URL navigation.
- **SC-002**: A user with the required permission can access a protected action without any additional steps beyond normal navigation.
- **SC-003**: Adding a new permission object and its functions to the configuration file and restarting the application makes those definitions available in under 5 seconds — no code changes required.
- **SC-004**: Saving a new set of permissions for a role takes effect on the very next request made by a user holding that role — no session restart or cache flush required by the administrator.
- **SC-005**: Page templates can check any object-function permission for the current user with a single call, and the correct show/hide behaviour is verifiable by logging in as two users with different role permissions.
- **SC-006**: A user holding multiple roles is granted access if any one of their roles includes the required permission — verified by assigning complementary permission sets to two roles and assigning both to a single test user.
- **SC-007**: The "Access Denied" page is displayed with the application's admin layout and branding — not a raw HTTP error response.

---

## Assumptions

- Phase 1 (Foundation & Scaffold) is complete: the database, Identity configuration, ApplicationUser, ApplicationRole, and the project layer structure are in place.
- Phase 2 and Phase 3 are complete: authenticated sessions and the admin layout (used to render the 403 page) are available.
- A single permissions configuration file covers all objects and functions for the entire application. Per-tenant or per-environment permission definitions are out of scope.
- Permission evaluation is role-based only — there are no user-level permission overrides. A user's effective permissions are the union of all permissions from all their assigned roles.
- The SuperAdmin role, seeded in Phase 1, is implicitly granted all permissions and bypasses the permission check. This behaviour is assumed but the mechanism for the bypass (e.g., a special role name check) will be decided at implementation time.
- Session-cached permission state is not used; every permission check queries the persisted role-permission assignments to ensure changes take effect immediately.
- The permission configuration file is read-only at runtime. No UI for adding new permission objects or functions is in scope for this phase — that is managed by editing the configuration file directly.
- Browser-side permission checks (e.g., hiding a button via JavaScript) are a usability aid only and are never treated as a security boundary. Server-side enforcement is the sole authoritative check.
