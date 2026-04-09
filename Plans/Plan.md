# 🏗️ ASP.NET Core Clean Architecture Admin Template
## Spec-Driven Development Plan (Spec Kit Methodology)

> **Primary Color:** `#4f46e5` (Indigo)  
> **Methodology:** Spec-Driven Development (SDD) — Constitution → Specify → Plan → Phases → Implement  
> **Goal:** A production-ready, reusable ASP.NET Core MVC admin template with clean architecture, dynamic role/permission management, and a modern Bootstrap dashboard with full RTL support.

---

## 📜 CONSTITUTION — Project DNA

> Non-negotiable principles that every phase and every AI-generated file must respect.

### Stack & Versions
| Layer | Technology | Version |
|---|---|---|
| Framework | ASP.NET Core MVC | .NET 8 |
| ORM | Entity Framework Core | 8.x |
| Auth | ASP.NET Core Identity | Built-in |
| Database | SQL Server | 2019+ |
| Frontend | Bootstrap | 5.3 |
| Data Tables | DataTables.net | 1.13+ |
| Icons | Font Awesome | 6.7+ |
| Fonts | Inter (LTR) / Cairo (RTL) | Google Fonts |
| Package Manager | NuGet (PM Console) | - |
| EF Migrations | Package Manager Console | `Script-Migration`, `Update-Database` |

### Architectural Principles
1. **Clean Architecture** — strict layer separation: `Domain → Application → Infrastructure → Web (MVC)`
2. **Dependency Inversion** — all dependencies point inward; Web depends on Application, not Infrastructure directly
3. **No business logic in Controllers** — controllers are thin; logic lives in Application Services / Handlers
4. **Repository + Unit of Work** pattern for all data access
5. **JSON-driven permissions** — objects and functions are not hardcoded; they are read from a configuration file
6. **Generic DataTable solution** — one JS file handles all server-side DataTables across the entire app
7. **Permission-aware UI** — the generic DataTable JS hides/shows action buttons based on current user permissions
8. **Design system first** — every view must use the established CSS custom properties and component classes; never deviate from the design token layer

### Naming Conventions
- **Projects:** `{AppName}.Domain`, `{AppName}.Application`, `{AppName}.Infrastructure`, `{AppName}.Web`
- **Interfaces:** `IUserService`, `IRoleRepository` (prefix `I`)
- **Services:** `UserService`, `RoleService`
- **ViewModels:** `UserViewModel`, `CreateUserViewModel` (suffix `ViewModel`)
- **Controllers:** `UsersController`, `RolesController` (plural noun)
- **Views:** mirror controller/action structure — `/Views/Users/Index.cshtml`
- **Migrations:** PM Console only — `Add-Migration`, `Update-Database`, `Script-Migration`

### Forbidden Patterns
- ❌ No `dotnet ef` CLI — always use PM Console
- ❌ No business logic in Views or Controllers
- ❌ No arbitrary inline styles — use CSS custom properties from the design token layer (see Design System below)
- ❌ No hardcoded colors (e.g. `#004D82`, `blue`) — always reference `var(--primary)`, `var(--success)`, etc.
- ❌ No hardcoded roles, objects, or functions — always read from `permissions.json`
- ❌ No jQuery DataTable initialization duplicated per page — always use the generic `datatable.js`
- ❌ No Bootstrap `d-flex` on `<body>` — layout is managed by `.main-wrapper` + fixed sidebar
- ❌ No `@Html.Partial()` in new views — always use `<partial>` tag helper or `Html.PartialAsync()`
- ❌ No direction-specific margin/padding properties (`margin-left`, `padding-right`) in shared CSS — always use logical properties (`margin-inline-start`, `padding-inline-end`) for RTL compatibility

### Security Constraints
- All admin routes protected by `[Authorize]` attribute
- Permission checks via a custom `[HasPermission("Employee", "Create")]` attribute
- CSRF tokens on all POST forms
- Identity passwords: min 8 chars, uppercase, digit, special char

---

### 🎨 Design System (Non-Negotiable)

Every view, partial, and layout **must** consume the following tokens from `site.css`. Adding new colors or spacing values outside this system is forbidden.

