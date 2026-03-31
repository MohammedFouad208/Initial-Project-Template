# Feature Specification: Foundation & Project Scaffold

**Feature Branch**: `001-foundation-scaffold`
**Created**: 2026-03-29
**Status**: Draft
**Input**: User description: "Foundation and Project Scaffold — Create solution with 4 clean architecture projects, configure Identity, EF Core, database connection, DI registrations, first migration, and seed SuperAdmin user"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Solution Builds and Runs (Priority: P1)

A developer clones the repository, opens the solution, and builds it. The solution compiles with zero errors and zero warnings. The developer can press F5 (or `dotnet run` on the Web project) and the application starts, displaying a default page or redirect to a login screen.

**Why this priority**: Without a runnable solution, no other feature can be developed or tested. This is the absolute foundation.

**Independent Test**: Build the solution from a clean clone and verify it compiles and starts without errors.

**Acceptance Scenarios**:

1. **Given** a freshly cloned repository, **When** the developer builds the solution, **Then** the build succeeds with 0 errors and 0 warnings.
2. **Given** the solution is built, **When** the developer starts the Web project, **Then** the application launches and responds to HTTP requests.
3. **Given** the solution structure, **When** inspecting project references, **Then** dependencies follow clean architecture direction: Web → Application ← Infrastructure, Application → Domain ← Infrastructure, and no reverse dependencies exist.

---

### User Story 2 - Database Created via Migration (Priority: P2)

A developer runs the initial database migration using Package Manager Console. The database is created on the configured SQL Server instance with all Identity tables plus the custom `RolePermissions` table. The extended columns (`FullName`, `IsActive`, `CreatedAt` on Users; `Description`, `CreatedAt` on Roles) are present.

**Why this priority**: The database is required before authentication or any data-driven feature can work.

**Independent Test**: Run `Update-Database` from PM Console against a fresh SQL Server instance and verify all expected tables and columns exist.

**Acceptance Scenarios**:

1. **Given** a configured connection string pointing to an empty SQL Server instance, **When** the developer runs `Update-Database` in PM Console, **Then** the database is created with all Identity tables and the `RolePermissions` table.
2. **Given** the migration has run, **When** inspecting the `AspNetUsers` table, **Then** columns `FullName` (string), `IsActive` (boolean), and `CreatedAt` (datetime) exist alongside standard Identity columns.
3. **Given** the migration has run, **When** inspecting the `AspNetRoles` table, **Then** columns `Description` (string) and `CreatedAt` (datetime) exist alongside standard Identity columns.
4. **Given** the migration has run, **When** inspecting the `RolePermissions` table, **Then** it contains columns `Id`, `RoleId` (foreign key to `AspNetRoles`), `ObjectName`, `FunctionName`, and `CreatedAt`.

---

### User Story 3 - SuperAdmin Seed Login (Priority: P3)

After running migrations, the developer starts the application and the data seeder automatically creates a SuperAdmin user, a SuperAdmin role, and assigns all permissions from `permissions.json` to that role. The developer can log in with the seeded SuperAdmin credentials.

**Why this priority**: A seeded admin account is essential for testing all subsequent features (auth pages, permission engine, management screens) without requiring manual user creation.

**Independent Test**: Start the application against a freshly migrated database, then attempt to log in with the seeded SuperAdmin credentials.

**Acceptance Scenarios**:

1. **Given** a freshly migrated database with no data, **When** the application starts for the first time, **Then** the seeder creates a SuperAdmin user with a known email and password.
2. **Given** the seeder has run, **When** checking the database, **Then** a "SuperAdmin" role exists and is assigned to the SuperAdmin user.
3. **Given** the seeder has run, **When** checking the `RolePermissions` table, **Then** all objects and functions defined in `permissions.json` are assigned to the SuperAdmin role.
4. **Given** the seeded SuperAdmin user exists, **When** the developer navigates to the login page and enters the SuperAdmin credentials, **Then** authentication succeeds and the user is redirected to the application.
5. **Given** the seeder has already run on a previous startup, **When** the application starts again, **Then** the seeder does not create duplicate users, roles, or permissions (idempotent seeding).

---

### User Story 4 - Identity Security Configuration (Priority: P4)

The application enforces the project's password and lockout policies. Users cannot register or be created with weak passwords. Accounts are locked out after repeated failed login attempts.

**Why this priority**: Security policies must be configured at the foundation level so all subsequent auth-related features inherit them automatically.

**Independent Test**: Attempt to create a user (via seeder or programmatically) with a password that violates the policy and verify rejection. Attempt multiple failed logins and verify lockout.

**Acceptance Scenarios**:

1. **Given** the application is running, **When** attempting to create a user with a password shorter than 8 characters, **Then** the operation fails with a validation error.
2. **Given** the application is running, **When** attempting to create a user with a password lacking an uppercase letter, a digit, or a special character, **Then** the operation fails with a validation error.
3. **Given** a valid user account, **When** the user fails login attempts beyond the configured lockout threshold, **Then** the account is locked out for the configured duration.

