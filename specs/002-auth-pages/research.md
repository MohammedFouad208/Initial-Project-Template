# Research: Authentication Pages (Phase 2)

**Branch**: `002-auth-pages` | **Date**: 2026-03-31
**Purpose**: Resolve all design unknowns and technical questions before Phase 1 design artifacts are written.

---

## R-001 — Where should SignInManager and UserManager be consumed?

**Question**: The constitution requires thin controllers (Principle III) and inward-pointing dependencies (Principle II). Should `AccountController` inject `SignInManager<ApplicationUser>` directly, or should Identity be wrapped behind an Application-layer service?

**Decision**: Wrap Identity behind `IAccountService` in the Application layer. `AccountController` depends only on `IAccountService`.

**Rationale**:
- Constitution Principle III forbids business logic in controllers. Checking `IsActive`, handling `SignInResult`, and mapping `IdentityResult` errors to user messages all constitute business logic.
- Wrapping Identity gives a stable interface that does not change if Identity's internals evolve.
- `AccountService` lives in `AdminTemplate.Application/Services/` and receives `SignInManager<ApplicationUser>`, `UserManager<ApplicationUser>`, and `IEmailSender` via constructor injection.

**Alternatives considered**:
- Inject `SignInManager` directly into `AccountController` — rejected because it violates Principle III; lockout message logic, inactive-account checks, and result mapping would live in the controller.

---

## R-002 — Where should IEmailSender be defined?

**Question**: The email sender abstraction must not introduce a direct SMTP dependency into Application or Web. Where does the interface live, and where does the implementation live?

**Decision**: `IEmailSender` is defined in `AdminTemplate.Application/Providers/` (alongside the existing `IPermissionProvider`). The stub no-op implementation `NoOpEmailSender` lives in `AdminTemplate.Infrastructure/Providers/`.

**Rationale**:
- Application layer owns the abstraction (same as `IPermissionProvider`). This is consistent with the project's existing pattern.
- Infrastructure hosts the implementation, consistent with all other `I*` → `*` pairs (e.g., `IPermissionProvider` → `JsonPermissionProvider`).
- No SMTP NuGet package is needed for Phase 2. The stub logs messages to the debug output. Real SMTP (`MailKit`, `SendGrid`, etc.) can be dropped in later without touching Application or Web.

**Alternatives considered**:
- Define `IEmailSender` in Web — rejected; Web layer should not own cross-cutting abstractions.
- Use ASP.NET Core's built-in `IEmailSender<TUser>` (from Identity UI) — rejected; that interface is bound to Identity UI scaffolding which is not used in this custom template.

---

## R-003 — How to handle the "Remember Me" persistent cookie

**Question**: "Remember Me" must persist the session across browser restarts. What is the correct mechanism?

**Decision**: Pass `isPersistent: model.RememberMe` to `SignInManager.PasswordSignInAsync(...)`. ASP.NET Core Identity sets an authentication cookie with `IsPersistent = true` when this flag is true, which survives browser restarts per the configured `CookieAuthenticationOptions.ExpireTimeSpan`.

**Rationale**: Built-in behaviour — no custom implementation required. `ExpireTimeSpan` is already configured in Phase 1 via `AddDefaultIdentity` options.

**Alternatives considered**: Custom cookie, custom middleware — rejected; unnecessary complexity over a native Identity feature.

---

## R-004 — Password reset token generation and validation

**Question**: How are reset tokens generated, encoded for URL transmission, and validated?

**Decision**:
1. Generate: `await _userManager.GeneratePasswordResetTokenAsync(user)` — returns a raw token string.
2. Encode: `WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))` — required for safe URL embedding.
3. Build reset URL: `Url.Action("ResetPassword", "Account", new { email, token = encodedToken }, Request.Scheme)`
4. Send URL in email body via `IEmailSender.SendAsync(...)`.
5. Validate on submit: decode with `Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken))` then call `await _userManager.ResetPasswordAsync(user, decodedToken, newPassword)`.

**Rationale**: This is the standard ASP.NET Core Identity pattern. `WebEncoders` is in `Microsoft.AspNetCore.WebUtilities` (included with ASP.NET Core — no extra NuGet needed).

**Alternatives considered**: Custom token generation — rejected; Identity's built-in data-protection backed tokens are cryptographically secure, time-limited by default, and single-use.