#### Color Tokens
| Token | Value | Usage |
|---|---|---|
| `--primary` | `#4f46e5` | Buttons, active states, links, focus rings |
| `--primary-dark` | `#3730a3` | Hover on primary elements |
| `--primary-light` | `#eef2ff` | Icon backgrounds, hover fills, badge backgrounds |
| `--success` | `#10b981` | Success badges, trend-up indicators |
| `--warning` | `#f59e0b` | Warning badges, stat accents |
| `--danger` | `#ef4444` | Error alerts, delete states, trend-down |
| `--info` | `#06b6d4` | Info badges, stat accents |
| `--bg-body` | `#f1f5f9` | Page background |
| `--bg-card` | `#ffffff` | Card / panel backgrounds |
| `--text-primary` | `#0f172a` | Headings and body text |
| `--text-secondary` | `#64748b` | Labels, secondary copy |
| `--text-muted` | `#94a3b8` | Timestamps, hints |
| `--border-color` | `#e2e8f0` | Card borders, dividers |

#### Sidebar
- Background: `#1e1b4b` (dark indigo)
- Fixed position, width `260px` / collapses to `72px`
- Collapse state persisted to `localStorage`
- Active link: `var(--primary)` background + drop shadow
- Mobile: slides in via CSS transform + dark backdrop overlay

#### Typography
- **LTR:** `Inter` (Google Fonts) — weights 400, 500, 600, 700
- **RTL:** `Cairo` (Google Fonts) — weights 400, 500, 600, 700
- Font switched automatically via `[dir="rtl"]` selector in `site.css`
- Base size: `0.9375rem`; headings use `font-weight: 700`

#### RTL Support
- `dir` attribute managed by `site.js`; persisted to `localStorage` key `textDirection`
- Inline script in `<head>` of every layout prevents FOUC on direction load
- All layouts expose an **RTL/LTR toggle** button to the user
- Use CSS logical properties throughout: `margin-inline-start`, `padding-inline-end`, `inset-inline-start`, `border-inline-start`
- Flexbox rows with `gap` naturally reorder in RTL — do not override with fixed margins

#### Component Classes (defined in `site.css`)
| Class | Purpose |
|---|---|
| `.stat-card` | Dashboard stat panel with hover lift + colored left border |
| `.stat-card.stat-{primary\|success\|warning\|info}` | Colored left accent per card variant |
| `.stat-icon.icon-{primary\|success\|warning\|info}` | Circular icon badge |
| `.badge-soft.badge-soft-{primary\|success\|warning\|danger}` | Soft pill badge |
| `.table th / td` | Tabular data with uppercase headers and hover rows |
| `.btn-primary` | Indigo button with hover shadow + ripple effect |
| `.btn-loading` | Spinner state injected by `site.js` on form submit |
| `.field-icon-wrap` / `.field-icon` | Input with inline leading icon (RTL-aware via `inset-inline-start`) |
| `.activity-item` | Feed row with colored dot + text + timestamp |
| `.page-header` | Section above page content with `h1` + breadcrumb |

#### Auth Layout (`_AuthLayout.cshtml`)
- Split-panel: decorative gradient left panel + white form right panel
- Left panel hidden on mobile (`d-none d-md-flex`)
- Every auth page title set via `ViewData["Title"]`
- Inputs use `.field-icon-wrap` for leading icons
- Validation errors render in `.alert.alert-danger` with `border-inline-start: 4px solid var(--danger)`
- Submit button uses `.btn-loading` class (added by `site.js`) during form submit

#### Dashboard Layout (`_Layout.cshtml`)
- Body has no flex class — layout handled by fixed `.sidebar` + `.main-wrapper` with `margin-inline-start`
- Topnav is `position: sticky; top: 0` at `z-index: 1020`
- Page content wrapper: `.page-content` with `padding: 1.75rem 1.5rem`
- Each page starts with a `.page-header` block containing `<h1>` + breadcrumb

---

## 📋 SPECIFICATION — What We Are Building

### Problem Statement
Developers building ASP.NET Core admin applications repeatedly write the same boilerplate: auth, role management, permission systems, and DataTables. This template eliminates that repetition with a clean, extensible foundation.

### Users & Roles
| Actor | Description |
|---|---|
| Super Admin | Full system access; manages users, roles, permissions |
| Admin | Access to objects/functions as granted by Super Admin |
| End User | Limited access; sees only what their role permits |

### Core Features