---

### Edge Cases

- What happens when the connection string is missing or invalid? The application should fail to start with a clear error message rather than silently crashing.
- What happens when `permissions.json` is missing or malformed? The application should report a configuration error at startup.
- What happens when the database already contains seed data from a prior run? The seeder must be idempotent — it should skip creation if the SuperAdmin user/role already exists.
- What happens when the `RolePermissions` table has a `RoleId` referencing a role that is deleted? The foreign key constraint should enforce referential integrity (cascade or restrict based on design choice).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The solution MUST contain exactly four projects following clean architecture: Domain, Application, Infrastructure, and Web (MVC).
- **FR-002**: Project references MUST follow clean architecture dependency direction — Web references Application; Infrastructure references Application and Domain; Application references Domain. No reverse dependencies.
- **FR-003**: The Domain layer MUST define an `ApplicationUser` entity extending the Identity user with `FullName` (string), `IsActive` (boolean, default true), and `CreatedAt` (datetime) properties.
- **FR-004**: The Domain layer MUST define an `ApplicationRole` entity extending the Identity role with `Description` (string) and `CreatedAt` (datetime) properties.
- **FR-005**: The Domain layer MUST define a `RolePermission` entity with `Id`, `RoleId` (foreign key to `ApplicationRole`), `ObjectName` (string), `FunctionName` (string), and `CreatedAt` (datetime) properties.
- **FR-006**: The Domain layer MUST define a `BaseEntity` class with common auditing properties that other entities can inherit from.
- **FR-007**: The Domain layer MUST define repository interfaces: `IUserRepository`, `IRoleRepository`, and `IPermissionRepository`.
- **FR-008**: The Infrastructure layer MUST implement the database context configured with Identity support and the `RolePermissions` table.
- **FR-009**: The Infrastructure layer MUST implement all repository interfaces defined in Domain.
- **FR-010**: The Web project MUST configure dependency injection for all Infrastructure and Application services in the composition root.
- **FR-011**: The Web project MUST configure Identity with the following password policy: minimum 8 characters, at least one uppercase letter, at least one digit, at least one special character.
- **FR-012**: The Web project MUST configure account lockout after a defined number of failed login attempts.
- **FR-013**: An initial database migration MUST be creatable and runnable via Package Manager Console (`Add-Migration`, `Update-Database`).
- **FR-014**: A data seeder MUST run on application startup and create a SuperAdmin user, a SuperAdmin role, assign the role to the user, and assign all permissions from `permissions.json` to the SuperAdmin role.
- **FR-015**: The data seeder MUST be idempotent — running multiple times must not produce duplicate data.
- **FR-016**: All admin routes MUST be protected with the `[Authorize]` attribute by default.

### Key Entities

- **ApplicationUser**: Represents a system user. Extends Identity's user with `FullName`, `IsActive` status flag, and `CreatedAt` timestamp. Linked to roles via the standard Identity user-role join.
- **ApplicationRole**: Represents a security role. Extends Identity's role with a `Description` and `CreatedAt` timestamp. Linked to permissions via the `RolePermission` entity.
- **RolePermission**: Represents an individual permission assignment. Links a role to a specific object-function pair (e.g., "Employee" + "Create"). References `ApplicationRole` via foreign key.
- **BaseEntity**: Provides common auditing fields (identifier, timestamps) for non-Identity entities.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The solution builds from a clean clone with 0 errors and 0 warnings.
- **SC-002**: The initial database migration runs successfully against a blank SQL Server instance in under 30 seconds.
- **SC-003**: The seeded SuperAdmin user can log in on the first application startup without any manual database intervention.
- **SC-004**: All four architectural layers are independently recognizable as separate projects with correct dependency directions verifiable through project reference inspection.
- **SC-005**: A developer new to the project can go from clone to running application (build → migrate → run → login) in under 5 minutes following the setup instructions.
- **SC-006**: The `RolePermissions` table contains one row per object-function combination from `permissions.json` for the SuperAdmin role after seeding (currently 16 rows based on 4 objects × their respective functions).

## Assumptions

- SQL Server 2019+ is available on the developer's machine (LocalDB or a full instance).
- The developer has .NET 8 SDK installed.
- The developer uses Visual Studio with Package Manager Console for EF migrations (not `dotnet ef` CLI, per constitution).
- The `permissions.json` file will be read by a provider implemented in a later phase (Phase 4); for seeding purposes in this phase, the seeder reads the JSON file directly.
- No UI pages are delivered in this phase — authentication pages, dashboard, and layouts are scoped to Phases 2 and 3.
- The application name placeholder (`AppName`) will be replaced with a concrete project name at scaffold time. The default name used is `AdminTemplate`.
