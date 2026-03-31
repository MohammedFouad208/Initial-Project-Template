# 🏗️ ASP.NET Core Clean Architecture Admin Template
## Spec-Driven Development Plan (Spec Kit Methodology)

> **Primary Color:** `#004D82`  
> **Methodology:** Spec-Driven Development (SDD) — Constitution → Specify → Plan → Phases → Implement  
> **Goal:** A production-ready, reusable ASP.NET Core MVC admin template with clean architecture, dynamic role/permission management, and a modern Bootstrap dashboard.

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
| Icons | Font Awesome | 6.x |
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
- ❌ No inline styles — use CSS variables anchored to `#004D82` primary
- ❌ No hardcoded roles, objects, or functions — always read from `permissions.json`
- ❌ No jQuery DataTable initialization duplicated per page — always use the generic `datatable.js`

### Security Constraints
- All admin routes protected by `[Authorize]` attribute
- Permission checks via a custom `[HasPermission("Employee", "Create")]` attribute
- CSRF tokens on all POST forms
- Identity passwords: min 8 chars, uppercase, digit, special char

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
- **Color:** `#004D82` as CSS `--primary-color`
- Collapsible sidebar with navigation menu
- Top navbar: app name, notification bell icon, user avatar dropdown (profile, logout)
- Dashboard home: stats cards (users count, roles count, active sessions)
- Responsive — mobile sidebar collapses to hamburger menu

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
**Goal:** Complete auth flow with branded, modern UI.

| # | Task | Deliverable |
|---|---|---|
| 2.1 | Create `AccountController` with Login, Register, ForgotPassword, ResetPassword actions | Controller |
| 2.2 | Design Login page: centered card, logo, email+password, Remember Me, Forgot link | `Login.cshtml` |
| 2.3 | Design Register page: full name, email, password, confirm password | `Register.cshtml` |
| 2.4 | Design Forgot Password page + Reset Password page | Both views |
| 2.5 | Configure email sender (SMTP stub/interface) for reset link | `IEmailSender` impl |
| 2.6 | Style all auth pages with `#004D82` primary, Bootstrap 5, no sidebar | Auth layout |
| 2.7 | Implement lockout feedback on Login page | UX message |

**Acceptance Criteria:** Full auth flow works end to end. Reset email link lands on correct page.

---

### PHASE 3 — Admin Layout & Dashboard
**Goal:** Modern admin shell that all future pages will use.

| # | Task | Deliverable |
|---|---|---|
| 3.1 | Create `_Layout.cshtml` with sidebar + topnav structure | Shared layout |
| 3.2 | Build collapsible sidebar: logo, nav items with icons, active state | `_Sidebar.cshtml` |
| 3.3 | Build topnav: hamburger toggle, notification bell (badge), user avatar dropdown | `_Topnav.cshtml` |
| 3.4 | CSS: define `--primary-color: #004D82`, sidebar gradient, hover states | `site.css` |
| 3.5 | Dashboard home page: stat cards (Total Users, Total Roles, Active Sessions) | `Dashboard/Index.cshtml` |
| 3.6 | Make layout responsive: sidebar collapses on mobile | CSS + JS |
| 3.7 | Logout action with confirmation | Topnav → AccountController |

**Acceptance Criteria:** Admin layout renders correctly on desktop and mobile. Sidebar collapses. Logout works.

---

### PHASE 4 — Permission Engine
**Goal:** JSON-driven permission system with custom authorization attribute.

| # | Task | Deliverable |
|---|---|---|
| 4.1 | Create `permissions.json` in `/Config/` with Employee, User, Role, Report objects | JSON file |
| 4.2 | Implement `IPermissionProvider` + `JsonPermissionProvider` reading the JSON | Provider class |
| 4.3 | Create `RolePermission` entity + repository + service | CRUD service |
| 4.4 | Implement `IPermissionService.UserHasPermission(userId, object, function)` | Service method |
| 4.5 | Build `HasPermissionAttribute` (IAsyncAuthorizationFilter) | Custom filter attribute |
| 4.6 | Register permission services in DI | `Program.cs` |
| 4.7 | Return custom 403 view when permission denied | `Views/Shared/403.cshtml` |
| 4.8 | Expose `PermissionContext` to Razor views via Tag Helper or ViewBag helper | Permission-aware views |

**Acceptance Criteria:** Applying `[HasPermission("Employee","Create")]` to an action blocks unauthorized users with 403.

---

### PHASE 5 — Generic DataTable Solution
**Goal:** Reusable JS file used by all management pages; permission-aware buttons.