#### F1 — Authentication (Microsoft Identity)
- Login page with email + password
- Register/Signup page
- Forgot Password → Email reset link → Reset Password
- Remember Me support
- Lockout after N failed attempts

#### F2 — Dynamic Permission System
- Permissions defined in `/Config/permissions.json`
- Structure: `{ "Objects": [ { "Name": "Employee", "Functions": ["Browse", "Create", "Update", "Delete", "Export"] } ] }`
- Loaded at startup via `IPermissionProvider` service
- Roles are assigned permission combinations at runtime (not hardcoded)
- Custom `AuthorizationHandler` reads assigned permissions per user's role

#### F3 — Admin Dashboard (Bootstrap 5)
- **Primary color:** `var(--primary)` = `#4f46e5`; all theme colors from design token layer
- Fixed sidebar (`#1e1b4b`) collapsing to icon-only `72px` strip; state in `localStorage`
- Topnav: hamburger toggle, page title, RTL/LTR toggle, notification bell, user avatar with initials + dropdown
- Dashboard home: 4 stat cards (Total Users, Active Users, Total Roles, Inactive Users) with animated counter, trend indicators, hover lift
- Recent activity feed; chart placeholder area; recent users table with soft badges
- Responsive — mobile sidebar slides in via CSS transform + dark backdrop

#### F4 — User Management
- DataTable list: Name, Email, Roles, Status, Actions
- Create/Edit modal or page
- Assign multiple roles to a user
- Activate/Deactivate user (soft delete)
- Server-side search, sort, pagination

#### F5 — Role Management
- DataTable list: Role Name, Permissions count, Users count, Actions
- Create/Edit Role
- Permission assignment UI: grouped by Object → checkboxes per Function
- Delete Role (with guard: cannot delete role that has users)

#### F6 — Role-Permission Management Page
- Matrix view: Rows = Objects, Columns = Functions
- Checkbox grid per role
- Save sends a batch of permission assignments
- Reads object/function list from `permissions.json`

#### F7 — Generic DataTable Solution
- Single `/wwwroot/js/datatable.js` file
- Accepts config object: `{ url, columns, permissions, createUrl, editUrl, deleteUrl }`
- Automatically hides Create/Edit/Delete buttons if user lacks that permission
- Supports server-side: sorting, searching, pagination
- Passes anti-forgery token on delete
- Emits events: `dtable:created`, `dtable:deleted` for custom hooks

#### F8 — Authorization Middleware & Attribute
- `[HasPermission("ObjectName", "FunctionName")]` action filter attribute
- Returns 403 Forbidden view for unauthorized access
- Service `IPermissionService.UserHasPermission(userId, objectName, functionName)`

---

## 🗂️ SOLUTION STRUCTURE

```
📁 Solution: AppName
├── 📁 AppName.Domain
│   ├── Entities/
│   │   ├── ApplicationUser.cs         (extends IdentityUser)
│   │   ├── ApplicationRole.cs         (extends IdentityRole)
│   │   └── RolePermission.cs          (RoleId, ObjectName, FunctionName)
│   ├── Interfaces/
│   │   ├── IUserRepository.cs
│   │   ├── IRoleRepository.cs
│   │   └── IPermissionRepository.cs
│   └── Common/
│       └── BaseEntity.cs
│
├── 📁 AppName.Application
│   ├── Services/
│   │   ├── UserService.cs
│   │   ├── RoleService.cs
│   │   └── PermissionService.cs
│   ├── Interfaces/
│   │   ├── IUserService.cs
│   │   ├── IRoleService.cs
│   │   └── IPermissionService.cs
│   ├── DTOs/
│   │   ├── UserDto.cs
│   │   ├── RoleDto.cs
│   │   └── PermissionDto.cs
│   └── Providers/
│       └── IPermissionProvider.cs     (reads permissions.json)
│
├── 📁 AppName.Infrastructure
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── Migrations/                (PM Console managed)
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   ├── RoleRepository.cs
│   │   └── PermissionRepository.cs
│   ├── Providers/
│   │   └── JsonPermissionProvider.cs  (reads /Config/permissions.json)
│   └── Extensions/
│       └── InfrastructureServiceExtensions.cs
│
└── 📁 AppName.Web (MVC)
    ├── Controllers/
    │   ├── AccountController.cs       (Login, Register, ForgotPassword)
    │   ├── DashboardController.cs
    │   ├── UsersController.cs
    │   ├── RolesController.cs
    │   └── RolePermissionsController.cs
    ├── Views/
    │   ├── Shared/
    │   │   ├── _Layout.cshtml         (sidebar + topnav)
    │   │   ├── _Sidebar.cshtml
    │   │   ├── _Topnav.cshtml
    │   │   └── _Notification.cshtml
    │   ├── Account/
    │   │   ├── Login.cshtml
    │   │   ├── Register.cshtml
    │   │   ├── ForgotPassword.cshtml
    │   │   └── ResetPassword.cshtml
    │   ├── Dashboard/
    │   │   └── Index.cshtml
    │   ├── Users/
    │   │   ├── Index.cshtml
    │   │   ├── Create.cshtml
    │   │   └── Edit.cshtml
    │   ├── Roles/
    │   │   ├── Index.cshtml
    │   │   ├── Create.cshtml
    │   │   └── Edit.cshtml
    │   └── RolePermissions/
    │       └── Index.cshtml           (permission matrix)
    ├── wwwroot/
    │   ├── css/
    │   │   ├── site.css               (CSS variables, primary color)
    │   │   └── sidebar.css
    │   └── js/
    │       ├── datatable.js           (GENERIC DataTable solution)
    │       └── permissions-helper.js
    ├── Config/
    │   └── permissions.json           (objects + functions definition)
    ├── Filters/
    │   └── HasPermissionAttribute.cs  (custom auth filter)
    └── Program.cs / Startup
```

