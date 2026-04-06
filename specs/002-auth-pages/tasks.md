---
description: "Task list for Feature 002 — Authentication Pages"
---

# Tasks: Authentication Pages (Phase 2)

**Input**: Design documents from `specs/002-auth-pages/`
**Prerequisites**: plan.md ✅ · spec.md ✅ · research.md ✅ · data-model.md ✅ · contracts/auth-contracts.md ✅ · quickstart.md ✅

**Tests**: No automated test tasks — Phase 2 uses manual smoke tests per `quickstart.md`.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. Each story phase can be implemented and verified independently before starting the next.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (writes to different files, no incomplete dependency)
- **[Story]**: Which user story this belongs to — [US1]–[US4] maps to spec.md priorities P1–P4
- Setup / Foundational / Polish phases have no story label
- Exact file paths are included in all descriptions

---

## Phase 1: Setup

**Purpose**: Configuration prerequisites that must be in place before any code is written.
The solution structure (4 projects, project references, NuGet packages) is already complete from Phase 1 (Foundation Scaffold).

- [ ] T001 Verify `AdminTemplate.Web/appsettings.json` contains an `Identity:Lockout` section with `MaxFailedAccessAttempts: 5`, `DefaultLockoutTimeSpan: "00:05:00"`, and `AllowedForNewUsers: true`; add the section if it is absent (aligns with US4 acceptance criteria)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Application-layer abstractions, Infrastructure stub, standalone auth layout, and DI wiring. ALL of these must be complete before any user story phase can begin — every auth page depends on the layout, the interfaces, and the DI registrations.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T002 Create service result types in `AdminTemplate.Application/Services/AccountServiceResults.cs`: `LoginResult` enum (Success, InvalidCredentials, LockedOut, Inactive), `RegisterResult` record (bool Succeeded, IEnumerable\<string\> Errors, static Ok() and Fail(IEnumerable\<IdentityError\>) factory methods), `ResetPasswordResult` record (same shape as RegisterResult)
- [ ] T003 [P] Create `AdminTemplate.Application/Interfaces/IAccountService.cs` with five method signatures: `Task<LoginResult> LoginAsync(string email, string password, bool rememberMe)`, `Task<RegisterResult> RegisterAsync(string fullName, string email, string password)`, `Task ForgotPasswordAsync(string email, string resetUrlBase)`, `Task<ResetPasswordResult> ResetPasswordAsync(string email, string token, string newPassword)`, `Task LogoutAsync()` — include XML doc comments per contracts/auth-contracts.md
- [ ] T004 [P] Create `AdminTemplate.Application/Providers/IEmailSender.cs` with one method: `Task SendAsync(string toEmail, string subject, string htmlBody)` — interface in Application layer, not Web
- [ ] T005 Create `AdminTemplate.Infrastructure/Providers/NoOpEmailSender.cs` implementing `IEmailSender`; `SendAsync` writes to `Debug.WriteLine` and `Console.WriteLine` in the format: `[NoOpEmailSender] To: {toEmail} | Subject: {subject}` followed by the htmlBody — no SMTP, no exceptions (depends on T004)
- [ ] T006 Create `AdminTemplate.Web/Views/Shared/_AuthLayout.cshtml` — standalone HTML page with no sidebar or topnav; include Bootstrap 5.3 CDN (`<link>` in `<head>`), Font Awesome 6.x CDN, a `<style>` block defining `:root { --primary-color: #004D82; }`, a centred card container (`max-width: 420px`, auto horizontal margins, `margin-top: 10vh`), `@RenderBody()` inside the card, and `@await RenderSectionAsync("Scripts", required: false)` before `</body>`
- [ ] T007 Add DI registrations to `AdminTemplate.Infrastructure/Extensions/InfrastructureServiceExtensions.cs`: `services.AddScoped<IEmailSender, NoOpEmailSender>()` and `services.AddScoped<IAccountService, AccountService>()` — both registrations in the existing extension method that `Program.cs` calls (depends on T003, T004, T005 and anticipates T009)

**Checkpoint**: Foundation ready — interfaces exist, stub is registered, auth layout renders. All user story pages can now be scaffolded.

---

## Phase 3: User Story 1 — Registered User Logs In (Priority: P1) 🎯 MVP

