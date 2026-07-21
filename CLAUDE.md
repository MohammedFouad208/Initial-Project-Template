# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Admin Template** — A production-ready ASP.NET Core 8 MVC admin panel with Clean Architecture, ASP.NET Core Identity, and a role-based permission engine. Supports English and Arabic (RTL) localization.

## Commands

### Run the app
```bash
dotnet run --project AdminTemplate.Web
# Access: https://localhost:5001
```

### Apply EF Core migrations
From Visual Studio Package Manager Console (preferred):
```powershell
Update-Database
```
Or via CLI:
```bash
dotnet ef database update --project AdminTemplate.Infrastructure --startup-project AdminTemplate.Web
```

### Add a new migration
```powershell
Add-Migration MigrationName -Project AdminTemplate.Infrastructure -StartupProject AdminTemplate.Web
```

### Build
```bash
dotnet build
```

There are no automated tests in this project.

## Architecture

The solution follows Clean Architecture with strict layer boundaries:

```
AdminTemplate.Domain        → Entities, interfaces — no external dependencies
AdminTemplate.Application   → DTOs, service interfaces, business logic
AdminTemplate.Infrastructure → EF Core, repositories, Identity, seed data
AdminTemplate.Web           → MVC controllers, views, filters, static files
```

**Dependency rule:** Domain ← Application ← Infrastructure ← Web. Infrastructure and Web both reference Application; nothing references Web.

### Dependency Injection

All registrations flow through `InfrastructureServiceExtensions.AddInfrastructure()` (called from `Program.cs`). Add new services, repositories, and providers there.

### Authorization

Fine-grained permissions use a custom `[HasPermission("Object", "Function")]` attribute (`Web/Filters/HasPermissionAttribute.cs`). Permission objects and their allowed functions are declared in `Web/Config/permissions.json`. The `RolePermissions` table stores which roles have which permissions. When adding a new permission-protected resource, register it in `permissions.json` first.

### DataTables

All server-side tables share a single JS initializer (`wwwroot/js/datatable.js`). Controllers return `DataTableResponse<T>` from `Application/Common/DataTable/`. Action buttons (Edit/Delete) are rendered client-side and conditionally shown based on the user's permissions passed from the server.

### Localization

Strings live in `Web/Resources/SharedResource.resx` (English) and `SharedResource.ar.resx` (Arabic). Inject `IStringLocalizer<SharedResource>` wherever localized strings are needed. RTL layout is applied automatically when Arabic is selected.

### Entity Pattern

- All entities extend `BaseEntity` (`Guid Id` + `DateTime CreatedAt`)
- EF relationships and constraints are configured in `ApplicationDbContext.OnModelCreating()`
- Repositories wrap `UserManager`/`RoleManager` (for Identity) or direct `DbContext` access
- Services in `Application/` call repositories and return DTOs — never expose raw entities to controllers

### Code Generation (SystemAdmin Area)

The `SystemAdmin` area (`Web/Areas/SystemAdmin/`) contains the dynamic entity builder. `EntityBuilderService` and `CodeGeneratorService` use metadata in `EntityDefinition`/`EntityColumn` tables plus Scriban templates in `Web/CodeGenTemplates/` to scaffold CRUD controllers and views at runtime.

## Key Configuration

**Connection string:** `AdminTemplate.Web/appsettings.json` → `ConnectionStrings:DefaultConnection`

**Seed credentials** (read from `appsettings.json` `Seed:*` keys, runs automatically on first start):
| Role | Email | Password |
|------|-------|----------|
| SuperAdmin | superadmin@admintemplate.local | Admin@1234! |
| Admin | admin@admintemplate.local | Admin@1234! |
| Viewer | viewer@admintemplate.local | Viewer@1234! |

**SMTP:** Configure `Smtp:*` keys in appsettings or user secrets. `NoOpEmailSender` is the placeholder used when SMTP is unconfigured.

## Conventions

- **Controller names:** Plural (`UsersController`, `RolesController`)
- **ViewModels:** `{Action}{Entity}ViewModel` (e.g., `CreateUserViewModel`) — kept in `Web/ViewModels/`
- **DTOs:** `{Entity}Dto` / `{Action}{Entity}Dto` — kept in `Application/DTOs/`
- **Interfaces:** `I{Name}` in `Domain/Interfaces/` (repositories) or `Application/Interfaces/` (services)
- All controller actions that modify data require `[ValidateAntiForgeryToken]`
- All admin routes require `[Authorize]`; permission-specific routes add `[HasPermission(...)]`
- Frontend libraries (Bootstrap, DataTables, Font Awesome, jQuery) are loaded from CDN in `_Layout.cshtml`; local copies exist under `wwwroot/lib/` for offline fallback