---

## 📄 PERMISSIONS.JSON STRUCTURE

```json
{
  "PermissionObjects": [
    {
      "Name": "Employee",
      "DisplayName": "Employees",
      "Functions": ["Browse", "Create", "Update", "Delete", "Export"]
    },
    {
      "Name": "User",
      "DisplayName": "Users",
      "Functions": ["Browse", "Create", "Update", "Delete"]
    },
    {
      "Name": "Role",
      "DisplayName": "Roles",
      "Functions": ["Browse", "Create", "Update", "Delete", "AssignPermissions"]
    },
    {
      "Name": "Report",
      "DisplayName": "Reports",
      "Functions": ["Browse", "Export"]
    }
  ]
}
```

---

## 🗄️ DATABASE SCHEMA (Key Tables)

```
AspNetUsers          (Identity — extended with: FullName, IsActive, CreatedAt)
AspNetRoles          (Identity — extended with: Description, CreatedAt)
AspNetUserRoles      (Identity)
RolePermissions      (Id, RoleId FK→AspNetRoles, ObjectName, FunctionName, CreatedAt)
```

---

## 🎨 GENERIC DATATABLE.JS DESIGN

```javascript
// Usage example on any page:
AppDataTable.init({
  tableId: '#employeesTable',
  ajaxUrl: '/Employees/GetData',
  columns: [
    { data: 'name', title: 'Name' },
    { data: 'email', title: 'Email' },
    { data: 'department', title: 'Department' },
  ],
  permissions: {
    canCreate: @Json.Serialize(await _permissionService.HasPermission("Employee","Create")),
    canUpdate: @Json.Serialize(await _permissionService.HasPermission("Employee","Update")),
    canDelete: @Json.Serialize(await _permissionService.HasPermission("Employee","Delete")),
  },
  createUrl: '/Employees/Create',
  editUrl: '/Employees/Edit',
  deleteUrl: '/Employees/Delete',
  objectName: 'Employee'   // for display in confirm dialogs
});
```

---

## 🗓️ PHASES & TASKS

---

### PHASE 1 — Foundation & Project Scaffold
**Goal:** Runnable solution with clean architecture layers, DB connected, Identity configured.

| # | Task | Deliverable |
|---|---|---|
| 1.1 | Create solution with 4 projects (Domain, Application, Infrastructure, Web) | `.sln` + 4 `.csproj` |
| 1.2 | Add project references following clean architecture direction | Dependency graph correct |
| 1.3 | Define `ApplicationUser`, `ApplicationRole`, `BaseEntity` in Domain | Entity classes |
| 1.4 | Configure `ApplicationDbContext` with Identity + `RolePermissions` table | `DbContext` |
| 1.5 | Setup DI registrations: Infrastructure services, Application services | `Program.cs` |
| 1.6 | Configure Identity options (password rules, lockout) | `Program.cs` |
| 1.7 | Run first migration via PM Console | `InitialCreate` migration |
| 1.8 | Seed: SuperAdmin user + SuperAdmin role + all permissions from JSON | `DataSeeder.cs` |