| # | Task | Deliverable |
|---|---|---|
| 5.1 | Create `AppDataTable` JS object in `/wwwroot/js/datatable.js` | JS file |
| 5.2 | Config API: `init({ tableId, ajaxUrl, columns, permissions, createUrl, editUrl, deleteUrl })` | API design |
| 5.3 | Render toolbar: Create button (hidden if !canCreate) | Toolbar rendering |
| 5.4 | Render action column: Edit/Delete per row (hidden if !canUpdate/!canDelete) | Column rendering |
| 5.5 | Server-side: pass `draw`, `start`, `length`, `search`, `order` to controller | AJAX params |
| 5.6 | Controller base method: `GetDataTableResponse<T>(IQueryable<T>, request)` generic helper | `DataTableHelper.cs` |
| 5.7 | Delete confirmation dialog with object name | SweetAlert or native confirm |
| 5.8 | CSRF token injection on delete POST | Security |
| 5.9 | Loading state, empty state, error state handling | UX polish |

**Acceptance Criteria:** Any page can initialize a DataTable with 5 lines of JS. Buttons hide/show per permission.

---

### PHASE 6 — User Management
**Goal:** Full CRUD for users with role assignment.

| # | Task | Deliverable |
|---|---|---|
| 6.1 | `UsersController.Index` → view with DataTable | `Users/Index.cshtml` |
| 6.2 | `UsersController.GetData` → server-side DataTable endpoint (JSON) | API action |
| 6.3 | Create User page: name, email, password, role multi-select, active toggle | `Users/Create.cshtml` |
| 6.4 | Edit User page: same fields, no password (separate change password) | `Users/Edit.cshtml` |
| 6.5 | Activate/Deactivate (toggle `IsActive`) | Action + UI |
| 6.6 | `UserService`: CreateAsync, UpdateAsync, DeactivateAsync, GetPagedAsync | Service layer |
| 6.7 | Apply `[HasPermission("User", "Browse/Create/Update/Delete")]` | Attributes on actions |

**Acceptance Criteria:** Users list loads via DataTable. CRUD operations work. Role assignment saves correctly.

---

### PHASE 7 — Role Management
**Goal:** Full CRUD for roles.

| # | Task | Deliverable |
|---|---|---|
| 7.1 | `RolesController.Index` → DataTable with: Name, Description, User Count, Permission Count | `Roles/Index.cshtml` |
| 7.2 | `RolesController.GetData` → server-side JSON | API action |
| 7.3 | Create/Edit Role page: name, description | Views |
| 7.4 | Delete Role guard: reject if users assigned | Service validation |
| 7.5 | `RoleService`: CreateAsync, UpdateAsync, DeleteAsync, GetPagedAsync | Service layer |
| 7.6 | Apply permission attributes | Security |

**Acceptance Criteria:** Roles CRUD works. Deleting a role with users shows friendly error.

---

### PHASE 8 — Role-Permission Management
**Goal:** Matrix UI to assign permissions to roles.

| # | Task | Deliverable |
|---|---|---|
| 8.1 | `RolePermissionsController.Index(roleId)` — load matrix for a role | Controller action |
| 8.2 | Build permission matrix view: Rows = Objects, Columns = Functions, Cells = Checkboxes | `RolePermissions/Index.cshtml` |
| 8.3 | "Check All" per row (all functions for one object) | JS UX |
| 8.4 | Save: POST batch of checked permissions → replace all for that role | POST action |
| 8.5 | `PermissionService.SaveRolePermissions(roleId, List<PermissionDto>)` | Service method |
| 8.6 | Link from Roles list: "Manage Permissions" button → opens matrix | Navigation |

**Acceptance Criteria:** SuperAdmin can define exactly which objects/functions Role X can access. Saves persist correctly.

---

### PHASE 9 — Polish, Guards & Seed Data
**Goal:** Production-ready hardening.

| # | Task | Deliverable |
|---|---|---|
| 9.1 | Sidebar menu items visibility driven by permissions (hide items user can't access) | `_Sidebar.cshtml` |
| 9.2 | 403 and 404 custom error pages, styled with admin layout | Error views |
| 9.3 | Add `[ValidateAntiForgeryToken]` to all POST actions | Security audit |
| 9.4 | Comprehensive seed: 3 demo roles (SuperAdmin, Admin, Viewer) with permission sets | `DataSeeder.cs` |
| 9.5 | `README.md`: how to clone, configure connection string, run migrations, seed, start | Documentation |
| 9.6 | `appsettings.json` template: connection string placeholder, SMTP config, lockout settings | Config file |
| 9.7 | Basic client-side form validation (Bootstrap 5 + jQuery Validate) | All forms |
| 9.8 | Toast notifications for success/error actions | UX |

**Acceptance Criteria:** App fully secured, no raw errors exposed, ready to clone and extend.

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

- [ ] Solution builds with 0 errors and 0 warnings
- [ ] DB migrates cleanly from scratch via PM Console
- [ ] SuperAdmin seed user can log in and access all features
- [ ] A role with only "Employee → Browse" permission cannot access Create/Edit/Delete
- [ ] DataTable loads server-side on Users, Roles pages
- [ ] Create/Edit/Delete buttons hidden automatically based on permissions
- [ ] Permission matrix saves correctly and takes effect immediately on next request
- [ ] Sidebar is responsive and collapses on mobile
- [ ] All POST actions protected with CSRF tokens
- [ ] Custom 403 page shown for unauthorized access attempts

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