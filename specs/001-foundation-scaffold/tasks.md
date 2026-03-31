---
description: "Task list for Feature 001 â€” Foundation & Project Scaffold"
---

# Tasks: Foundation & Project Scaffold

**Input**: Design documents from `specs/001-foundation-scaffold/`
**Prerequisites**: plan.md âœ… Â· spec.md âœ… Â· research.md âœ… Â· data-model.md âœ… Â· contracts/internal-contracts.md âœ… Â· quickstart.md âœ…

**Tests**: No automated test tasks â€” Phase 1 uses manual smoke tests per `quickstart.md`.

**Organization**: Tasks are grouped by user story to enable independent verification at each checkpoint.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no shared dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1â€“US4, maps to spec.md priorities P1â€“P4)
- Exact file paths are included in all descriptions

---

## Phase 1: Setup (Solution & Project Scaffold)

**Purpose**: Create the .NET solution, four projects, project references, and NuGet packages.
No code is written here â€” this is pure project initialization.

- [X] T001 Create `AdminTemplate.sln` blank solution and four projects at the solution root: `AdminTemplate.Domain` (Class Library .NET 8), `AdminTemplate.Application` (Class Library .NET 8), `AdminTemplate.Infrastructure` (Class Library .NET 8), `AdminTemplate.Web` (ASP.NET Core Web App MVC .NET 8)
- [X] T002 Add project references per clean architecture direction: Application â†’ Domain; Infrastructure â†’ Domain + Application; Web â†’ Application + Infrastructure (verify no reverse references exist)
- [X] T003 [P] Install NuGet packages in `AdminTemplate.Infrastructure`: `Microsoft.EntityFrameworkCore.SqlServer` 8.*, `Microsoft.EntityFrameworkCore.Tools` 8.*, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.*
- [X] T004 [P] Install NuGet package in `AdminTemplate.Web`: `Microsoft.EntityFrameworkCore.Design` 8.*
- [X] T005 [P] Install NuGet package in `AdminTemplate.Application`: `Microsoft.Extensions.Identity.Core` 8.*

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Configuration files and the Web composition root (`Program.cs`). These must be in place before any user story can be independently verified. Also covers **US4** (Identity security configuration) since it is pure `Program.cs` configuration with no separate files.

**âš ï¸ CRITICAL**: No user story implementation work can begin until this phase is complete â€” `Program.cs` must compile for the solution to build.

- [X] T006 [P] Create `AdminTemplate.Web/appsettings.json` with `ConnectionStrings:DefaultConnection` (LocalDB), `Identity:Lockout` (MaxFailedAccessAttempts: 5, DefaultLockoutTimeSpanMinutes: 15), and `Seed:SuperAdminEmail` / `Seed:SuperAdminPassword` sections
- [X] T007 [P] Create `AdminTemplate.Web/appsettings.Development.json` with `Logging:LogLevel` overrides for development verbosity
- [X] T008 Create `AdminTemplate.Web/Program.cs` with: MVC services registration (`AddControllersWithViews`), ASP.NET Core Identity registration with `ApplicationUser` and `ApplicationRole` and password options (RequireUppercase, RequireDigit, RequireNonAlphanumeric, MinLength 8) and lockout options (read from config), global authorization fallback policy (`RequireAuthenticatedUser()`), `UseAuthentication()` + `UseAuthorization()` middleware, and a placeholder `MapControllerRoute` default route

**Checkpoint**: `Program.cs` compiles. `appsettings.json` is in place. Identity policy (US4) is configured. Foundation ready for user story layers.

---

## Phase 3: User Story 1 â€” Solution Builds and Runs (Priority: P1) ðŸŽ¯ MVP

**Goal**: All four layers have their types defined. The solution builds with 0 errors and 0 warnings and starts without exceptions.

**Independent Test**: Build the solution from a clean state â€” verify 0 errors and 0 warnings. Start the Web project â€” verify it responds to HTTP requests and redirects unauthenticated requests to login.

### Domain Layer â€” Entities and Interfaces