**Acceptance Criteria:** App runs, DB created, can login with seeded SuperAdmin.

---

### PHASE 2 — Authentication Pages
**Goal:** Complete auth flow with branded, modern UI matching the design system.

> **UI Rule:** All auth views must use `_AuthLayout.cshtml` (split-panel). Every input must use `.field-icon-wrap` + `.field-icon`. Validation errors must render inside `.alert.alert-danger` with `border-inline-start`. The submit CTA must be `.btn.btn-primary.btn-lg` with `.btn-loading` support. The layout must include the RTL/LTR toggle button.

| # | Task | Deliverable |
|---|---|---|
| 2.1 | Create `AccountController` with Login, Register, ForgotPassword, ResetPassword actions | Controller |
| 2.2 | Design Login page: split-panel layout, icon inputs, Remember Me checkbox, Forgot link | `Login.cshtml` |
| 2.3 | Design Register page: full name, email, password, confirm password with icon inputs | `Register.cshtml` |
| 2.4 | Design Forgot Password + Confirmation + Reset Password pages | All 3 views |
| 2.5 | Configure email sender (SMTP stub/interface) for reset link | `IEmailSender` impl |
| 2.6 | Style all auth pages via `_AuthLayout.cshtml` — decorative left panel, white right form | Auth layout |
| 2.7 | Implement lockout feedback on Login page with `.alert.alert-danger` styling | UX message |

**Acceptance Criteria:** Full auth flow works end to end. All pages match the split-panel design. RTL toggle works on auth pages. Reset email link lands on correct page.

---

### PHASE 3 — Admin Layout & Dashboard
**Goal:** Modern admin shell that all future pages will use, built on the design system.

> **UI Rule:** `_Layout.cshtml` must not add `d-flex` to `<body>`. Sidebar is `position: fixed` from `sidebar.css`. Main content uses `.main-wrapper` with `margin-inline-start: var(--sidebar-width)`. Topnav must include: hamburger (`.sidebar-desktop-toggle`), page title (`.topnav-title`), RTL toggle (`.rtl-toggle-btn`), notification bell, user avatar with initials (`.topnav-avatar`). All layouts include the inline FOUC-prevention script for `dir` attribute.

| # | Task | Deliverable |
|---|---|---|
| 3.1 | Create `_Layout.cshtml`: no `d-flex` on body, sidebar backdrop div, Google Fonts `<link>`, inline dir script | Shared layout |
| 3.2 | Build sidebar: `#1e1b4b` bg, brand icon, section labels, icon + text nav links, active state with shadow | `_Sidebar.cshtml` |
| 3.3 | Build topnav: hamburger, page title, RTL toggle, notification bell, avatar dropdown with initials | `_Topnav.cshtml` |
| 3.4 | CSS variables: full design token layer including colors, shadows, radii, transitions, typography | `site.css` + `sidebar.css` |
| 3.5 | Dashboard: 4 stat cards (`data-count` animation), activity feed, chart placeholder, users table | `Dashboard/Index.cshtml` |
| 3.6 | Responsive: mobile sidebar uses `transform` slide-in + `sidebar-backdrop` overlay; desktop uses collapse-to-72px | `sidebar.css` + `site.js` |
| 3.7 | `site.js`: `initSidebar()`, `initRtlToggle()`, `initFormLoadingStates()`, `animateCounters()`, ripple effect | `site.js` |
| 3.8 | Logout action with confirmation | Topnav → AccountController |

**Acceptance Criteria:** Admin layout renders correctly on desktop, tablet, and mobile. Sidebar collapses to icon strip on desktop. Mobile sidebar slides in with backdrop. RTL toggle mirrors the entire layout. Counter animation runs on page load.

---

### PHASE 4 — Permission Engine
**Goal:** JSON-driven permission system with custom authorization attribute.

> **UI Rule:** The 403 error page (`Error403.cshtml`) must use `_Layout.cshtml`, show a large lock icon in `var(--danger-light)`, and provide a back-link button styled as `.btn.btn-outline-secondary`. Apply the `.page-header` pattern.

