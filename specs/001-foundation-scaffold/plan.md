# Implementation Plan: Foundation & Project Scaffold

**Branch**: `001-foundation-scaffold` | **Date**: 2026-03-29 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-foundation-scaffold/spec.md`

## Summary

Create the clean architecture solution skeleton for the ASP.NET Core MVC admin template. This phase delivers a 4-project solution (Domain, Application, Infrastructure, Web), an EF Core database context backed by SQL Server, ASP.NET Core Identity configured with security policies, a `RolePermissions` table, an initial migration, and an idempotent data seeder that provisions a SuperAdmin user with full permissions derived from `permissions.json`. No UI pages are delivered; the result is a runnable, authenticated, database-backed foundation that all subsequent phases build on.

## Technical Context

**Language/Version**: C# 12 / .NET 8
**Primary Dependencies**: ASP.NET Core MVC 8, ASP.NET Core Identity (built-in), Entity Framework Core 8 (SQL Server provider), Microsoft.Extensions.DependencyInjection (built-in)
**Storage**: SQL Server 2019+ (configurable via connection string; LocalDB supported for development)
**Testing**: Manual smoke tests per acceptance criteria; no automated test project in Phase 1 (added in a later phase)
**Target Platform**: Windows / Linux server — ASP.NET Core 8 cross-platform web application
**Project Type**: Web application (ASP.NET Core MVC) with clean architecture multi-project solution
**Performance Goals**: Startup must complete in < 10 seconds on developer hardware; migration must complete in < 30 seconds
**Constraints**: 4 projects maximum per clean architecture mandate; no `dotnet ef` CLI — PM Console only
**Scale/Scope**: Single-tenant admin template; initial seed targets a single SuperAdmin account and up to ~4 permission objects × ~5 functions = ~20 permission rows

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Constitution Principle | Status | Notes |
|---|---|---|---|
| I | Clean Architecture — 4-layer separation | ✅ PASS | Solution defines Domain, Application, Infrastructure, Web as separate projects |
| II | Dependency Inversion — dependencies point inward | ✅ PASS | Web → Application ← Infrastructure → Domain; Web references Infrastructure only in Program.cs for DI |
| III | Thin Controllers — no business logic | ✅ PASS | Phase 1 defines no controllers; seeding and DI are infrastructure concerns |
| IV | Repository + Unit of Work — all data access via repositories | ✅ PASS | IUserRepository, IRoleRepository, IPermissionRepository defined in Domain; implementations in Infrastructure |
| V | JSON-Driven Permissions — no hardcoded roles/functions | ✅ PASS | Seeder reads permissions.json at startup; no permission strings are hardcoded in source |
| VI | Generic DataTable Solution | ✅ N/A | No UI in Phase 1; gate applies from Phase 5+ |
| VII | Permission-Aware UI | ✅ N/A | No UI in Phase 1; gate applies from Phase 3+ |
| SEC | CSRF on all POST actions | ✅ N/A | No POST actions in Phase 1; gate applies from Phase 2+ |
| SEC | [Authorize] on all admin routes | ✅ PASS | Global authorization policy configured in Program.cs; a fallback AllowAnonymous applied only to Account routes |
| SEC | Identity password policy enforced | ✅ PASS | Min 8 chars, uppercase, digit, special char configured in Program.cs |
| NMG | Migrations via PM Console only | ✅ PASS | Instructions and quickstart specify Add-Migration / Update-Database only |

**GATE RESULT: ALL CHECKS PASS — Proceed to Phase 0 research.**

## Project Structure

### Documentation (this feature)

```text
specs/001-foundation-scaffold/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (internal service interfaces)
└── tasks.md             # Phase 2 output (speckit.tasks command)
```

### Source Code (repository root)

```text
AdminTemplate.sln

AdminTemplate.Domain/
├── AdminTemplate.Domain.csproj
├── Common/
│   └── BaseEntity.cs
├── Entities/
│   ├── ApplicationUser.cs
│   ├── ApplicationRole.cs
│   └── RolePermission.cs
└── Interfaces/
    ├── IUserRepository.cs
    ├── IRoleRepository.cs
    └── IPermissionRepository.cs

AdminTemplate.Application/
├── AdminTemplate.Application.csproj
├── DTOs/
│   ├── UserDto.cs
│   ├── RoleDto.cs
│   └── PermissionDto.cs
├── Interfaces/
│   ├── IUserService.cs
│   ├── IRoleService.cs
│   └── IPermissionService.cs
├── Providers/
│   └── IPermissionProvider.cs
└── Services/
    ├── UserService.cs
    ├── RoleService.cs
    └── PermissionService.cs

AdminTemplate.Infrastructure/
├── AdminTemplate.Infrastructure.csproj
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Migrations/           ← PM Console managed
├── Extensions/
│   └── InfrastructureServiceExtensions.cs
├── Providers/
│   └── JsonPermissionProvider.cs
├── Repositories/
│   ├── UserRepository.cs
│   ├── RoleRepository.cs
│   └── PermissionRepository.cs
└── Seed/
    └── DataSeeder.cs

AdminTemplate.Web/
├── AdminTemplate.Web.csproj
├── Config/
│   └── permissions.json
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

**Structure Decision**: Option 2 variant — multi-project clean architecture solution. Four separate `.csproj` files in a single `.sln`. No `src/` wrapper folder to keep PM Console target selection simpler. The Web project is the composition root and hosts all startup configuration. Migrations live in Infrastructure so DbContext and model definitions are co-located with their implementation.

## Complexity Tracking

> No constitution violations in this phase — all complexity is mandated by the constitution itself.