- [X] T009 [P] [US1] Create `AdminTemplate.Domain/Common/BaseEntity.cs` â€” abstract class with `Guid Id` (initialized to `Guid.NewGuid()`) and `DateTime CreatedAt` (initialized to `DateTime.UtcNow`) properties
- [X] T010 [P] [US1] Create `AdminTemplate.Domain/Entities/ApplicationUser.cs` â€” extends `IdentityUser<string>` with `string FullName` (required, max 200), `bool IsActive` (default `true`), `DateTime CreatedAt` properties
- [X] T011 [P] [US1] Create `AdminTemplate.Domain/Entities/ApplicationRole.cs` â€” extends `IdentityRole<string>` with `string? Description` (max 500) and `DateTime CreatedAt` properties
- [X] T012 [US1] Create `AdminTemplate.Domain/Entities/RolePermission.cs` â€” inherits `BaseEntity`; adds `string RoleId`, `string ObjectName` (max 100), `string FunctionName` (max 100) properties (depends on T009)
- [X] T013 [P] [US1] Create `AdminTemplate.Domain/Interfaces/IUserRepository.cs` â€” interface with `GetByIdAsync`, `GetByEmailAsync`, `GetAllAsync`, `GetPagedAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` per contracts/internal-contracts.md
- [X] T014 [P] [US1] Create `AdminTemplate.Domain/Interfaces/IRoleRepository.cs` â€” interface with `GetByIdAsync`, `GetByNameAsync`, `GetAllAsync`, `GetPagedAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `HasUsersAsync` per contracts/internal-contracts.md
- [X] T015 [P] [US1] Create `AdminTemplate.Domain/Interfaces/IPermissionRepository.cs` â€” interface with `GetByRoleIdAsync`, `ExistsAsync`, `AddAsync`, `AddRangeAsync`, `DeleteByRoleIdAsync`, `DeleteAsync` per contracts/internal-contracts.md

### Application Layer â€” DTOs, Interfaces, Service Stubs

- [X] T016 [P] [US1] Create DTO records in `AdminTemplate.Application/DTOs/`: `PermissionObjectDto.cs` (Name, DisplayName, Functions), `PermissionDto.cs` (ObjectName, FunctionName), `UserDto.cs` (Id, FullName, Email, IsActive, Roles, CreatedAt), `RoleDto.cs` (Id, Name, Description, UserCount, PermissionCount, CreatedAt) â€” all as C# `record` types per contracts/internal-contracts.md
- [X] T017 [P] [US1] Create `AdminTemplate.Application/Providers/IPermissionProvider.cs` â€” interface with `IReadOnlyList<PermissionObjectDto> GetAll()` per contracts/internal-contracts.md
- [X] T018 [P] [US1] Create service interfaces in `AdminTemplate.Application/Interfaces/`: `IUserService.cs`, `IRoleService.cs`, `IPermissionService.cs` â€” full signatures per contracts/internal-contracts.md (stubs only for methods not needed in this phase)
- [X] T019 [P] [US1] Create service stubs in `AdminTemplate.Application/Services/`: `UserService.cs`, `RoleService.cs`, `PermissionService.cs` â€” implement interfaces, throw `NotImplementedException` for all methods (will be fleshed out in Phases 6â€“8)
- [X] T020 [US1] Build the solution and verify it compiles with 0 errors and 0 warnings; fix any compilation issues before proceeding (depends on T009â€“T019)

**Checkpoint**: Solution builds. All four layers are present with their types. Running the Web project serves HTTP requests. US1 acceptance criteria met.

---

## Phase 4: User Story 2 â€” Database Created via Migration (Priority: P2)

**Goal**: `ApplicationDbContext` is configured with Identity + `RolePermissions` table. The initial migration is created and can be applied against a SQL Server instance.

**Independent Test**: Run `Add-Migration InitialCreate` then `Update-Database` in PM Console. Inspect the database to verify `AspNetUsers` has `FullName`, `IsActive`, `CreatedAt`; `AspNetRoles` has `Description`, `CreatedAt`; `RolePermissions` table exists with Guid PK, FK to `AspNetRoles`, unique constraint.

- [X] T021 [US2] Create `AdminTemplate.Infrastructure/Data/ApplicationDbContext.cs` â€” inherits `IdentityDbContext<ApplicationUser, ApplicationRole, string>`; adds `DbSet<RolePermission> RolePermissions`; overrides `OnModelCreating` to call `base.OnModelCreating` then configure `RolePermission` via Fluent API (depends on T010, T011, T012)
- [X] T022 [US2] Add Fluent API configuration for `RolePermission` in `ApplicationDbContext.OnModelCreating`: `HasKey(rp => rp.Id)`, `HasOne<ApplicationRole>().WithMany().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade)`, `HasIndex(rp => new { rp.RoleId, rp.ObjectName, rp.FunctionName }).IsUnique()`, column max-length constraints for `ObjectName` (100) and `FunctionName` (100) (depends on T021)
- [X] T023 [US2] Create `AdminTemplate.Infrastructure/Extensions/InfrastructureServiceExtensions.cs` â€” static extension method `AddInfrastructure(this IServiceCollection services, IConfiguration configuration)` that registers `ApplicationDbContext` with `UseSqlServer(configuration.GetConnectionString("DefaultConnection"))` (depends on T021)
- [X] T024 [US2] Call `builder.Services.AddInfrastructure(builder.Configuration)` in `AdminTemplate.Web/Program.cs` and remove any placeholder DbContext registration added in T008 (depends on T023)
- [X] T025 [US2] Create the initial EF Core migration by running in PM Console: `Add-Migration InitialCreate -Project AdminTemplate.Infrastructure -StartupProject AdminTemplate.Web` â€” verify Migrations folder appears with `InitialCreate` migration files

**Checkpoint**: Migration exists. `Update-Database` creates the database with all expected tables and columns. US2 acceptance criteria met.

---

## Phase 5: User Story 3 â€” SuperAdmin Seed Login (Priority: P3)

**Goal**: `permissions.json` is created, `IPermissionProvider` is implemented, all three repositories are implemented, and the `DataSeeder` idempotently seeds SuperAdmin user + role + all 16 permission rows on startup.

**Independent Test**: Apply the migration against a fresh database, start the application, then verify: `AspNetUsers` has one row (superadmin@admintemplate.local), `AspNetRoles` has one row (SuperAdmin), `RolePermissions` has 16 rows. Restart the application â€” row counts remain the same (idempotency). Log in with seed credentials â€” authentication succeeds.

- [X] T026 [P] [US3] Create `AdminTemplate.Web/Config/permissions.json` with Employee (5 functions), User (4 functions), Role (5 functions), Report (2 functions) objects per data-model.md; set file property "Copy to Output Directory: Copy if newer"
- [X] T027 [P] [US3] Create `AdminTemplate.Infrastructure/Providers/JsonPermissionProvider.cs` implementing `IPermissionProvider` â€” reads `permissions.json` via `IConfiguration` or `IWebHostEnvironment`-resolved path, deserializes into `IReadOnlyList<PermissionObjectDto>`, caches result as singleton (depends on T017)
- [X] T028 [P] [US3] Create `AdminTemplate.Infrastructure/Repositories/UserRepository.cs` implementing `IUserRepository` â€” constructor-inject `UserManager<ApplicationUser>`; implement `GetByEmailAsync` (uses `FindByEmailAsync`), `AddAsync` (uses `CreateAsync` with password hashing), all other methods; stub remaining methods with `NotImplementedException` for now (depends on T013, T021)
- [X] T029 [P] [US3] Create `AdminTemplate.Infrastructure/Repositories/RoleRepository.cs` implementing `IRoleRepository` â€” constructor-inject `RoleManager<ApplicationRole>`; implement `GetByNameAsync` (uses `FindByNameAsync`), `AddAsync` (uses `CreateAsync`), `HasUsersAsync` (queries `UserManager.GetUsersInRoleAsync`); stub remaining methods (depends on T014, T021)
- [X] T030 [P] [US3] Create `AdminTemplate.Infrastructure/Repositories/PermissionRepository.cs` implementing `IPermissionRepository` â€” constructor-inject `ApplicationDbContext`; implement `GetByRoleIdAsync`, `ExistsAsync`, `AddAsync`, `AddRangeAsync`, `DeleteByRoleIdAsync`, `DeleteAsync` (all required for seeder per contracts/internal-contracts.md) (depends on T015, T021)
- [X] T031 [US3] Create `AdminTemplate.Infrastructure/Seed/DataSeeder.cs` â€” constructor-inject `IUserRepository`, `IRoleRepository`, `IPermissionRepository`, `IPermissionProvider`, `IConfiguration`; implement `SeedAsync()` that: (1) checks if SuperAdmin role exists by name â†’ creates if not, (2) checks if SuperAdmin user exists by email â†’ creates if not and assigns role, (3) for each permission object+function from `IPermissionProvider.GetAll()` checks `ExistsAsync` â†’ calls `AddAsync` if not found (idempotent); reads seed credentials from `IConfiguration["Seed:SuperAdminEmail"]` and `IConfiguration["Seed:SuperAdminPassword"]` (depends on T027â€“T030)
- [X] T032 [US3] Register in `InfrastructureServiceExtensions.AddInfrastructure`: `IPermissionRepository` â†’ `PermissionRepository` (Scoped), `IUserRepository` â†’ `UserRepository` (Scoped), `IRoleRepository` â†’ `RoleRepository` (Scoped), `IPermissionProvider` â†’ `JsonPermissionProvider` (Singleton), `DataSeeder` (Scoped); also register `UserService`, `RoleService`, `PermissionService` service stubs (Scoped) (depends on T023, T027â€“T031)
- [X] T033 [US3] Add seeder invocation in `AdminTemplate.Web/Program.cs` after `app.Build()` and before `app.Run()`: create `IServiceScope`, resolve `DataSeeder`, call `await seeder.SeedAsync()` (depends on T031, T032)

**Checkpoint**: Application seeds SuperAdmin on first start. 16 rows in `RolePermissions`. Idempotent on restart. Login with seed credentials succeeds. US3 acceptance criteria met.

---

## Phase N: Polish & Validation

**Purpose**: Validate full smoke test checklist from `quickstart.md`, confirm US4 Identity security configuration is enforced, and verify all acceptance criteria.

- [ ] T034 Apply the migration against a freshly created database: `Update-Database -Project AdminTemplate.Infrastructure -StartupProject AdminTemplate.Web`
- [ ] T035 [P] Run all smoke test items from `quickstart.md` Section 10 and confirm all 7 checkboxes pass: solution builds 0 errors, migration succeeds, SuperAdmin seeded, SuperAdmin role seeded, 16 permission rows, app starts, restart is idempotent
- [ ] T036 [P] Verify US4 Identity security rules: attempt to create a user with a short password via `UserManager` and confirm `IdentityResult` returns an error; confirm lockout is configured with `MaxFailedAccessAttempts: 5` in `appsettings.json` and bound to Identity options in `Program.cs`
- [ ] T037 [P] Verify the global authorization fallback policy: navigate to any admin route without being authenticated and confirm a redirect to login (not a 200) occurs; confirm `[AllowAnonymous]` routes (if any) are accessible without auth

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup â€” T001â€“T005)
    â””â”€â”€ Phase 2 (Foundational â€” T006â€“T008)
            â””â”€â”€ Phase 3 (US1 â€” T009â€“T020)
                    â””â”€â”€ Phase 4 (US2 â€” T021â€“T025)
                            â””â”€â”€ Phase 5 (US3 â€” T026â€“T033)
                                    â””â”€â”€ Phase N (Polish â€” T034â€“T037)
```

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational phase (Phase 2). No dependency on other user stories.
- **US2 (P2)**: Depends on US1 â€” needs `ApplicationUser`, `ApplicationRole`, `RolePermission` entities to define `ApplicationDbContext`.
- **US3 (P3)**: Depends on US2 â€” needs the migration applied and DbContext registered to run the seeder against the database.
- **US4 (P4)**: Implemented in Phase 2 (Foundational) as Identity options in `Program.cs`. Validated in Phase N.