| # | Task | Deliverable |
|---|---|---|
| 4.1 | Create `permissions.json` in `/Config/` with Employee, User, Role, Report objects | JSON file |
| 4.2 | Implement `IPermissionProvider` + `JsonPermissionProvider` reading the JSON | Provider class |
| 4.3 | Create `RolePermission` entity + repository + service | CRUD service |
| 4.4 | Implement `IPermissionService.UserHasPermission(userId, object, function)` | Service method |
| 4.5 | Build `HasPermissionAttribute` (IAsyncAuthorizationFilter) | Custom filter attribute |
| 4.6 | Register permission services in DI | `Program.cs` |
| 4.7 | Return custom 403 view styled to design system | `Views/Shared/Error403.cshtml` |
| 4.8 | Expose `PermissionContext` to Razor views via Tag Helper or ViewBag helper | Permission-aware views |

**Acceptance Criteria:** Applying `[HasPermission("Employee","Create")]` to an action blocks unauthorized users with 403 page matching the design system.

---

### PHASE 5 — Generic DataTable Solution
**Goal:** Reusable JS file used by all management pages; permission-aware buttons styled to the design system.

> **UI Rule:** The Create button must use `.btn.btn-primary.btn-sm` with a `fa-plus` icon. Edit button: `.btn.btn-outline-secondary.btn-sm`. Delete button: `.btn.btn-outline-danger.btn-sm`. Table wrapper div must use `.card.table-card`. `<thead>` cells must match the `.table th` styles (uppercase, `var(--text-secondary)`). Use `badge-soft` classes for status columns. Delete confirmation must render a styled modal or dialog — not a raw `window.confirm()`.

| # | Task | Deliverable |
|---|---|---|
| 5.1 | Create `AppDataTable` JS object in `/wwwroot/js/datatable.js` | JS file |
| 5.2 | Config API: `init({ tableId, ajaxUrl, columns, permissions, createUrl, editUrl, deleteUrl })` | API design |
| 5.3 | Render toolbar above table: Create button (hidden if `!canCreate`), search input | Toolbar rendering |
| 5.4 | Render action column: Edit/Delete per row styled to design system (hidden if `!canUpdate/!canDelete`) | Column rendering |
| 5.5 | Server-side: pass `draw`, `start`, `length`, `search`, `order` to controller | AJAX params |
| 5.6 | Controller base method: `GetDataTableResponse<T>(IQueryable<T>, request)` generic helper | `DataTableHelper.cs` |
| 5.7 | Delete confirmation: Bootstrap 5 modal with object name, danger-styled confirm button | Modal UI |
| 5.8 | CSRF token injection on delete POST | Security |
| 5.9 | Loading skeleton, empty-state illustration, error state with retry — all styled to design tokens | UX polish |

**Acceptance Criteria:** Any page can initialize a DataTable with 5 lines of JS. Buttons hide/show per permission. All UI elements match the design system.

---

### PHASE 6 — User Management
**Goal:** Full CRUD for users with role assignment, styled to the design system.

> **UI Rule:** Index page uses `.page-header` + `AppDataTable`. Status column renders `.badge-soft.badge-soft-success` (Active) / `.badge-soft.badge-soft-danger` (Inactive). User avatars in the table use a small circle with initials (same as `.topnav-avatar` pattern). Create/Edit forms use `.card` wrapper, `.form-label`, `.form-control` with focus ring, and a `.btn.btn-primary` submit. Form validation errors use `.alert.alert-danger` with `border-inline-start`.

| # | Task | Deliverable |
|---|---|---|
| 6.1 | `UsersController.Index` → view with `.page-header`, breadcrumb, `AppDataTable` | `Users/Index.cshtml` |
| 6.2 | `UsersController.GetData` → server-side DataTable endpoint (JSON) | API action |
| 6.3 | Create User page: icon inputs, role multi-select, active toggle — styled to design system | `Users/Create.cshtml` |
| 6.4 | Edit User page: same styling, no password field | `Users/Edit.cshtml` |
| 6.5 | Activate/Deactivate: toggle renders as `.badge-soft` pill with click handler | Action + UI |
| 6.6 | `UserService`: CreateAsync, UpdateAsync, DeactivateAsync, GetPagedAsync | Service layer |
| 6.7 | Apply `[HasPermission("User", "Browse/Create/Update/Delete")]` | Attributes on actions |

