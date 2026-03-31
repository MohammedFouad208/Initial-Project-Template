# Quickstart: Foundation & Project Scaffold

**Branch**: `001-foundation-scaffold`
**Date**: 2026-03-29
**Prerequisite**: Visual Studio 2022 17.8+ (for .NET 8 support), SQL Server 2019+ or SQL Server LocalDB, NuGet Package Manager Console

---

## 1. Create the Solution

In Visual Studio → File → New → Blank Solution → name it `AdminTemplate`.

Then add four Class Library or ASP.NET Core projects:

```
# In Package Manager Console (Tools → NuGet Package Manager → Package Manager Console)
# Or use Visual Studio's "Add New Project" UI

# The solution file sits at the root. Four projects are added at the same level.
```

**Project types to create**:

| Project Name | Template |
|---|---|
| `AdminTemplate.Domain` | Class Library (.NET 8) |
| `AdminTemplate.Application` | Class Library (.NET 8) |
| `AdminTemplate.Infrastructure` | Class Library (.NET 8) |
| `AdminTemplate.Web` | ASP.NET Core Web App (MVC) (.NET 8) |

---

## 2. Add Project References

In Visual Studio Solution Explorer, right-click each project → Add → Project Reference:

| Project | References |
|---|---|
| `AdminTemplate.Application` | `AdminTemplate.Domain` |
| `AdminTemplate.Infrastructure` | `AdminTemplate.Domain`, `AdminTemplate.Application` |
| `AdminTemplate.Web` | `AdminTemplate.Application`, `AdminTemplate.Infrastructure` |

**Verify** no reverse reference exists (Domain must reference nothing; Application must not reference Infrastructure or Web).

---

## 3. Install NuGet Packages

Open **Package Manager Console** (`Tools → NuGet Package Manager → Package Manager Console`).

```powershell
# Infrastructure project
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 8.* -Project AdminTemplate.Infrastructure
Install-Package Microsoft.EntityFrameworkCore.Tools -Version 8.* -Project AdminTemplate.Infrastructure
Install-Package Microsoft.AspNetCore.Identity.EntityFrameworkCore -Version 8.* -Project AdminTemplate.Infrastructure

# Application project
Install-Package Microsoft.Extensions.Identity.Core -Version 8.* -Project AdminTemplate.Application

# Web project (startup)
Install-Package Microsoft.EntityFrameworkCore.Design -Version 8.* -Project AdminTemplate.Web
```

> **Note**: `Microsoft.EntityFrameworkCore.Design` must be installed in the startup project (Web) so that PM Console can resolve the design-time `DbContext` factory when generating migrations.

---

## 4. Configure the Connection String

In `AdminTemplate.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AdminTemplateDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Identity": {
    "Lockout": {
      "MaxFailedAccessAttempts": 5,
      "DefaultLockoutTimeSpanMinutes": 15
    }
  },
  "Seed": {
    "SuperAdminEmail": "superadmin@admintemplate.local",
    "SuperAdminPassword": "Admin@1234!"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Replace the connection string with your SQL Server instance if not using LocalDB.

---

## 5. Add the permissions.json File

Create `AdminTemplate.Web/Config/permissions.json`:

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

Ensure the file is set to **Copy to Output Directory: Copy if newer** in its properties.

---

## 6. Create the Database Migration

In **Package Manager Console**:

```powershell
# Set the default project to Infrastructure (where DbContext lives)
# Set startup project to Web (where appsettings.json lives)
Add-Migration InitialCreate -Project AdminTemplate.Infrastructure -StartupProject AdminTemplate.Web
```

Verify that a `Migrations/` folder appears inside `AdminTemplate.Infrastructure` with the generated files.

---

## 7. Apply the Migration

```powershell
Update-Database -Project AdminTemplate.Infrastructure -StartupProject AdminTemplate.Web
```

Expected output: `Done.` — the database is created with all Identity tables plus `RolePermissions`.

---

## 8. Run the Application

Press **F5** (or `Ctrl+F5`) in Visual Studio with `AdminTemplate.Web` set as the startup project.

On first startup, the `DataSeeder` runs automatically and:
1. Creates the **SuperAdmin** role (if not present)
2. Creates the **superadmin@admintemplate.local** user (if not present)
3. Assigns the SuperAdmin role to the user
4. Seeds all 16 permission rows from `permissions.json` to the SuperAdmin role

You should be redirected to the login page (or a placeholder home page if auth pages are not yet implemented in this phase).

---

## 9. Verify the Seed

Connect to your SQL Server database and run:

```sql
SELECT u.Email, u.FullName, u.IsActive, r.Name AS RoleName
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id;

SELECT COUNT(*) AS PermissionCount FROM RolePermissions;
-- Expected: 16
```

---

## 10. Smoke Test Checklist

- [ ] Solution builds with 0 errors and 0 warnings
- [ ] `Update-Database` completes successfully
- [ ] `AspNetUsers` table contains the SuperAdmin user
- [ ] `AspNetRoles` table contains the SuperAdmin role
- [ ] `RolePermissions` table contains 16 rows
- [ ] Application starts without exceptions
- [ ] Running the application a second time does not duplicate seed data

---

## Troubleshooting

| Problem | Solution |
|---|---|
| `No DbContext was found` during migration | Ensure `Microsoft.EntityFrameworkCore.Design` is installed in `AdminTemplate.Web` and `-StartupProject AdminTemplate.Web` is specified |
| Connection string error | Verify LocalDB is installed (`sqllocaldb info`) or update the connection string to your SQL Server instance |
| `permissions.json` not found at startup | Check that the file is in `AdminTemplate.Web/Config/` and "Copy to Output Directory" is set |
| Duplicate seed data | The seeder should be idempotent — check that `DataSeeder` checks for existing users/roles before creating |
| Login fails after seeding | Verify the `Seed:SuperAdminPassword` in `appsettings.json` meets the Identity password policy (8+ chars, uppercase, digit, special) |
