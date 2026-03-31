# Feature Specification: Authentication Pages (Phase 2)

**Feature Branch**: `002-auth-pages`  
**Created**: 2026-03-31  
**Status**: Draft  
**Input**: User description: "PHASE 2 — Authentication Pages"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registered User Logs In (Priority: P1)

A registered user visits the application and provides their email and password to gain access to the admin dashboard. They may optionally choose "Remember Me" to stay logged in across browser sessions.

**Why this priority**: Login is the gateway to the entire application. Every other feature in the admin template depends on a working authentication flow. Without login, nothing else can be accessed or tested.

**Independent Test**: Can be fully tested by seeding a user in the database, navigating to the login page, entering valid credentials, and verifying redirection to the dashboard.

**Acceptance Scenarios**:

1. **Given** a registered user with valid credentials, **When** they submit the login form with correct email and password, **Then** they are redirected to the admin dashboard.
2. **Given** a registered user, **When** they submit the login form with an incorrect password, **Then** an error message is displayed and they remain on the login page.
3. **Given** a registered user, **When** they check "Remember Me" and close the browser, **Then** reopening the browser keeps them logged in without re-entering credentials.
4. **Given** a registered user who is logged in, **When** they click Logout, **Then** their session is cleared and they are redirected to the login page.

---

### User Story 2 - New User Registers an Account (Priority: P2)

A new user creates an account by providing their full name, email, and a password that meets the defined complexity rules.

**Why this priority**: Registration enables new administrators to be onboarded to the system. It is the entry point for new accounts and must be available alongside the login flow.

**Independent Test**: Can be fully tested by navigating to the registration page, filling in valid data, submitting, and verifying the account is created and the user is redirected.

**Acceptance Scenarios**:

1. **Given** a user on the registration page, **When** they submit valid full name, email, password, and matching confirmation, **Then** their account is created and they are logged in or redirected to login.
2. **Given** a user on the registration page, **When** they enter a password shorter than 8 characters or missing required complexity, **Then** a clear validation message tells them what is required.
3. **Given** a user on the registration page, **When** they enter a password and a non-matching confirmation password, **Then** a validation error is shown before submission.
4. **Given** a user on the registration page, **When** they submit an email already registered in the system, **Then** an error message indicates the email is already in use.

---

### User Story 3 - User Resets a Forgotten Password (Priority: P3)

A user who cannot remember their password requests a reset link via email, then uses that link to set a new password.

**Why this priority**: Password recovery is essential for user self-service and prevents locked-out admins from needing manual intervention. It depends on the email notification stub being in place.

**Independent Test**: Can be fully tested by navigating to Forgot Password, submitting a registered email, verifying the reset link is generated, following the link, and confirming a new password can be set.

**Acceptance Scenarios**:

1. **Given** a user on the Forgot Password page, **When** they submit a registered email address, **Then** a reset link is sent to that email and a confirmation message is shown.
2. **Given** a user who received a reset link, **When** they follow the link and enter a valid new password, **Then** their password is updated and they can log in with the new password.
3. **Given** a user who follows a reset link that has already been used or has expired, **Then** they see a clear error message and are prompted to request a new link.
4. **Given** a user on the Forgot Password page, **When** they submit an email that is not registered, **Then** the system shows the same confirmation message (to prevent email enumeration) but does not send an email.

---

### User Story 4 - Account Lockout After Repeated Failures (Priority: P4)

A user (or an attacker) who repeatedly submits incorrect login credentials is temporarily locked out of the account, with a clear message explaining the situation.

**Why this priority**: Lockout is a security control that protects all accounts from brute-force attacks. It is a baseline requirement before the application can be considered production-ready.

**Independent Test**: Can be fully tested by submitting incorrect credentials the threshold number of times and verifying the lockout message appears and the account cannot be accessed until the lockout period expires.

**Acceptance Scenarios**:

1. **Given** a user who submits incorrect credentials 5 consecutive times, **When** they attempt to log in again during the lockout period, **Then** a clear message informs them their account is temporarily locked.
2. **Given** a locked-out user, **When** the lockout period expires, **Then** they can attempt to log in again normally.
3. **Given** a user who fails login attempts but then succeeds before reaching the threshold, **Then** the failure counter resets.