**Acceptance Criteria:** Users list loads via DataTable. CRUD operations work. Role assignment saves correctly. All UI matches the design system.

---

### PHASE 7 — Role Management
**Goal:** Full CRUD for roles, styled to the design system.

> **UI Rule:** Same patterns as Phase 6. The "Manage Permissions" action button per row uses `.btn.btn-outline-primary.btn-sm` with a `fa-key` icon. Delete error (role has users) must render as `.alert.alert-danger` with `border-inline-start: 4px solid var(--danger)` — not a raw exception page.

| # | Task | Deliverable |
|---|---|---|
| 7.1 | `RolesController.Index` → `.page-header`, breadcrumb, `AppDataTable` with: Name, Description, User Count, Permission Count, Actions | `Roles/Index.cshtml` |
| 7.2 | `RolesController.GetData` → server-side JSON | API action |
| 7.3 | Create/Edit Role: `.card` wrapper, `.form-control` inputs, `.btn.btn-primary` submit | Views |
| 7.4 | Delete Role guard: return `.alert.alert-danger` if users assigned | Service validation + UX |
| 7.5 | `RoleService`: CreateAsync, UpdateAsync, DeleteAsync, GetPagedAsync | Service layer |
| 7.6 | Apply permission attributes | Security |

**Acceptance Criteria:** Roles CRUD works. Deleting a role with users shows a styled error message matching the design system.

---

### PHASE 8 — Role-Permission Management
**Goal:** Matrix UI to assign permissions to roles, styled to the design system.

> **UI Rule:** Matrix wrapper uses `.card`. Object rows have a sticky first column. Checkboxes styled with Bootstrap 5 `.form-check-input`. "Check All" for a row is a small `.btn.btn-outline-secondary.btn-sm`. The Save button is `.btn.btn-primary` with `.btn-loading` support. Success/error feedback after save uses `.alert.alert-success` / `.alert.alert-danger`.

| # | Task | Deliverable |
|---|---|---|
| 8.1 | `RolePermissionsController.Index(roleId)` — load matrix for a role; `.page-header` with role name | Controller action |
| 8.2 | Permission matrix: `.card` wrapper, styled thead, Bootstrap checkboxes, sticky object column | `RolePermissions/Index.cshtml` |
| 8.3 | "Check All" per row; "Uncheck All"; select-all-in-column — styled to design system | JS UX |
| 8.4 | Save: POST batch → replace all for that role; redirect with `.alert.alert-success` TempData message | POST action |
| 8.5 | `PermissionService.SaveRolePermissions(roleId, List<PermissionDto>)` | Service method |
| 8.6 | Link from Roles list: "Manage Permissions" `.btn.btn-outline-primary.btn-sm` with `fa-key` icon | Navigation |

**Acceptance Criteria:** SuperAdmin can define exactly which objects/functions Role X can access. Saves persist correctly. All UI matches the design system.

---

### PHASE 9 — Polish, Guards & Seed Data
**Goal:** Production-ready hardening with full design system compliance audit.

> **UI Rule:** Toast notifications use a Bootstrap 5 Toast positioned `bottom-end`, with `var(--success)` / `var(--danger)` border-inline-start. Error pages (403, 404) must use `_Layout.cshtml`, center a large icon in the appropriate soft-color circle, and provide a styled back/home button. Sidebar item visibility must not affect the DOM structure — use `d-none` class, not conditional rendering, to keep the sidebar layout stable.

| # | Task | Deliverable |
|---|---|---|
| 9.1 | Sidebar: permission-driven `d-none` on nav links user cannot access | `_Sidebar.cshtml` |
| 9.2 | 403 page: large `fa-lock` in `var(--danger-light)` circle, message, back button; 404 similarly | `Error403.cshtml`, `Error404.cshtml` |
| 9.3 | Add `[ValidateAntiForgeryToken]` to all POST actions | Security audit |
| 9.4 | Comprehensive seed: 3 demo roles (SuperAdmin, Admin, Viewer) with permission sets | `DataSeeder.cs` |
| 9.5 | `README.md`: clone → connection string → migration → seed → run | Documentation |
| 9.6 | `appsettings.json`: connection string placeholder, SMTP config, lockout config | Config file |
| 9.7 | jQuery Validate + Bootstrap 5 integration on all forms; real-time `.is-valid`/`.is-invalid` classes | All forms |
| 9.8 | Toast container in `_Layout.cshtml`; `site.js` exposes `AppToast.show(message, type)` using design tokens | Toast system |
| 9.9 | Design system audit: verify every view uses only design-token colors, logical properties, and established component classes | Final review |

