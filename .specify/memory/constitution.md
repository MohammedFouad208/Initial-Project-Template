<!--
  Sync Impact Report
  ==================================================
  Version change: N/A → 1.0.0 (initial ratification)
  Modified principles: N/A (all new)
  Added sections:
    - Core Principles (7 principles)
    - Technology Stack & Constraints
    - Security Requirements
    - Governance
  Removed sections: None
  Templates requiring updates:
    - .specify/templates/plan-template.md ✅ no changes needed
      (Constitution Check section is generic; will be filled per feature)
    - .specify/templates/spec-template.md ✅ no changes needed
      (Template is feature-agnostic; constitution constrains content)
    - .specify/templates/tasks-template.md ✅ no changes needed
      (Phase structure aligns with constitution principles)
  Follow-up TODOs: None
  ==================================================
-->

# ASP.NET Core Clean Architecture Admin Template Constitution

## Core Principles

### I. Clean Architecture (NON-NEGOTIABLE)

All code MUST follow strict layer separation:
`Domain → Application → Infrastructure → Web (MVC)`.

- **Domain**: Entities, interfaces, value objects. Zero
  external dependencies.
- **Application**: Services, DTOs, provider interfaces.
  Depends only on Domain.
- **Infrastructure**: EF Core, repositories, external
  providers. Implements Domain and Application interfaces.
- **Web (MVC)**: Controllers, views, static assets, filters.
  Depends on Application; references Infrastructure only
  for DI registration.

Projects MUST be named:
`{AppName}.Domain`, `{AppName}.Application`,
`{AppName}.Infrastructure`, `{AppName}.Web`.

### II. Dependency Inversion

All dependencies MUST point inward. The Web layer depends
on Application abstractions, never on Infrastructure
directly (except at the composition root in `Program.cs`
for DI wire-up).

- Interfaces are defined in Domain or Application.
- Implementations live in Infrastructure.
- Controllers receive Application-layer services via
  constructor injection.

### III. Thin Controllers

Controllers MUST NOT contain business logic. Their sole
responsibilities are:

- Accepting HTTP requests and binding input.
- Delegating to Application services/handlers.
- Returning views or JSON responses.

All business rules, validation logic, and data
orchestration MUST reside in Application services.

### IV. Repository + Unit of Work

All data access MUST go through the Repository pattern
backed by Entity Framework Core.

- One repository interface per aggregate root, defined
  in Domain (`IUserRepository`, `IRoleRepository`,
  `IPermissionRepository`).
- Implementations in Infrastructure using
  `ApplicationDbContext`.
- No direct `DbContext` usage in Application or Web
  layers.

### V. JSON-Driven Permissions

Permission objects and functions MUST NOT be hardcoded
in source code. They MUST be read from
`/Config/permissions.json` at startup via an
`IPermissionProvider` service.

- Adding a new object/function requires only a JSON
  edit — zero code changes.
- Roles are assigned permission combinations at runtime
  through the admin UI.

### VI. Generic DataTable Solution

A single `/wwwroot/js/datatable.js` file MUST handle
all server-side DataTables across the entire application.

- Each page initializes the table via a config object:
  `{ tableId, ajaxUrl, columns, permissions, createUrl,
    editUrl, deleteUrl }`.
- No per-page jQuery DataTable initialization is
  permitted.
- The generic script MUST support server-side sorting,
  searching, and pagination.

### VII. Permission-Aware UI

The generic DataTable JS and all UI components MUST
hide or show action buttons (Create, Edit, Delete,
Export) based on the current user's permissions.

- Permissions are resolved server-side and serialized
  into the page as a config object.
- Sidebar menu items MUST be hidden when the user lacks
  Browse permission for the corresponding object.

## Technology Stack & Constraints

### Mandated Stack

| Layer | Technology | Version |
|---|---|---|
| Framework | ASP.NET Core MVC | .NET 8 |
| ORM | Entity Framework Core | 8.x |
| Auth | ASP.NET Core Identity | Built-in |
| Database | SQL Server | 2019+ |
| Frontend | Bootstrap | 5.3 |
| Data Tables | DataTables.net | 1.13+ |
| Icons | Font Awesome | 6.x |
| Package Manager | NuGet (PM Console) | — |
| EF Migrations | Package Manager Console | — |

### Naming Conventions

- **Interfaces**: Prefix `I` (`IUserService`,
  `IRoleRepository`).
- **Services**: `UserService`, `RoleService`.
- **ViewModels**: Suffix `ViewModel`
  (`CreateUserViewModel`).
- **Controllers**: Plural noun (`UsersController`,
  `RolesController`).
- **Views**: Mirror controller/action structure
  (`/Views/Users/Index.cshtml`).
- **Migrations**: PM Console only — `Add-Migration`,
  `Update-Database`, `Script-Migration`.

### Forbidden Patterns

- `dotnet ef` CLI MUST NOT be used — always use
  PM Console.
- Business logic MUST NOT appear in Views or
  Controllers.
- Inline styles MUST NOT be used — use CSS variables
  anchored to `--primary-color: #004D82`.
- Roles, objects, or functions MUST NOT be hardcoded —
  always read from `permissions.json`.
- Per-page jQuery DataTable initialization MUST NOT
  exist — always use the generic `datatable.js`.

## Security Requirements

- All admin routes MUST be protected by the
  `[Authorize]` attribute.
- Permission checks MUST use a custom
  `[HasPermission("ObjectName", "FunctionName")]`
  authorization filter attribute.
- A custom 403 Forbidden view MUST be returned for
  unauthorized access.
- CSRF tokens (`[ValidateAntiForgeryToken]`) MUST be
  present on all POST actions.
- Identity password policy: minimum 8 characters,
  at least one uppercase letter, one digit, one
  special character.
- Account lockout MUST be configured after N failed
  login attempts.
- Anti-forgery tokens MUST be sent with AJAX DELETE
  requests from the generic DataTable.

## Governance

This constitution is the supreme authority for all
implementation decisions in this project. It supersedes
ad-hoc preferences, external tutorials, and AI-generated
suggestions that conflict with the principles above.

### Amendment Procedure

1. Propose the change by updating this document with the
   new or modified principle.
2. Increment the version using semantic versioning:
   - **MAJOR**: Principle removal or incompatible
     redefinition.
   - **MINOR**: New principle added or existing one
     materially expanded.
   - **PATCH**: Wording clarification, typo fix, or
     non-semantic refinement.
3. Update the `Last Amended` date.
4. Run the consistency propagation checklist against all
   templates in `.specify/templates/`.

### Compliance Review

- Every spec, plan, and task list MUST include a
  "Constitution Check" gate verifying alignment with
  these principles before implementation begins.
- Code reviews MUST verify that no forbidden pattern
  has been introduced.

**Version**: 1.0.0 | **Ratified**: 2026-03-29 | **Last Amended**: 2026-03-29