### Within Each Phase

- All tasks marked **[P]** within the same phase can be worked on simultaneously (different files, no inter-task dependency).
- Tasks without **[P]** depend on prior tasks within the same phase.
- Phase 3 models (T009â€“T012) must complete before Phase 3 service stubs (T019) that reference them â€” but T009, T010, T011 are [P] with each other.
- Phase 5 repositories (T028â€“T030) are all [P] with each other â€” different files, same DbContext dependency already satisfied.

### Parallel Opportunities

**Phase 1**: T003, T004, T005 are all [P] â€” NuGet installs targeting different projects.

**Phase 2**: T006 and T007 are [P] â€” separate `appsettings` files.

**Phase 3**:
- Group A (parallel): T009, T010, T011, T013, T014, T015, T016, T017, T018
- Then: T012 (depends on T009), T019 (depends on interfaces from T018)
- Then: T020 (build validation)

**Phase 5**:
- T026, T027, T028, T029, T030 are [P] â€” independent files
- Then: T031 (depends on T027â€“T030)
- Then: T032, T033 (depends on T031)

---

## Parallel Execution Example: Phase 3 (US1)

```
Start simultaneously:
  â†’ T009  AdminTemplate.Domain/Common/BaseEntity.cs
  â†’ T010  AdminTemplate.Domain/Entities/ApplicationUser.cs
  â†’ T011  AdminTemplate.Domain/Entities/ApplicationRole.cs
  â†’ T013  AdminTemplate.Domain/Interfaces/IUserRepository.cs
  â†’ T014  AdminTemplate.Domain/Interfaces/IRoleRepository.cs
  â†’ T015  AdminTemplate.Domain/Interfaces/IPermissionRepository.cs
  â†’ T016  AdminTemplate.Application/DTOs/*.cs
  â†’ T017  AdminTemplate.Application/Providers/IPermissionProvider.cs
  â†’ T018  AdminTemplate.Application/Interfaces/*.cs

After T009 is done:
  â†’ T012  AdminTemplate.Domain/Entities/RolePermission.cs

After T018 is done:
  â†’ T019  AdminTemplate.Application/Services/*.cs (stubs)

After all T009â€“T019:
  â†’ T020  Build validation (0 errors required)
```