**Goal**: A seeded user can navigate to `/Account/Login`, submit valid credentials, and be redirected to the home/dashboard page. Invalid credentials show an error. Logout clears the session.

**Independent Test**: Use the seeded SuperAdmin credentials from Phase 1. Verify: (1) valid login → redirect, (2) bad password → error on the same page, (3) inactive account (`IsActive = 0` in DB) → "deactivated" message, (4) Logout button → session cleared, redirect to Login. See `quickstart.md` Tests 1–4 and 11–12.

- [ ] T008 [P] [US1] Create `AdminTemplate.Web/ViewModels/LoginViewModel.cs` with properties: `Email` (string, `[Required]`, `[EmailAddress]`), `Password` (string, `[Required]`, `[DataType(DataType.Password)]`), `RememberMe` (bool)
- [ ] T009 [US1] Create `AdminTemplate.Application/Services/AccountService.cs` implementing `IAccountService`; inject `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, `IEmailSender` via constructor; implement `LoginAsync`: find user by email with `UserManager.FindByEmailAsync` → if null or `!user.IsActive` return `LoginResult.Inactive`/`LoginResult.InvalidCredentials` → call `SignInManager.PasswordSignInAsync(user, password, isPersistent: rememberMe, lockoutOnFailure: true)` → map `SignInResult` to `LoginResult`; implement `LogoutAsync`: call `SignInManager.SignOutAsync()` (depends on T002, T003)
- [ ] T010 [US1] Create `AdminTemplate.Web/Controllers/AccountController.cs` decorated with `[AllowAnonymous]`; implement `Login()` GET returning `View(new LoginViewModel())`; implement `Login(LoginViewModel model, string? returnUrl)` POST with `[ValidateAntiForgeryToken]` — on `ModelState.IsValid` call `IAccountService.LoginAsync`, map each `LoginResult` case to a `ModelState.AddModelError` message, on `LoginResult.Success` redirect using `Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index","Home")`; implement `Logout()` POST with `[Authorize]` and `[ValidateAntiForgeryToken]` calling `IAccountService.LogoutAsync()` then `RedirectToAction("Login")` (depends on T008, T009)
- [ ] T011 [US1] Create `AdminTemplate.Web/Views/Account/Login.cshtml` — set `@{ Layout = "_AuthLayout"; }` at top; render a card with a logo/title area (`AdminTemplate` in primary color), `asp-validation-summary="All"` div styled as Bootstrap danger alert, email input with `asp-for="Email"` and `asp-validation-for="Email"`, password input with `asp-for="Password"` and `asp-validation-for="Password"`, remember-me checkbox with `asp-for="RememberMe"`, submit button styled with `background-color: var(--primary-color)`, Forgot Password link to `Url.Action("ForgotPassword","Account")`, Register link; display `TempData["SuccessMessage"]` as Bootstrap success alert above the form if present (depends on T006, T008, T010)

**Checkpoint**: US1 complete — login, locked-out feedback, inactive account denial, and logout all work using the seeded SuperAdmin account.

---

## Phase 4: User Story 2 — New User Registers an Account (Priority: P2)

**Goal**: A new user navigates to `/Account/Register`, fills in full name, email, and a password meeting complexity rules, and their account is created. Duplicate email and weak passwords are rejected with field-level errors.

**Independent Test**: Navigate to `/Account/Register`; submit valid data → account created → redirected to Login with success message. Submit duplicate email → error. Submit weak password → Identity complexity error. Submit mismatched passwords → client-side error. See `quickstart.md` Tests 4–6.

