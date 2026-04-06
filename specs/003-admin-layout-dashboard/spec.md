# Feature Specification: Admin Layout & Dashboard

**Feature Branch**: `003-admin-layout-dashboard`  
**Created**: 2026-03-31  
**Status**: Draft  
**Input**: User description: "PHASE 3 — Admin Layout & Dashboard"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Navigate the Admin Shell (Priority: P1)

An authenticated admin user opens the application and is presented with a consistent admin shell on every page: a branded sidebar on the left for navigation and a top navigation bar at the top showing the app name, a notification bell, and a user avatar dropdown. The user can click any sidebar item to navigate to a section, see which section is currently active, and at any time access account options (profile, logout) from the top bar.

**Why this priority**: The admin shell is the container that all other features live inside. Without it, no admin page can function. It must be delivered first to unblock every subsequent phase.

**Independent Test**: Can be tested by logging in, verifying the sidebar and topnav render, clicking navigation links, and confirming the active state highlights the correct item.

**Acceptance Scenarios**:

1. **Given** a logged-in admin user, **When** they load any admin page, **Then** the sidebar and top navigation bar are visible with the app branding and the user's name or avatar displayed.
2. **Given** a sidebar with navigation links, **When** the user clicks a link, **Then** the corresponding page loads and that link is visually marked as active.
3. **Given** the top navigation bar, **When** the user clicks the avatar dropdown, **Then** options for profile and logout appear.

---

### User Story 2 - Collapse Sidebar on Mobile (Priority: P2)

An admin user accessing the application on a mobile device or a narrow browser window sees the sidebar hidden by default to maximise content area. A hamburger icon in the top bar allows them to open and close the sidebar on demand.

**Why this priority**: Responsive behaviour is required for the template to be usable across devices. Without it, the layout breaks on mobile, making all admin pages unusable on small screens.

**Independent Test**: Can be tested independently by resizing the browser to a mobile viewport, verifying the sidebar is hidden, tapping the hamburger icon, and confirming the sidebar slides in and out.

**Acceptance Scenarios**:

1. **Given** a mobile-sized viewport, **When** an admin page loads, **Then** the sidebar is collapsed and not visible by default.
2. **Given** a collapsed sidebar on mobile, **When** the user taps the hamburger toggle, **Then** the sidebar overlays the content and becomes navigable.
3. **Given** an open sidebar overlay on mobile, **When** the user taps outside the sidebar or taps the toggle again, **Then** the sidebar closes.
4. **Given** a desktop-sized viewport, **When** the page loads, **Then** the sidebar is visible by default alongside main content.

---

### User Story 3 - View the Dashboard Home (Priority: P3)

After logging in, an admin user lands on a dashboard home page that provides an at-a-glance summary of key system metrics: total registered users, total configured roles, and active sessions. The page uses the admin shell layout.

**Why this priority**: The dashboard home is the landing page after login but its content (stat counts) depends on the admin shell being in place. It is the first content page rendered inside the layout.

**Independent Test**: Can be tested by logging in and verifying the dashboard page displays three stat cards with labels and numeric values pulled from the system.

**Acceptance Scenarios**:

1. **Given** a logged-in admin user, **When** they access the dashboard home, **Then** they see stat cards showing the total number of users, total number of roles, and a count of active sessions.
2. **Given** the dashboard home, **When** the page renders, **Then** stats reflect current data from the system (not hardcoded values).
3. **Given** the dashboard home, **When** viewed on a mobile device, **Then** the stat cards stack vertically and remain legible.

---

### User Story 4 - Log Out Safely (Priority: P4)

An admin user who has finished their session clicks logout from the top navigation avatar dropdown. The system ends their session and redirects them to the login page.

**Why this priority**: Logout is a security-critical action; however it is a simpler deliverable than the shell itself and depends on the topnav being built.

**Independent Test**: Can be tested by logging in, clicking logout from the topnav dropdown, and confirming the session is terminated and the login page is shown.

**Acceptance Scenarios**:

1. **Given** a logged-in admin user, **When** they click "Logout" in the topnav dropdown, **Then** their session is terminated and they are redirected to the login page.
2. **Given** a logged-out state, **When** the user attempts to navigate to any admin page directly, **Then** they are redirected to the login page.

