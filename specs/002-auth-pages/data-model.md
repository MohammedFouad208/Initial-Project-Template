# Data Model: Authentication Pages (Phase 2)

**Branch**: `002-auth-pages` | **Date**: 2026-03-31
**Purpose**: Define ViewModels, service result types, and entity extensions used by the auth flow. No new database migrations are required — all entity changes were made in Phase 1.

---

## Existing Entities (from Phase 1 — no changes)

### ApplicationUser

Defined in `AdminTemplate.Domain/Entities/ApplicationUser.cs`. The following fields are relevant to Phase 2:

| Field | Type | Source | Notes |
|-------|------|--------|-------|
| `Id` | `string` | IdentityUser | Primary key (GUID string) |
| `Email` | `string` | IdentityUser | Used as login username |
| `NormalizedEmail` | `string` | IdentityUser | Managed by Identity |
| `PasswordHash` | `string` | IdentityUser | Managed by Identity |
| `LockoutEnabled` | `bool` | IdentityUser | Must be `true` to enable lockout |
| `LockoutEnd` | `DateTimeOffset?` | IdentityUser | Set by Identity on lockout |
| `AccessFailedCount` | `int` | IdentityUser | Incremented on each bad login |
| `FullName` | `string` | Custom (Phase 1) | Displayed in topnav after login |
| `IsActive` | `bool` | Custom (Phase 1) | Checked before SignIn |
| `CreatedAt` | `DateTime` | Custom (Phase 1) | Audit field |

---

## ViewModels (new in Phase 2)

ViewModels live in `AdminTemplate.Web/ViewModels/`. They carry Data Annotations for both client-side and server-side validation.

### LoginViewModel

```
AdminTemplate.Web/ViewModels/LoginViewModel.cs
```

| Property | Type | Validation | Notes |
|----------|------|-----------|-------|
| `Email` | `string` | Required, EmailAddress | Bound to email input |
| `Password` | `string` | Required | Bound to password input; `DataType.Password` |
| `RememberMe` | `bool` | — | Bound to checkbox; passed as `isPersistent` to SignInManager |

---

### RegisterViewModel

```
AdminTemplate.Web/ViewModels/RegisterViewModel.cs
```

| Property | Type | Validation | Notes |
|----------|------|-----------|-------|
| `FullName` | `string` | Required, MaxLength(100) | Maps to `ApplicationUser.FullName` |
| `Email` | `string` | Required, EmailAddress | Maps to `ApplicationUser.Email` |
| `Password` | `string` | Required, MinLength(8), `DataType.Password` | Identity complexity enforced server-side |
| `ConfirmPassword` | `string` | Required, Compare("Password"), `DataType.Password` | Client-side match check |

---

### ForgotPasswordViewModel

```
AdminTemplate.Web/ViewModels/ForgotPasswordViewModel.cs
```

| Property | Type | Validation | Notes |
|----------|------|-----------|-------|
| `Email` | `string` | Required, EmailAddress | Used to look up registered user |

---

### ResetPasswordViewModel

```
AdminTemplate.Web/ViewModels/ResetPasswordViewModel.cs
```

| Property | Type | Validation | Notes |
|----------|------|-----------|-------|
| `Email` | `string` | Required, EmailAddress | Re-echoed from query string for user confirmation |
| `Token` | `string` | Required | Base64Url-encoded reset token from query string; stored as hidden field |
| `Password` | `string` | Required, MinLength(8), `DataType.Password` | New password |
| `ConfirmPassword` | `string` | Required, Compare("Password"), `DataType.Password` | Confirmation |

---

## Service Result Types (new in Phase 2)

Defined in `AdminTemplate.Application/Services/` (nested within or alongside `AccountService`). These are simple enum/class types that allow `AccountController` to branch on outcome without catching exceptions or inspecting Identity internals.

### LoginResult (enum)

```
AdminTemplate.Application/Services/AccountService.cs  (nested enum)
```

| Value | Meaning |
|-------|---------|
| `Success` | Credentials valid, `IsActive = true`, no lockout |
| `InvalidCredentials` | Email not found or password incorrect |
| `LockedOut` | Identity `SignInResult.IsLockedOut = true` |
| `Inactive` | User found but `IsActive = false` |
| `RequiresTwoFactor` | Identity `SignInResult.RequiresTwoFactor` (reserved — 2FA is out of scope for this phase; surface as generic error if encountered) |

### RegisterResult (class)

```csharp
// Result of AccountService.RegisterAsync
public class RegisterResult
{
    public bool Succeeded { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];
}
```

Wraps `IdentityResult.Errors` into a list of human-readable strings, insulating the controller from `IdentityError` types.

---

## State Transitions

### Login Flow

```
[Login Page]
    │
    ├─ User.IsActive == false  ──────────────► LoginResult.Inactive ──► ModelError: "Account deactivated"
    │
    ├─ PasswordSignInAsync fails            ► LoginResult.InvalidCredentials ──► ModelError: "Invalid email or password"
    │
    ├─ SignInResult.IsLockedOut             ► LoginResult.LockedOut ──► ModelError: "Account temporarily locked"
    │
    └─ SignInResult.Succeeded               ► LoginResult.Success ──► RedirectToAction("Index", "Dashboard")
```

### Password Reset Flow

```
[ForgotPassword Page]
    │
    ├─ Email not registered ──► (silent) ──► Show confirmation message (no email sent)
    │                                            ↑ (prevents email enumeration)
    └─ Email registered     ──► Generate token ──► Encode ──► Build URL ──► IEmailSender.SendAsync ──► Show confirmation

[ResetPassword Page (via link)]
    │
    ├─ Token expired / used / invalid ──► IdentityResult.Errors ──► ModelError
    │
    └─ Token valid + new password valid ──► ResetPasswordAsync ──► RedirectToAction("Login") + success message
```

---

## Post-Design Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Clean Architecture | ✅ PASS | ViewModels in Web; service result types in Application; no new Domain types |
| II | Dependency Inversion | ✅ PASS | No Application or Domain type references Web or Infrastructure types |
| III | Thin Controllers | ✅ PASS | Controller branches on `LoginResult` / `RegisterResult`; no Identity logic in controller |
| SEC | CSRF | ✅ PASS | All ViewModels used on POST forms; `[ValidateAntiForgeryToken]` applied at controller action level |
| SEC | Email enumeration | ✅ PASS | `ForgotPasswordAsync` always returns success signal regardless of registration status |