- [ ] T012 [P] [US2] Create `AdminTemplate.Web/ViewModels/RegisterViewModel.cs` with properties: `FullName` (string, `[Required]`, `[MaxLength(100)]`), `Email` (string, `[Required]`, `[EmailAddress]`), `Password` (string, `[Required]`, `[MinLength(8)]`, `[DataType(DataType.Password)]`), `ConfirmPassword` (string, `[Required]`, `[Compare("Password")]`, `[DataType(DataType.Password)]`)
- [ ] T013 [US2] Add `RegisterAsync` implementation to `AdminTemplate.Application/Services/AccountService.cs` — create `new ApplicationUser { UserName = email, Email = email, FullName = fullName, IsActive = true, CreatedAt = DateTime.UtcNow }`, call `UserManager.CreateAsync(user, password)`, return `RegisterResult.Ok()` on success or `RegisterResult.Fail(result.Errors)` on failure (depends on T009)
- [ ] T014 [US2] Add `Register()` GET and `Register(RegisterViewModel model)` POST (`[ValidateAntiForgeryToken]`) to `AdminTemplate.Web/Controllers/AccountController.cs` — POST calls `IAccountService.RegisterAsync`, on failure adds each error to `ModelState` and returns `View(model)`, on success sets `TempData["SuccessMessage"] = "Account created successfully. Please sign in."` and redirects to `RedirectToAction("Login")` (depends on T012, T013)
- [ ] T015 [US2] Create `AdminTemplate.Web/Views/Account/Register.cshtml` — set `@{ Layout = "_AuthLayout"; }` at top; render card with title, `asp-validation-summary="All"` error area, `asp-for` inputs for FullName, Email, Password, ConfirmPassword each with `asp-validation-for` span, submit button in primary color, link back to Login; add `@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") }` for client-side validation (depends on T006, T012, T014)

**Checkpoint**: US2 complete — registration form validates client-side and server-side; new account persists in DB; duplicate email rejected; post-registration redirect to Login with success message.

---

## Phase 5: User Story 3 — User Resets a Forgotten Password (Priority: P3)

**Goal**: A user who cannot remember their password submits their email on the Forgot Password page. A reset link is generated and sent via `IEmailSender` (logged to console by `NoOpEmailSender`). Following the link opens the Reset Password page. A valid new password saves successfully.

**Independent Test**: Navigate to `/Account/ForgotPassword`; submit SuperAdmin email → confirmation page shown → copy reset URL from console debug output → open URL → submit valid new password → redirected to Login → log in with new password succeeds. Submitting an unregistered email → same confirmation page shown, no error. See `quickstart.md` Tests 7–10.

- [ ] T016 [P] [US3] Create `AdminTemplate.Web/ViewModels/ForgotPasswordViewModel.cs` with property: `Email` (string, `[Required]`, `[EmailAddress]`)
- [ ] T017 [P] [US3] Create `AdminTemplate.Web/ViewModels/ResetPasswordViewModel.cs` with properties: `Email` (string, `[Required]`, `[EmailAddress]`), `Token` (string, `[Required]` — hidden field), `Password` (string, `[Required]`, `[MinLength(8)]`, `[DataType(DataType.Password)]`), `ConfirmPassword` (string, `[Required]`, `[Compare("Password")]`, `[DataType(DataType.Password)]`)
- [ ] T018 [US3] Add `ForgotPasswordAsync` implementation to `AdminTemplate.Application/Services/AccountService.cs` — call `UserManager.FindByEmailAsync(email)`; if user is null, return immediately (email enumeration guard, FR-011); generate token with `UserManager.GeneratePasswordResetTokenAsync(user)`, encode with `WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))`, build callback URL as `$"{resetUrlBase}/Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={encodedToken}"`, send via `IEmailSender.SendAsync(...)` with a plain HTML reset email body (depends on T009)
- [ ] T019 [US3] Add `ResetPasswordAsync` implementation to `AdminTemplate.Application/Services/AccountService.cs` — find user by email; if null return `ResetPasswordResult.Fail("Invalid request")`; decode token with `Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken))`; call `UserManager.ResetPasswordAsync(user, decodedToken, newPassword)`; return `ResetPasswordResult.Ok()` or `ResetPasswordResult.Fail(result.Errors)` (depends on T018)
- [ ] T020 [US3] Add ForgotPassword GET, ForgotPassword POST (`[ValidateAntiForgeryToken]`), ForgotPasswordConfirmation GET, ResetPassword GET (`string email, string token` query params → bind into `ResetPasswordViewModel`), and ResetPassword POST (`[ValidateAntiForgeryToken]`) to `AdminTemplate.Web/Controllers/AccountController.cs` — ForgotPassword POST calls `ForgotPasswordAsync($"{Request.Scheme}://{Request.Host}")` then redirects to ForgotPasswordConfirmation; ResetPassword POST calls `ResetPasswordAsync`, on success redirects to Login, on failure adds errors to ModelState and returns View (depends on T016, T017, T018, T019)
- [ ] T021 [P] [US3] Create `AdminTemplate.Web/Views/Account/ForgotPassword.cshtml` — set `@{ Layout = "_AuthLayout"; }` at top; render card with title "Forgot Password", `asp-validation-summary` error area, `asp-for="Email"` input with `asp-validation-for="Email"`, submit button in primary color, link back to Login; add `@section Scripts` with `_ValidationScriptsPartial` (depends on T006, T016)
- [ ] T022 [P] [US3] Create `AdminTemplate.Web/Views/Account/ForgotPasswordConfirmation.cshtml` — set `@{ Layout = "_AuthLayout"; }` at top; render card with title "Check Your Email" and message: "A password reset link has been sent. Please check your inbox (and spam folder). In development, the link is printed to the application console output."; include a link back to Login (depends on T006)
- [ ] T023 [US3] Create `AdminTemplate.Web/Views/Account/ResetPassword.cshtml` — set `@{ Layout = "_AuthLayout"; }` at top; render card with title "Reset Password", `asp-validation-summary` error area, hidden inputs for Email and Token (`asp-for="Email"` type=hidden, `asp-for="Token"` type=hidden), password and confirm-password inputs each with `asp-validation-for`, submit button in primary color, link back to Login; add `@section Scripts` with `_ValidationScriptsPartial` (depends on T006, T017)