**Acceptance Criteria:** App fully secured. No raw errors exposed. All pages pass the design system audit. RTL works on every page. Ready to clone and extend.

---

## 🔗 PHASE DEPENDENCY MAP

```
Phase 1 (Foundation)
    └── Phase 2 (Auth Pages)
            └── Phase 3 (Admin Layout)
                    └── Phase 4 (Permission Engine)
                            └── Phase 5 (Generic DataTable)
                                    ├── Phase 6 (User Management)
                                    ├── Phase 7 (Role Management)
                                    └── Phase 8 (Role-Permission Matrix)
                                                └── Phase 9 (Polish & Hardening)
```

---

## 📊 EFFORT ESTIMATE

| Phase | Complexity | Est. Sessions |
|---|---|---|
| Phase 1 — Foundation | Medium | 1 |
| Phase 2 — Auth Pages | Low | 1 |
| Phase 3 — Admin Layout | Medium | 1 |
| Phase 4 — Permission Engine | High | 2 |
| Phase 5 — Generic DataTable | High | 2 |
| Phase 6 — User Management | Medium | 1 |
| Phase 7 — Role Management | Medium | 1 |
| Phase 8 — Role-Permission Matrix | Medium | 1 |
| Phase 9 — Polish & Hardening | Low | 1 |
| **Total** | | **~11 sessions** |

---

## 🧪 ACCEPTANCE CRITERIA (Overall)

### Functional
- [ ] Solution builds with 0 errors and 0 warnings
- [ ] DB migrates cleanly from scratch via PM Console
- [ ] SuperAdmin seed user can log in and access all features
- [ ] A role with only "Employee → Browse" permission cannot access Create/Edit/Delete
- [ ] DataTable loads server-side on Users, Roles pages
- [ ] Create/Edit/Delete buttons hidden automatically based on permissions
- [ ] Permission matrix saves correctly and takes effect immediately on next request
- [ ] All POST actions protected with CSRF tokens
- [ ] Custom 403 page shown for unauthorized access attempts

### Design System
- [ ] Every page uses only design-token CSS variables — no hardcoded colors anywhere
- [ ] Sidebar is `position: fixed`, collapses to 72px on desktop, slides in on mobile with backdrop
- [ ] RTL toggle switches the entire layout (sidebar, forms, tables, icons) without a page reload
- [ ] Layout direction persists across page navigation and browser refresh via `localStorage`
- [ ] All form inputs have leading icons via `.field-icon-wrap`; focus ring uses `var(--primary)` at 15% opacity
- [ ] Stat cards on dashboard animate counter values on first load (`data-count` + `countUp()`)
- [ ] Submit buttons enter `.btn-loading` spinner state on form submit
- [ ] `.badge-soft` classes used for all status and role indicators
- [ ] No `margin-left` / `padding-right` in shared CSS — logical properties used throughout
- [ ] Fonts load as Inter (LTR) and Cairo (RTL) with no visible flash (FOUC script in `<head>`)

---

## 📌 SPEC KIT WORKFLOW REFERENCE

This plan follows the **GitHub Spec Kit** Spec-Driven Development process:

1. ✅ **Constitution** — Project DNA defined above (stack, principles, naming, forbidden patterns)
2. ✅ **Specify** — Full specification: users, features F1-F8, DB schema, JSON structure
3. ✅ **Plan** — Technical plan: solution structure, patterns, generic DataTable API design
4. ✅ **Tasks** — Phase breakdown with numbered tasks and acceptance criteria per phase
5. ⏳ **Implement** — Execute phase by phase; validate acceptance criteria before next phase

> **The spec is the source of truth. Code is its expression.**  
> Any change to requirements means updating this plan first, then regenerating the affected phase.

---

*Generated: March 2026 | Template: ASP.NET Core 8 + Clean Architecture + Dynamic Permission System*