---

### Edge Cases

- What happens when a user navigates directly to a protected admin page without being logged in? They must be redirected to the login page.
- What happens when a user submits the login or registration form with all fields empty? Client-side and server-side validation must reject the submission with field-level error messages.
- What happens when the email service is unavailable during a password reset request? The user sees a server-side error message; no silent failure occurs.
- What happens when a reset link token is tampered with or malformed? The system rejects it and shows a clear error.
- What happens when a registered user's account is set to inactive (`IsActive = false`) and they try to log in? They are denied access with an informative message.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow users to log in using their registered email address and password.
- **FR-002**: System MUST support a "Remember Me" option that persists the authenticated session across browser restarts.
- **FR-003**: System MUST lock a user's account temporarily after 5 consecutive failed login attempts and display a clear lockout message.
- **FR-004**: System MUST allow new users to register by providing full name, email, password, and password confirmation.
- **FR-005**: System MUST enforce password complexity: minimum 8 characters, at least one uppercase letter, at least one numeric digit, and at least one special character.
- **FR-006**: System MUST reject registration when the email is already associated with an existing account.
- **FR-007**: System MUST allow users to request a password reset by submitting their registered email address.
- **FR-008**: System MUST send a time-limited, single-use reset link to the user's email via a configurable email sender.
- **FR-009**: System MUST allow users to set a new password using a valid, unexpired reset token.
- **FR-010**: System MUST invalidate or reject reset tokens that have been used or have expired.
- **FR-011**: System MUST NOT reveal whether an email address is registered when a password reset is requested (to prevent email enumeration).
- **FR-012**: All authentication pages (Login, Register, Forgot Password, Reset Password) MUST use a standalone layout with no admin sidebar or topnav.
- **FR-013**: All authentication pages MUST use the primary brand color (`#004D82`) and Bootstrap 5 styling.
- **FR-014**: System MUST display clear, field-level and form-level error messages for all validation failures.
- **FR-015**: System MUST deny access to inactive accounts (`IsActive = false`) at login time with an informative message.

### Key Entities

- **ApplicationUser**: Represents a registered user of the system. Key attributes: full name, email address, active/inactive status, account creation date, and lockout tracking (failure count, lockout end time). Extends the platform's built-in identity user model.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A returning user can complete the login flow and reach the dashboard in under 30 seconds under normal conditions.
- **SC-002**: A new user can complete account registration and be ready to log in within 2 minutes.
- **SC-003**: A user who forgot their password can regain access within 5 minutes, assuming email is delivered promptly.
- **SC-004**: After 5 consecutive failed login attempts, the account lock is applied on the very next submission — no bypass is possible without waiting out the lockout period.
- **SC-005**: All four authentication pages (Login, Register, Forgot Password, Reset Password) render correctly and are fully usable on both desktop and mobile screen sizes.
- **SC-006**: Zero sensitive information (stack traces, internal error messages, or email existence hints) is exposed to the end user through the auth pages.

## Assumptions

- Email delivery for password reset is implemented via an interface (`IEmailSender`) with a stub/no-op implementation in the template. Actual SMTP configuration is the responsibility of the developer consuming the template.
- Authentication is email + password only for this phase; SSO, OAuth2, and social login are out of scope.
- Registration is open by default — no invite-only or admin-approval workflow is needed in this phase.
- "Remember Me" uses the platform's built-in persistent cookie mechanism with standard secure-cookie settings.
- Lockout defaults are: 5 failed attempts triggers a 5-minute lockout. Both values are configurable via `appsettings.json`.
- Password complexity rules (min 8 chars, uppercase, digit, special character) are applied uniformly to both registration and password reset flows.
- The authentication layout (no sidebar) is a separate Razor layout from the main admin layout built in Phase 3.
- Phase 1 (Foundation Scaffold) must be complete — the database schema, Identity configuration, and seeded SuperAdmin account must already exist before this phase begins.