**Checkpoint**: US3 complete — reset token is generated and logged to console, reset link opens the form, valid token → password updated, invalid/expired token → error shown.

---

## Phase 6: User Story 4 — Account Lockout After Repeated Failures (Priority: P4)

**Goal**: After 5 consecutive failed login attempts the account is locked and a clear lockout message is displayed. Lockout duration is configurable in `appsettings.json`.

**Independent Test**: Submit wrong credentials 5 times for any valid account → 5th attempt shows lockout message, not "invalid credentials". Wait for lockout period → login succeeds again. See `quickstart.md` Test 3 and SC-004.

- [ ] T024 [US4] Verify `AdminTemplate.Web/Program.cs` Identity configuration reads lockout settings from `appsettings.json` — ensure `options.Lockout.MaxFailedAccessAttempts`, `options.Lockout.DefaultLockoutTimeSpan`, and `options.Lockout.AllowedForNewUsers` are bound from `IConfiguration` (or hardcoded to match appsettings values); confirm `LockoutEnabled` is set to `true` on the seeded SuperAdmin in `DataSeeder.cs` if it isn't already (a default Identity user has `LockoutEnabled = true`)
- [ ] T025 [P] [US4] Verify `AccountService.LoginAsync` passes `lockoutOnFailure: true` as the 4th positional argument in the `SignInManager.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure: true)` call (without this flag, Identity will not increment `AccessFailedCount` and lockout will never trigger); add inline comment: `// lockoutOnFailure: true — required for US4 lockout behaviour`

**Checkpoint**: US4 complete — 5 consecutive bad-password attempts lock the account; `LoginResult.LockedOut` branch in `AccountController.Login` POST surfaces "Your account has been temporarily locked. Please try again later." message.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validation script wiring for all auth views, TempData flash support, and a final build verification.

- [ ] T026 Add `@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") }` to each auth view that already exists (Login.cshtml, Register.cshtml, ForgotPassword.cshtml, ResetPassword.cshtml) if not yet present — this enables jQuery Validate unobtrusive client-side validation; `_AuthLayout.cshtml` must already render the Scripts section (established in T006)
- [ ] T027 [P] Confirm `AdminTemplate.Web/Views/_ViewImports.cshtml` includes `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` — required for `asp-for`, `asp-validation-for`, `asp-action`, `asp-controller` tag helpers used by all auth views; add it if absent
- [ ] T028 Build the solution and verify it compiles with 0 errors and 0 warnings; start the Web project, navigate to `/Account/Login`, and confirm the auth layout renders without JavaScript console errors; fix any compilation or runtime issues found

**Checkpoint**: All 15 functional requirements are addressed. Auth flow is complete end-to-end. Solution builds clean.

---

## Dependencies

