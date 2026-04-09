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
    - .specify/templates/spec-template.md ✅ no changes needed
    - .specify/templates/tasks-template.md ✅ no changes needed
  Follow-up TODOs: None
  ==================================================

  Sync Impact Report (v1.1.0)
  ==================================================
  Version change: 1.0.0 → 1.1.0
  Last amended: 2026-04-07
  Modified sections:
    - Core Principles: added Principle VIII (Design System First)
    - Technology Stack: added Fonts row; updated Font Awesome to 6.7+
    - Forbidden Patterns: 4 new entries (hardcoded colors, d-flex on body,
      Html.Partial, direction-specific properties)
    - New section: Design System Standards (color tokens, RTL rules,
      component classes, typography)
  Templates requiring updates:
    - .specify/templates/plan-template.md ✅ already updated (Plans/Plan.md
      already reflects new design system governance)
    - .specify/templates/spec-template.md ✅ no structural change needed
    - .specify/templates/tasks-template.md ✅ no structural change needed
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

### VIII. Design System First (NON-NEGOTIABLE)

Every view MUST use the CSS custom properties defined
in `wwwroot/css/site.css`. No hardcoded color values,
spacing values, or font declarations are permitted.

- The design token layer (`site.css`) is the single
  source of truth for visual decisions.
- All components (buttons, cards, badges, modals) MUST
  use the established class patterns documented in the
  Design System Standards section of this constitution.
- RTL support is achieved through CSS logical properties
  (`margin-inline-start`, `padding-inline-end`, etc.);
  direction-specific properties (`margin-left`,
  `padding-right`) are forbidden.
- The split-panel `_AuthLayout.cshtml` and sidebar
  `_Sidebar.cshtml` MUST NOT be modified to use inline
  styles or Bootstrap utility overrides.

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
| Fonts | Inter (LTR) / Cairo (RTL) | Google Fonts |
| Icons | Font Awesome | 6.7+ |
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
- Hardcoded color, spacing, or font values MUST NOT
  appear in any CSS, Razor, or HTML file — always
  reference a CSS custom property (e.g., `var(--primary)`).
- `d-flex` MUST NOT be applied to `<body>` — the fixed
  sidebar layout uses `margin-inline-start` on
  `.main-wrapper` instead.
- `@Html.Partial()` MUST NOT be used — always use the
  `<partial>` tag helper.
- Direction-specific CSS properties (`left`, `right`,
  `margin-left`, `padding-right`, etc.) MUST NOT be
  used — replace with CSS logical properties
  (`inline-start`, `inline-end`).

## Design System Standards

### Color Tokens

| Token | Value | Usage |
|---|---|---|
| `--primary` | `#4f46e5` | Primary actions, links, active states |
| `--primary-dark` | `#4338ca` | Hover/focus states for primary |
| `--primary-light` | `#ede9fe` | Soft badges, backgrounds |
| `--success` | `#10b981` | Success states, badges |
| `--danger` | `#ef4444` | Destructive actions, errors |
| `--warning` | `#f59e0b` | Warnings |
| `--info` | `#3b82f6` | Info states |
| `--text-primary` | `#1e293b` | Body text |
| `--text-secondary` | `#64748b` | Labels, captions |
| `--border-color` | `#e2e8f0` | Borders, dividers |
| `--body-bg` | `#f1f5f9` | Page background |
| `--sidebar-bg` | `#1e1b4b` | Sidebar background |
| `--sidebar-width` | `260px` | Sidebar expanded width |
| `--sidebar-collapsed` | `72px` | Sidebar collapsed width |

### Component Classes

| Component | Required Classes |
|---|---|
| Primary button | `.btn.btn-primary` |
| Danger button | `.btn.btn-danger` |
| Outline secondary | `.btn.btn-outline-secondary` |
| Outline danger | `.btn.btn-outline-danger` |
| Soft badge (success) | `.badge.badge-soft.badge-soft-success` |
| Soft badge (danger) | `.badge.badge-soft.badge-soft-danger` |
| Stat card | `.stat-card` with `.stat-icon` and `.stat-value[data-count]` |
| Page card | `.card.table-card` |
| Form field with icon | `.field-icon-wrap` wrapping `<input>` + `<span class="field-icon">` |

### Typography

- Body font (LTR): Inter, sans-serif
- Body font (RTL): Cairo, sans-serif
- Font switching is automatic via `[dir="rtl"]` CSS selector in `site.css`
- Do not declare `font-family` in component CSS; rely on the root rule in `site.css`

### RTL Rules

- Direction is toggled by setting `dir` attribute on `<html>` and persisted in `localStorage` key `textDirection`
- Use `margin-inline-start` / `margin-inline-end` instead of `margin-left` / `margin-right`
- Use `padding-inline-start` / `padding-inline-end` instead of `padding-left` / `padding-right`
- Use `inset-inline-start` / `inset-inline-end` instead of `left` / `right` in positioned elements
- `border-inline-start` / `border-inline-end` instead of `border-left` / `border-right`

### Layout Specs

**Sidebar**: `position: fixed; inset-block: 0; inset-inline-start: 0;` — width controlled by `--sidebar-width`. `.main-wrapper` uses `margin-inline-start: var(--sidebar-width)`. Collapsed state: `body.sidebar-collapsed` reduces both to `--sidebar-collapsed`.

**Auth layout**: Two-column split — decorative panel (`.auth-panel`, 45% width, gradient using `--primary`) on inline-start; form panel (`.auth-form-panel`) on inline-end. Collapses to single column below 768 px.

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

**Version**: 1.1.0 | **Ratified**: 2026-03-29 | **Last Amended**: 2026-04-07
