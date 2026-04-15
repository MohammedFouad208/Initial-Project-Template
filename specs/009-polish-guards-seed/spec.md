# Feature Specification: Polish, Guards & Seed Data

**Feature Branch**: `009-polish-guards-seed`  
**Created**: 2026-04-12  
**Status**: Draft  
**Input**: User description: "PHASE 9 — Polish, Guards & Seed Data"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developer Clones and Runs the Template from Scratch (Priority: P1)

A new developer clones the repository, configures the connection string, runs migrations, seeds the database, and launches the app. They log in with the seeded SuperAdmin account and immediately have full access to all management features.

**Why this priority**: This is the primary value proposition of the template — a zero-friction onboarding experience. All other stories depend on the template being runnable from a clean state.

**Independent Test**: Can be fully tested by following the README from a fresh clone on a new machine and verifying the app starts, seeds succeed, and SuperAdmin login works.

**Acceptance Scenarios**:

1. **Given** a freshly cloned repository, **When** the developer follows the README setup steps, **Then** the app starts without errors and the seed data is present in the database.
2. **Given** the seeded database, **When** the developer logs in with the SuperAdmin credentials, **Then** they can access all management pages and perform all CRUD operations.
3. **Given** the README instructions, **When** a developer with no prior project knowledge follows them, **Then** the app is running within 10 minutes.

---

### User Story 2 - Unauthorized User Sees a Styled Error Page (Priority: P2)

A logged-in user with limited permissions attempts to access a resource they are not authorized for. Instead of a raw exception or blank page, they see a branded 403 error page matching the admin design system, with a clear message and a navigation button.

**Why this priority**: Security and user experience are non-negotiable for a production-ready template. An unhandled 403 breaks trust and exposes raw error information.

**Independent Test**: Can be fully tested by logging in as a Viewer-role user and navigating directly to a URL protected by a permission the Viewer does not have, then verifying the 403 page renders correctly.

**Acceptance Scenarios**:

1. **Given** a logged-in user without "User → Create" permission, **When** they navigate directly to the Create User URL, **Then** they see the styled 403 page with a lock icon and a back button.
2. **Given** a user navigates to a non-existent route, **When** the app cannot match the URL, **Then** they see a styled 404 page with a home button.
3. **Given** the 403 or 404 error page, **When** the user clicks the back/home button, **Then** they are navigated to the dashboard without errors.

---

### User Story 3 - Sidebar Reflects User's Actual Permissions (Priority: P2)

A logged-in Admin-role user sees only the navigation links in the sidebar that correspond to the permissions assigned to their role. Links to inaccessible areas are hidden visually but the sidebar DOM structure remains intact.

**Why this priority**: Showing navigation items the user cannot access creates confusion and potential security concerns. Hiding via CSS class (rather than removing from DOM) keeps the sidebar layout stable.

**Independent Test**: Can be fully tested by logging in as an Admin user with a specific subset of permissions and verifying that only the corresponding sidebar links are visible.

**Acceptance Scenarios**:

1. **Given** a user whose role only has "User → Browse" and "Role → Browse" permissions, **When** they view the sidebar, **Then** only the Users and Roles nav links are visible; all others have the `d-none` class applied.
2. **Given** the sidebar with hidden links, **When** the page renders, **Then** the sidebar layout (width, spacing, brand area) is identical to a fully visible sidebar.
3. **Given** a SuperAdmin user, **When** they view the sidebar, **Then** all navigation links are visible.

---

### User Story 4 - Forms Provide Real-Time Validation Feedback (Priority: P3)

A user filling out any form in the admin panel (Create User, Create Role, etc.) receives real-time visual feedback. Fields that pass validation show a green border; fields that fail show a red border with an inline message. The submit button enters a loading state during submission.

**Why this priority**: Real-time validation reduces form submission errors and improves perceived quality. The loading state prevents double submissions.

**Independent Test**: Can be fully tested on the Create User form by submitting with an empty required field and verifying the invalid state appears, then correcting the field and verifying the valid state appears.

**Acceptance Scenarios**:

1. **Given** a Create User form, **When** the user submits with an empty required field, **Then** that field shows an invalid visual state and an inline error message.
2. **Given** a field in an invalid state, **When** the user enters a valid value, **Then** the field transitions to a valid visual state.
3. **Given** a valid form, **When** the user clicks submit, **Then** the submit button enters a loading/spinner state and cannot be clicked again until the response returns.

---

### User Story 5 - Actions Show Toast Feedback Notifications (Priority: P3)

After completing an action (save permissions, create user, delete role), the user sees a brief toast notification confirming success or reporting an error. The toast uses the design system color tokens and disappears automatically.

**Why this priority**: Toast feedback closes the action loop for the user, reducing uncertainty about whether an operation succeeded.

**Independent Test**: Can be fully tested by saving role permissions and verifying a success toast appears at the bottom-right with the correct styling, then auto-dismisses.

**Acceptance Scenarios**:

1. **Given** a user saves role permissions successfully, **When** the page reloads, **Then** a success toast appears at the bottom-right with a green accent border and auto-dismisses after a few seconds.
2. **Given** an operation that fails (e.g., trying to delete a role with assigned users), **When** the response returns, **Then** a danger-styled toast appears with an appropriate error message.
3. **Given** a toast notification, **When** the user does not interact with it, **Then** it auto-dismisses without user action.