```
T001 (config)
    └── T002 (result types)
            ├── T003 (IAccountService) ──────────────────────────────────────────┐
            │                                                                     │
            └── T004 (IEmailSender)                                              │
                    └── T005 (NoOpEmailSender)                                   │
                            └── T007 (DI registration) ◄───────────────────────┘
                                                                                 │
T006 (_AuthLayout) ─────────────────────────────────────────────────────────────┤
                                                                                 │
Phase 3 (US1 — Login):                                                           │
    T008 (LoginViewModel) ──────────────────────────┐                            │
    T009 (AccountService LoginAsync/LogoutAsync) ◄──┘ ◄── T002, T003            │
    T010 (AccountController Login+Logout) ◄─────────── T008, T009               │
    T011 (Login.cshtml) ◄───────────────────────────── T006, T008, T010         │
                                                                                 │
Phase 4 (US2 — Register):                                                        │
    T012 (RegisterViewModel) ─────────────────────────┐                          │
    T013 (AccountService RegisterAsync) ◄─────────────┘ ◄── T009               │
    T014 (AccountController Register) ◄───────────────── T012, T013             │
    T015 (Register.cshtml) ◄──────────────────────────── T006, T012, T014       │
                                                                                 │
Phase 5 (US3 — Password Reset):                                                  │
    T016 (ForgotPasswordViewModel) ─────────────────────┐                        │
    T017 (ResetPasswordViewModel) ──────────────────────┤                        │
    T018 (AccountService ForgotPasswordAsync) ◄──────── T009, T004              │
    T019 (AccountService ResetPasswordAsync) ◄────────── T018                   │
    T020 (AccountController ForgotPwd+ResetPwd) ◄────── T016, T017, T018, T019 │
    T021 (ForgotPassword.cshtml) ◄───────────────────── T006, T016              │
    T022 (ForgotPasswordConfirmation.cshtml) ◄────────── T006                   │
    T023 (ResetPassword.cshtml) ◄──────────────────────── T006, T017            │
                                                                                 │
Phase 6 (US4 — Lockout):                                                         │
    T024 (Program.cs lockout config verify) ◄──────────── T001                  │
    T025 (AccountService lockoutOnFailure verify) ◄──────── T009                │
```

---

## Parallel Execution Opportunities

### Group A — Foundational interfaces (run in parallel after T002)
- T003 Create IAccountService + T004 Create IEmailSender

### Group B — Phase 3 kickoff (run in parallel after T007)
- T008 Create LoginViewModel (independent of Service and Controller)

### Group C — Phase 4 kickoff (run in parallel after T009)
- T012 Create RegisterViewModel (independent of Service extension)

### Group D — Phase 5 kickoff (run in parallel after T009)
- T016 Create ForgotPasswordViewModel + T017 Create ResetPasswordViewModel
- T021 ForgotPassword.cshtml + T022 ForgotPasswordConfirmation.cshtml (both need only T006)

### Group E — Polish (run in parallel)
- T026 Validation scripts + T027 ViewImports check

---

## Implementation Strategy

### MVP (deliver US1 first — Login + Logout)
Implement T001 → T002 → T003+T004 (parallel) → T005 → T006 → T007 → T008+T009 (T008 parallel) → T010 → T011

With US1 complete, a seeded user can authenticate and the entire Phase 3 feature set becomes verifiable.

### Incremental delivery order
1. **T001–T007**: Foundational wiring (~30 min)
2. **T008–T011**: US1 Login + Logout MVP (~45 min)
3. **T012–T015**: US2 Registration (~30 min)
4. **T016–T023**: US3 Password Reset (~45 min)
5. **T024–T025**: US4 Lockout verification (~10 min)
6. **T026–T028**: Polish + build verification (~15 min)

---

## Task Count Summary

| Phase | Tasks | Story |
|-------|-------|-------|
| Phase 1 — Setup | 1 | — |
| Phase 2 — Foundational | 6 | — |
| Phase 3 — US1 Login + Logout | 4 | P1 (MVP) |
| Phase 4 — US2 Registration | 4 | P2 |
| Phase 5 — US3 Password Reset | 8 | P3 |
| Phase 6 — US4 Lockout | 2 | P4 |
| Phase 7 — Polish | 3 | — |
| **Total** | **28** | |

**Parallel opportunities**: 5 groups identified (see above)  
**MVP scope**: T001–T011 (11 tasks) delivers a fully working Login + Logout against the seeded SuperAdmin