---

### Edge Cases

- What happens when the notification bell is clicked but there are no notifications? — The bell opens without a badge or shows an empty state message.
- What happens when the user's display name is very long? — The topnav avatar area truncates the name gracefully without breaking the layout.
- What happens when the sidebar has many navigation items that exceed the viewport height? — The sidebar becomes independently scrollable without affecting the main content.
- What happens if the active session count cannot be determined? — The dashboard stat card shows a dash or "N/A" rather than erroring.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST present a persistent sidebar navigation panel and a top navigation bar on every authenticated admin page.
- **FR-002**: The sidebar MUST display a logo or app name, a list of navigation links with icons, and visually highlight the currently active section.
- **FR-003**: The top navigation bar MUST display the application name, a notification bell icon with a badge when notifications are present, and a user avatar dropdown menu.
- **FR-004**: The user avatar dropdown MUST contain at minimum a "Profile" link and a "Logout" action.
- **FR-005**: The sidebar MUST be collapsible, triggered by a hamburger toggle button in the top navigation bar.
- **FR-006**: On mobile viewports, the sidebar MUST be hidden by default and toggled open/closed by the hamburger button.
- **FR-007**: The dashboard home page MUST display stat cards showing: total registered users count, total roles count, and active sessions count.
- **FR-008**: All stat values on the dashboard MUST be populated dynamically from live system data.
- **FR-009**: The admin shell layout MUST be consistently applied to all authenticated pages via a single shared layout file — individual pages MUST NOT define their own shell structure.
- **FR-010**: The primary brand colour (`#004D82`) MUST be defined as a CSS custom property and applied consistently across the sidebar, primary buttons, and active states.
- **FR-011**: The logout action MUST terminate the user session and redirect to the login page.
- **FR-012**: The layout MUST be fully responsive, adapting gracefully to desktop, tablet, and mobile screen sizes.

### Key Entities

- **Navigation Item**: A sidebar link entry with a label, an icon, a target route, and an active/inactive state.
- **Stat Card**: A dashboard widget displaying a label (e.g., "Total Users") and a live numeric value sourced from system data.
- **Notification**: A message or alert surfaced in the topnav bell; has a read/unread state represented by a badge count (scoped to future phases; layout must accommodate it).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All authenticated admin pages render within the shared admin shell without any page defining its own independent navigation structure.
- **SC-002**: On a mobile viewport (≤768px width), the sidebar is hidden on page load and can be toggled open and closed within 1 tap interaction.
- **SC-003**: The dashboard home page displays all three stat cards (Users, Roles, Active Sessions) with accurate live counts on every page load.
- **SC-004**: A session is fully terminated within 1 second of the user clicking "Logout", with the user redirected to the login page.
- **SC-005**: The admin shell layout passes a visual review on at least three viewport sizes: desktop (≥1200px), tablet (768px–1199px), and mobile (≤767px) with no broken or overlapping UI elements.
- **SC-006**: The primary brand colour (`#004D82`) is applied consistently via a single CSS variable reference across sidebar background, active nav states, and primary action elements — no hardcoded colour values appear in any layout stylesheet.

## Assumptions

- The authentication system (Phase 1 & 2) is already in place; this phase only consumes the existing user session — it does not implement login/logout logic from scratch.
- The "Active Sessions" stat on the dashboard will be approximated using the count of users with recent activity or all authenticated sessions tracked by Identity; an exact real-time session count mechanism is not in scope.
- Navigation items in the sidebar are defined at layout design time for the core modules (Dashboard, Users, Roles, Role Permissions); dynamic permission-based hiding of sidebar items is deferred to Phase 9 (Polish & Hardening).
- The notification bell in the topnav is a UI placeholder in this phase — no notification data model or delivery mechanism is implemented yet; it renders with a static or zero badge.
- Profile page content is out of scope for this phase; the "Profile" link in the avatar dropdown may be a stub or route placeholder.
- The app name displayed in the sidebar and topnav is configured via application settings and does not require a UI for editing in this phase.
