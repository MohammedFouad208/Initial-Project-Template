# Interface Contracts: Authentication Pages (Phase 2)

**Branch**: `002-auth-pages` | **Date**: 2026-03-31
**Purpose**: Formal contracts for Application-layer interfaces introduced in Phase 2. These are the stable boundaries that controllers and tests depend on.

---

## IAccountService

**Location**: `AdminTemplate.Application/Interfaces/IAccountService.cs`
**Consumed by**: `AccountController` (Web layer)
**Implemented by**: `AccountService` (Application layer — wraps Identity)

```csharp
namespace AdminTemplate.Application.Interfaces;

public interface IAccountService
{
    /// <summary>
    /// Validates credentials and signs the user in via cookie.
    /// Checks IsActive before calling SignInManager.
    /// </summary>
    /// <param name="email">User's registered email address.</param>
    /// <param name="password">Plain-text password (validated by Identity).</param>
    /// <param name="rememberMe">true = persistent cookie survives browser restart.</param>
    /// <returns>
    ///   LoginResult.Success          — credentials valid, user is active, signed in.
    ///   LoginResult.InvalidCredentials — email not found or password wrong.
    ///   LoginResult.LockedOut        — too many failed attempts; account temporarily locked.
    ///   LoginResult.Inactive         — user.IsActive == false; login denied.
    /// </returns>
    Task<LoginResult> LoginAsync(string email, string password, bool rememberMe);

    /// <summary>
    /// Creates a new ApplicationUser account.
    /// Returns errors from IdentityResult (e.g., duplicate email, weak password).
    /// </summary>
    Task<RegisterResult> RegisterAsync(string fullName, string email, string password);

    /// <summary>
    /// Generates a password reset token, encodes it for URL safety,
    /// builds the reset URL, and sends it via IEmailSender.
    /// Always returns without error even if the email is not registered
    /// (prevents email enumeration — FR-011).
    /// </summary>
    /// <param name="email">Email address submitted by the user.</param>
    /// <param name="resetUrlBase">
    ///   Callback URL prefix (scheme + host) used to build the reset link.
    ///   Example: "https://localhost:5001"
    /// </param>
    Task ForgotPasswordAsync(string email, string resetUrlBase);

    /// <summary>
    /// Validates the reset token and updates the user's password.
    /// </summary>
    /// <param name="email">Email address of the account being reset.</param>
    /// <param name="token">Base64Url-encoded token from the reset email link.</param>
    /// <param name="newPassword">New plain-text password (validated by Identity).</param>
    /// <returns>
    ///   Succeeded = true  — password updated successfully.
    ///   Succeeded = false — invalid/expired token or password policy violation; Errors contains messages.
    /// </returns>
    Task<ResetPasswordResult> ResetPasswordAsync(string email, string token, string newPassword);

    /// <summary>
    /// Signs the current user out, clearing the auth cookie.
    /// </summary>
    Task LogoutAsync();
}
```

### LoginResult Enum

```csharp
namespace AdminTemplate.Application.Services;

public enum LoginResult
{
    Success,
    InvalidCredentials,
    LockedOut,
    Inactive
}
```

### RegisterResult Class

```csharp
namespace AdminTemplate.Application.Services;

public class RegisterResult
{
    public bool Succeeded { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];

    public static RegisterResult Ok() => new() { Succeeded = true };

    public static RegisterResult Fail(IEnumerable<IdentityError> errors) =>
        new() { Succeeded = false, Errors = errors.Select(e => e.Description) };
}
```

### ResetPasswordResult Class

```csharp
namespace AdminTemplate.Application.Services;

public class ResetPasswordResult
{
    public bool Succeeded { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];

    public static ResetPasswordResult Ok() => new() { Succeeded = true };

    public static ResetPasswordResult Fail(IEnumerable<IdentityError> errors) =>
        new() { Succeeded = false, Errors = errors.Select(e => e.Description) };
}
```

---

## IEmailSender

**Location**: `AdminTemplate.Application/Providers/IEmailSender.cs`
**Consumed by**: `AccountService` (Application layer)
**Implemented by**: `NoOpEmailSender` (Infrastructure layer — Phase 2 stub)

```csharp
namespace AdminTemplate.Application.Providers;

public interface IEmailSender
{
    /// <summary>
    /// Sends an email message. The stub implementation logs to debug output.
    /// Replace the Infrastructure registration with a real SMTP or API implementation.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="htmlBody">HTML body content (safe HTML only — no user input directly interpolated).</param>
    Task SendAsync(string toEmail, string subject, string htmlBody);
}
```

---

## DI Registration Contract

`NoOpEmailSender` is registered in `InfrastructureServiceExtensions.cs` (already exists from Phase 1). The following registration is added:

```csharp
// In AdminTemplate.Infrastructure/Extensions/InfrastructureServiceExtensions.cs
services.AddScoped<IEmailSender, NoOpEmailSender>();
services.AddScoped<IAccountService, AccountService>();
```

**Note**: `AccountService` uses `SignInManager<ApplicationUser>` and `UserManager<ApplicationUser>`, which are registered automatically by `services.AddIdentity<ApplicationUser, ApplicationRole>(...)` in Phase 1. No additional Identity registrations are needed.

---

## AccountController Action Map

| HTTP Verb | Route | Action | Auth |
|-----------|-------|--------|------|
| GET | `/Account/Login` | `Login()` | `[AllowAnonymous]` |
| POST | `/Account/Login` | `Login(LoginViewModel, returnUrl)` | `[AllowAnonymous]`, `[ValidateAntiForgeryToken]` |
| GET | `/Account/Register` | `Register()` | `[AllowAnonymous]` |
| POST | `/Account/Register` | `Register(RegisterViewModel)` | `[AllowAnonymous]`, `[ValidateAntiForgeryToken]` |
| GET | `/Account/ForgotPassword` | `ForgotPassword()` | `[AllowAnonymous]` |
| POST | `/Account/ForgotPassword` | `ForgotPassword(ForgotPasswordViewModel)` | `[AllowAnonymous]`, `[ValidateAntiForgeryToken]` |
| GET | `/Account/ForgotPasswordConfirmation` | `ForgotPasswordConfirmation()` | `[AllowAnonymous]` |
| GET | `/Account/ResetPassword` | `ResetPassword(email, token)` | `[AllowAnonymous]` |
| POST | `/Account/ResetPassword` | `ResetPassword(ResetPasswordViewModel)` | `[AllowAnonymous]`, `[ValidateAntiForgeryToken]` |
| POST | `/Account/Logout` | `Logout()` | `[Authorize]`, `[ValidateAntiForgeryToken]` |

**Return URL handling**: The `Login` POST validates that `returnUrl` is a local URL (`Url.IsLocalUrl(returnUrl)`) before redirecting to prevent open redirect attacks.
