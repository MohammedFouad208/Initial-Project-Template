# Admin Template

A production-ready ASP.NET Core 8 MVC admin panel template with Clean Architecture, ASP.NET Core Identity, and a role-based permission engine.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2019+ (or LocalDB / SQL Express) 
- Visual Studio 2022+ or the `dotnet` CLI

---

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/your-org/admin-template.git
cd admin-template
```

### 2. Configure the connection string

Open `AdminTemplate.Web/appsettings.json` and update `ConnectionStrings:DefaultConnection`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=AdminTemplateDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

For production credentials, use [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) instead of editing `appsettings.json` directly.

---

## Database

Run EF Core migrations from the **Package Manager Console** (in Visual Studio) with the `AdminTemplate.Infrastructure` project as the default:

```powershell
Update-Database
```

Or with the .NET CLI:

```bash
dotnet ef database update --project AdminTemplate.Infrastructure --startup-project AdminTemplate.Web
```

---

## Seed Data

Seed runs automatically the first time the application starts. Three users and their roles are created idempotently:

| Role       | Email                             | Password      | Permissions                     |
|------------|-----------------------------------|---------------|---------------------------------|
| SuperAdmin | superadmin@admintemplate.local    | Admin@1234!   | All permissions on all objects  |
| Admin      | admin@admintemplate.local         | Admin@1234!   | Browse, Create, Update          |
| Viewer     | viewer@admintemplate.local        | Viewer@1234!  | Browse only                     |

> The seed credentials are read from `Seed:*` keys in `appsettings.json`.  
> Override them in user secrets or environment variables before the first app start to set custom passwords.

---

## Run

```bash
dotnet run --project AdminTemplate.Web
```

Then open `https://localhost:5001` (or the port shown in the console). Log in with any seed user from the table above.

---

## Configuration

### SMTP (email sending)

The `Smtp` section in `appsettings.json` configures the outgoing mail provider:

```json
"Smtp": {
  "Host": "smtp.example.com",
  "Port": 587,
  "UseSsl": true,
  "UserName": "your-smtp-username",
  "Password": "your-smtp-password",
  "FromAddress": "noreply@admintemplate.local",
  "FromDisplayName": "Admin Template"
}
```

Store real SMTP credentials in [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) or environment variables — **never commit passwords to source control**.

```bash
# Example: set user secrets for development
dotnet user-secrets set "Smtp:Password" "real-password" --project AdminTemplate.Web
```

### Identity lockout

```json
"Identity": {
  "Lockout": {
    "MaxFailedAccessAttempts": 5,
    "DefaultLockoutTimeSpanMinutes": 15
  }
}
```

---

## Project Structure

```
AdminTemplate.sln
├── AdminTemplate.Domain/          # Entities, interfaces (no external dependencies)
├── AdminTemplate.Application/     # DTOs, service interfaces, business logic
├── AdminTemplate.Infrastructure/  # EF Core, Identity, repositories, seed data
└── AdminTemplate.Web/             # ASP.NET Core MVC (controllers, views, static files)
```