---

## R-005 — Lockout feedback on the login page

**Question**: How should the login page communicate lockout state to the user?

**Decision**: `SignInManager.PasswordSignInAsync` returns `SignInResult.IsLockedOut = true` when the account is locked. `AccountService.LoginAsync` maps this to a `LoginResult` enum value of `LockedOut`. The controller adds a `ModelError` with a message such as "Your account has been temporarily locked. Please try again in a few minutes." The Login view renders it via `@Html.ValidationSummary(false)`.

**Rationale**: Reuses the standard `ModelState` error pipeline already used for invalid-credentials messages. No special view branching required.

**Alternatives considered**: Redirect to a dedicated lockout page — viable but heavier; rejected in favour of inline message for simpler UX.

---

## R-006 — Inactive account check

**Question**: The spec requires denying login to users with `IsActive = false`. Identity's `PasswordSignInAsync` does not know about `IsActive`. Where does this check happen?

**Decision**: `AccountService.LoginAsync` fetches the user by email via `_userManager.FindByEmailAsync(email)` first. If the user is found and `user.IsActive == false`, return `LoginResult.Inactive` before calling `SignInManager`. The controller surfaces this as a model error: "This account has been deactivated."

**Rationale**: The check must happen before Identity's lockout tracking runs, so that inactive accounts do not consume lockout attempt slots. Placing the check in `AccountService` keeps the controller thin.

**Alternatives considered**: Custom `IUserValidator<TUser>` — rejected; validators run during user creation, not login. Custom `SignInManager` subclass — over-engineered for a simple boolean flag check.

---

## R-007 — Standalone Auth layout structure

**Question**: Auth pages must have no sidebar or topnav. Should they use a completely separate layout file or override sections?

**Decision**: Create `Views/Shared/_AuthLayout.cshtml` — a minimal Bootstrap 5 page with a centred card. Auth views declare `@{ Layout = "_AuthLayout"; }` at the top. The main `_ViewStart.cshtml` sets the default layout to `"_Layout"` (which will be built in Phase 3). Auth views explicitly override this.

**Rationale**: Separate layouts are the standard ASP.NET Core MVC pattern for distinct visual regions. `_AuthLayout.cshtml` is fully self-contained (includes Bootstrap/FA CDN links, anti-forgery, basic head metadata). It will not be disturbed when Phase 3 builds the admin shell layout.

**Alternatives considered**: Null layout with inline full HTML — rejected; duplicates Bootstrap/CDN boilerplate across every auth view.

---

## R-008 — ViewModel location

**Question**: Should ViewModels live in the Application layer (with DTOs) or in the Web project?

**Decision**: ViewModels live in `AdminTemplate.Web/ViewModels/`. They are MVC binding and display concerns only.

**Rationale**: DTOs in the Application layer represent data transferred between layers. ViewModels carry Razor-specific annotations (`[Display]`, `[DataType(DataType.Password)]`, `asp-for` binding), which are UI concerns. Placing them in Application would pollute that layer with MVC framework attributes.

**Alternatives considered**: ViewModels in Application — rejected; introduces MVC framework dependency into the Application project.

---

## Summary of Decisions

| ID | Topic | Decision |
|----|-------|----------|
| R-001 | SignInManager location | Wrapped in `IAccountService` in Application |
| R-002 | IEmailSender location | Interface in Application/Providers; stub in Infrastructure/Providers |
| R-003 | Remember Me | `isPersistent` flag in `SignInManager.PasswordSignInAsync` |
| R-004 | Password reset tokens | Identity `GeneratePasswordResetTokenAsync` + `WebEncoders.Base64UrlEncode` |
| R-005 | Lockout feedback | `SignInResult.IsLockedOut` → `LoginResult.LockedOut` → `ModelState` error |
| R-006 | Inactive account guard | Check `user.IsActive` in `AccountService` before calling `SignInManager` |
| R-007 | Auth layout | Separate `_AuthLayout.cshtml`; Auth views set `Layout = "_AuthLayout"` |
| R-008 | ViewModels location | `AdminTemplate.Web/ViewModels/` — MVC concerns stay in Web |

**All NEEDS CLARIFICATION items resolved. Proceed to Phase 1 design.**