## Parallel Execution Example: Phase 5 (US3)

```
Start simultaneously:
  â†’ T026  AdminTemplate.Web/Config/permissions.json
  â†’ T027  AdminTemplate.Infrastructure/Providers/JsonPermissionProvider.cs
  â†’ T028  AdminTemplate.Infrastructure/Repositories/UserRepository.cs
  â†’ T029  AdminTemplate.Infrastructure/Repositories/RoleRepository.cs
  â†’ T030  AdminTemplate.Infrastructure/Repositories/PermissionRepository.cs

After T026â€“T030:
  â†’ T031  AdminTemplate.Infrastructure/Seed/DataSeeder.cs

After T031:
  â†’ T032  InfrastructureServiceExtensions.cs (register all)
  â†’ T033  Program.cs (seeder invocation)
```

---

## Implementation Strategy

**MVP scope**: Complete all phases in order â€” Phase 1 through Phase 5 is the minimum to achieve the Phase 1 overall acceptance criteria ("App runs, DB created, can login with seeded SuperAdmin").

**Delivery order** (single implementer, sequential):
1. Phase 1 â€” ~15 minutes (project creation)
2. Phase 2 â€” ~20 minutes (Program.cs + config)
3. Phase 3 â€” ~40 minutes (Domain + Application layer)
4. Phase 4 â€” ~30 minutes (DbContext + migration)
5. Phase 5 â€” ~60 minutes (repositories + seeder + wiring)
6. Phase N â€” ~15 minutes (smoke test validation)

**No optional tasks**: All tasks are required for Phase 1 acceptance criteria to be met.