---

### Edge Cases

- What happens when the seed data has already been applied and the app starts again? — The seeder must check for existing data and skip re-seeding to avoid duplicates.
- What happens if the SuperAdmin role is accidentally deleted? — The seeder creates it idempotently; the delete guard on roles prevents deletion if users are assigned.
- What happens when a user's role permissions change while they have an active session? — Permission checks occur per request; the next page load reflects the updated permissions.
- What happens when a POST request is submitted without a valid CSRF token? — The request is rejected with a 400 response; no data is modified.
- What happens if `permissions.json` contains an object with no functions defined? — The sidebar hides that object's nav link; the permission matrix shows an empty row rather than throwing an error.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The sidebar MUST apply `d-none` class to navigation links for permission objects the current user's role cannot access — the links MUST remain in the DOM.
- **FR-002**: The system MUST display a styled 403 error page when a user accesses a route they are not authorized for, within the admin layout.
- **FR-003**: The system MUST display a styled 404 error page when a user navigates to a non-existent route, within the admin layout.
- **FR-004**: All POST actions MUST be protected with CSRF token validation; requests without a valid token MUST be rejected.
- **FR-005**: The database seeder MUST create three demo roles (SuperAdmin, Admin, Viewer) with predefined permission sets on first run.
- **FR-006**: The database seeder MUST be idempotent — running it multiple times MUST NOT create duplicate roles, users, or permissions.
- **FR-007**: The SuperAdmin seed role MUST be assigned all permissions defined in `permissions.json`.
- **FR-008**: The Admin seed role MUST be assigned Browse and Create permissions for all objects.
- **FR-009**: The Viewer seed role MUST be assigned only Browse permissions for all objects.
- **FR-010**: `appsettings.json` MUST contain clearly labeled placeholder sections for: database connection string, SMTP configuration, and Identity lockout settings.
- **FR-011**: The `README.md` MUST document all steps from clone to running app: connection string setup, migration command, seed command, and app launch.
- **FR-012**: All forms MUST display real-time field validation using `.is-valid` / `.is-invalid` CSS classes on input focus-out.
- **FR-013**: Submit buttons on all forms MUST enter a loading state (spinner, disabled) upon form submission.
- **FR-014**: The admin layout MUST include a toast notification container; the `AppToast.show(message, type)` function MUST be available globally on all admin pages.
- **FR-015**: Toast notifications MUST automatically dismiss after a defined interval without user interaction.
- **FR-016**: Every view MUST use only design-token CSS variables for colors; no hardcoded color values are permitted.
- **FR-017**: All shared CSS MUST use logical properties (`margin-inline-start`, `padding-inline-end`) instead of direction-specific properties.

### Key Entities

- **Seed Role**: A pre-defined role (SuperAdmin, Admin, Viewer) with a specific set of permission assignments created during database initialization.
- **Seed User**: The SuperAdmin account created during database initialization with known credentials for first-time access.
- **Toast Notification**: A transient in-app message shown after user actions, carrying a message text and a type (success, danger, warning, info).

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer following the README can have the app running from a fresh clone in under 10 minutes.
- **SC-002**: 100% of POST endpoints in the application are covered by CSRF token validation — zero unprotected POST actions remain.
- **SC-003**: All three seeded roles (SuperAdmin, Admin, Viewer) are present with their correct permission sets after running the seeder on an empty database.
- **SC-004**: Re-running the seeder on an already-seeded database produces zero duplicate records.
- **SC-005**: The sidebar renders correctly (no layout shift, no broken spacing) when one or more nav links have the `d-none` class applied.
- **SC-006**: 100% of views pass the design system audit: no hardcoded color values, no direction-specific margin/padding properties in shared CSS.
- **SC-007**: The 403 and 404 error pages render within the admin layout with correctly styled icons and navigation buttons on both LTR and RTL layouts.
- **SC-008**: Toast notifications appear and auto-dismiss on all admin pages after relevant user actions, using the correct design-token color for success and danger states.
- **SC-009**: Form validation feedback (`.is-valid` / `.is-invalid` states) is functional on all Create and Edit forms across User, Role, and Role-Permission management pages.

---

## Assumptions

- All previous phases (1–8) have been implemented and the application builds and runs without errors before this phase begins.
- The `permissions.json` file is the single source of truth for permission objects and functions; no permissions are hardcoded elsewhere in the codebase.
- The SMTP email sending in the seed/config is configured as a placeholder interface — a real SMTP provider is not required to be functional for this phase.
- The seeded SuperAdmin user credentials will be documented in the README and in `appsettings.Development.json` as development-only defaults; they MUST be changed before production deployment.
- "Design system audit" in the context of this phase means a manual or semi-automated review of Razor views and CSS files — not an automated visual regression test suite.
- Bootstrap 5 Toast component is already loaded via the layout's CDN or bundle; no additional front-end packages are required for the toast system.
- jQuery Validate and the Bootstrap 5 jQuery Validate adapter are already referenced in the application's bundle or layout; this phase wires them up consistently across all forms.
