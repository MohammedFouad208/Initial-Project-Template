# Implementation Plan: Authentication Pages (Phase 2)

**Branch**: `002-auth-pages` | **Date**: 2026-03-31 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-auth-pages/spec.md`

## Summary

Deliver a complete, branded authentication flow on top of the Phase 1 foundation. This phase creates an `AccountController` in the Web layer backed by a new `IAccountService` in the Application layer, which wraps ASP.NET Core Identity's `SignInManager<ApplicationUser>` and `UserManager<ApplicationUser>`. Four pages are delivered — Login, Register, Forgot Password, and Reset Password — all using a standalone `_AuthLayout.cshtml` that has no admin sidebar or topnav. An `IEmailSender` abstraction is defined in Application, with a no-op stub implementation in Infrastructure. All pages use Bootstrap 5.3 with `--primary-color: #004D82`. No new database migrations are required; the schema from Phase 1 is complete.

## Technical Context

**Language/Version**: C# 12 / .NET 8
**Primary Dependencies**: ASP.NET Core MVC 8, ASP.NET Core Identity (built-in — `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`), Bootstrap 5.3 (CDN or local), Font Awesome 6.x (CDN or local)
**Storage**: SQL Server 2019+ — no new tables; inherits `AspNetUsers` schema with `FullName` and `IsActive` from Phase 1
**Testing**: Manual smoke tests per acceptance criteria in spec.md; use seeded SuperAdmin account from Phase 1
**Target Platform**: Windows / Linux server — ASP.NET Core 8 cross-platform web application
**Project Type**: Web application — ASP.NET Core MVC (UI pages only, no API endpoints)
**Performance Goals**: Page load under 1 second on developer hardware; form submission round-trip under 500 ms
**Constraints**: Email sending must be behind `IEmailSender` abstraction — no direct SMTP calls in Application or Web; no `dotnet ef` CLI; no new migrations needed
**Scale/Scope**: Single-tenant admin template; 4 page views, 1 controller, 1 application service, 1 email sender stub

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Constitution Principle | Status | Notes |
|---|---|---|---|
| I | Clean Architecture — 4-layer separation | ✅ PASS | `AccountController` in Web; `IAccountService` / `IEmailSender` in Application; `NoOpEmailSender` in Infrastructure; no new Domain types |
| II | Dependency Inversion — dependencies point inward | ✅ PASS | `AccountController` depends only on `IAccountService` (Application abstraction); Identity's `SignInManager`/`UserManager` are used inside `AccountService` in the Application layer, not injected into the controller |
| III | Thin Controllers — no business logic | ✅ PASS | Controller binds ViewModels, calls `IAccountService`, returns View or RedirectToAction; all auth logic in `AccountService` |
| IV | Repository + Unit of Work — all data access via repos | ✅ PASS | `AccountService` uses `IUserRepository` for user lookups; no direct `DbContext` usage in Application or Web |
| V | JSON-Driven Permissions — no hardcoded roles/functions | ✅ N/A | Auth pages have no permission objects; permissions engine is Phase 4 |
| VI | Generic DataTable Solution | ✅ N/A | No DataTables on auth pages |
| VII | Permission-Aware UI | ✅ N/A | Auth pages are public (pre-login); no permission UI needed |
| SEC | CSRF on all POST forms | ✅ PASS | `[ValidateAntiForgeryToken]` on Login POST, Register POST, ForgotPassword POST, ResetPassword POST; `@Html.AntiForgeryToken()` or tag helper in each form |
| SEC | `[Authorize]` on admin routes | ✅ N/A | All Account actions are `[AllowAnonymous]`; admin-only routes come in Phase 3+. Already redirected to login by global policy set in Phase 1 |
| SEC | Identity password policy enforced | ✅ PASS | Complexity rules configured in Phase 1; `AccountService` relies on Identity's built-in validators — no re-implementation needed |
| SEC | Inactive account guard (`IsActive = false`) | ✅ PASS | `AccountService.LoginAsync` checks `IsActive` before calling `SignInManager.PasswordSignInAsync`; returns `LoginResult.Inactive` on failure |
| SEC | Email enumeration prevention | ✅ PASS | `AccountService.ForgotPasswordAsync` always returns success regardless of whether the email is registered; email is only sent when user exists |
| NMG | Migrations via PM Console only | ✅ N/A | No new migrations in Phase 2 |

**GATE RESULT: ALL CHECKS PASS — Proceed to Phase 0 research.**

## Project Structure

### Documentation (this feature)

```text
specs/002-auth-pages/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (ViewModels + AccountServiceResult types)
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (IAccountService, IEmailSender)
└── tasks.md             # Phase 2 output (speckit.tasks command)
```

### Source Code (repository root)

```text
AdminTemplate.Application/
├── Interfaces/
│   └── IAccountService.cs             (NEW)
├── Services/
│   └── AccountService.cs              (NEW — wraps Identity SignInManager + UserManager)
└── Providers/
    └── IEmailSender.cs                (NEW — defined in Application layer)

AdminTemplate.Infrastructure/
└── Providers/
    └── NoOpEmailSender.cs             (NEW — stub; logs to console; real SMTP plugged in later)

AdminTemplate.Web/
├── Controllers/
│   └── AccountController.cs          (NEW — Login, Register, ForgotPassword, ResetPassword, Logout)
├── ViewModels/
│   ├── LoginViewModel.cs              (NEW)
│   ├── RegisterViewModel.cs           (NEW)
│   ├── ForgotPasswordViewModel.cs     (NEW)
│   └── ResetPasswordViewModel.cs      (NEW)
└── Views/
    ├── Account/
    │   ├── Login.cshtml               (NEW)
    │   ├── Register.cshtml            (NEW)
    │   ├── ForgotPassword.cshtml      (NEW)
    │   ├── ForgotPasswordConfirmation.cshtml  (NEW)
    │   └── ResetPassword.cshtml       (NEW)
    └── Shared/
        └── _AuthLayout.cshtml         (NEW — standalone layout, no sidebar/topnav)
```

**Structure Decision**: Follows the existing 4-project clean architecture layout established in Phase 1. `_AuthLayout.cshtml` is a new shared layout living alongside the future `_Layout.cshtml` (Phase 3); both coexist in `Views/Shared/`. `ViewModels/` is a new subfolder in the Web project — not in Application — because ViewModels are MVC binding concerns, not business contracts.

## Complexity Tracking

> No constitution violations — this section is not applicable.

