# Quickstart: Authentication Pages (Phase 2)

**Branch**: `002-auth-pages` | **Date**: 2026-03-31
**Prerequisite**: Phase 1 (Foundation Scaffold) must be complete — database migrated, SuperAdmin seeded.

---

## What Was Built

| Deliverable | Location |
|-------------|----------|
| `IAccountService` interface | `AdminTemplate.Application/Interfaces/IAccountService.cs` |
| `AccountService` implementation | `AdminTemplate.Application/Services/AccountService.cs` |
| `IEmailSender` interface | `AdminTemplate.Application/Providers/IEmailSender.cs` |
| `NoOpEmailSender` stub | `AdminTemplate.Infrastructure/Providers/NoOpEmailSender.cs` |
| `AccountController` | `AdminTemplate.Web/Controllers/AccountController.cs` |
| ViewModels (Login, Register, ForgotPassword, ResetPassword) | `AdminTemplate.Web/ViewModels/` |
| `_AuthLayout.cshtml` | `AdminTemplate.Web/Views/Shared/_AuthLayout.cshtml` |
| Login view | `AdminTemplate.Web/Views/Account/Login.cshtml` |
| Register view | `AdminTemplate.Web/Views/Account/Register.cshtml` |
| ForgotPassword view | `AdminTemplate.Web/Views/Account/ForgotPassword.cshtml` |
| ForgotPasswordConfirmation view | `AdminTemplate.Web/Views/Account/ForgotPasswordConfirmation.cshtml` |
| ResetPassword view | `AdminTemplate.Web/Views/Account/ResetPassword.cshtml` |

---

## No New Migrations Required

All database schema changes were applied in Phase 1. Run `Update-Database` only if you are setting up from scratch.

```powershell
# Package Manager Console (only if starting from scratch)
Update-Database
```

---

## Running the Application

```powershell
# In Package Manager Console or terminal
# Set AdminTemplate.Web as the startup project, then press F5 or:
dotnet run --project AdminTemplate.Web
```

Navigate to: `https://localhost:{port}/Account/Login`

---

## Testing Authentication

### Test 1 — Login with Seeded SuperAdmin

1. Navigate to `/Account/Login`
2. Enter the seeded credentials (configured in `DataSeeder.cs`):
   - Email: `admin@admintemplate.com` (or as configured)
   - Password: `Admin@123456` (or as configured)
3. **Expected**: Redirected to `/Dashboard`

### Test 2 — Invalid Credentials

1. Navigate to `/Account/Login`
2. Enter a valid email with a wrong password
3. **Expected**: Stay on Login page with error: "Invalid email or password."

### Test 3 — Account Lockout

1. Navigate to `/Account/Login`
2. Submit invalid credentials 5 consecutive times (same account)
3. **Expected**: On the 5th failure, message: "Your account has been temporarily locked."
4. Wait for the lockout period (default: 5 minutes from `appsettings.json`) and try again
5. **Expected**: Login succeeds normally after lockout expires

### Test 4 — Register New Account

1. Navigate to `/Account/Register`
2. Fill in full name, email, password (must meet complexity), matching confirmation
3. **Expected**: Account created; redirected to Login page (or auto-signed in)

### Test 5 — Register Duplicate Email

1. Navigate to `/Account/Register`
2. Use the SuperAdmin email address
3. **Expected**: Error: "Email `admin@admintemplate.com` is already taken."

### Test 6 — Password Complexity Validation

1. Navigate to `/Account/Register`
2. Enter a password like `password` (no uppercase, digit, or special char)
3. **Expected**: Server-side validation error listing complexity requirements

### Test 7 — Forgot Password (Registered Email)

1. Navigate to `/Account/ForgotPassword`
2. Enter the SuperAdmin email
3. **Expected**: Redirected to `/Account/ForgotPasswordConfirmation` with message "Check your email."
4. In development: check the debug/console output for the `NoOpEmailSender` log — the reset link is printed there

### Test 8 — Forgot Password (Unknown Email — Email Enumeration Guard)

1. Navigate to `/Account/ForgotPassword`
2. Enter an email not in the database
3. **Expected**: Same confirmation page shown — no error, no hint that the email doesn't exist

### Test 9 — Reset Password via Link

1. Complete Test 7 to obtain the reset URL from debug output
2. Open the reset URL in the browser
3. Enter a new valid password and confirm it
4. **Expected**: Redirected to Login page. Log in with the new password — should succeed.

### Test 10 — Expired / Invalid Reset Token

1. Navigate to `/Account/ResetPassword?email=...&token=INVALIDTOKEN`
2. Submit any password
3. **Expected**: Error message shown on the Reset Password page; no password change

### Test 11 — Inactive Account

1. In the database, set `IsActive = 0` for a user via SQL Server Management Studio:
   ```sql
   UPDATE AspNetUsers SET IsActive = 0 WHERE Email = 'admin@admintemplate.com'
   ```
2. Attempt to log in with that account
3. **Expected**: Error: "This account has been deactivated."
4. Restore: `UPDATE AspNetUsers SET IsActive = 1 WHERE Email = 'admin@admintemplate.com'`

### Test 12 — Unauthenticated Navigation Guard

1. Log out (or open a private window)
2. Navigate directly to `/Dashboard` (or any future admin route)
3. **Expected**: Redirected to `/Account/Login?ReturnUrl=%2FDashboard`
4. Log in successfully
5. **Expected**: Redirected back to `/Dashboard`

---

## Configuring Lockout Settings

Edit `appsettings.json`:

```json
"IdentityOptions": {
  "Lockout": {
    "MaxFailedAccessAttempts": 5,
    "DefaultLockoutTimeSpan": "00:05:00",
    "AllowedForNewUsers": true
  }
}
```

## Configuring Email (Swap the Stub)

Replace `NoOpEmailSender` in `InfrastructureServiceExtensions.cs`:

```csharp
// Replace this:
services.AddScoped<IEmailSender, NoOpEmailSender>();

// With your real implementation, e.g. MailKit:
services.AddScoped<IEmailSender, SmtpEmailSender>();
```

Add SMTP settings to `appsettings.json`:

```json
"Smtp": {
  "Host": "smtp.example.com",
  "Port": 587,
  "Username": "your@email.com",
  "Password": "yourpassword",
  "FromAddress": "noreply@example.com"
}
```

---

## Phase Acceptance Criteria

- [ ] Login with correct credentials → redirected to Dashboard
- [ ] Login with wrong credentials → error on Login page, no crash
- [ ] 5 failed logins → lockout message displayed
- [ ] Register with valid data → account created
- [ ] Register with duplicate email → friendly error
- [ ] Register with weak password → Identity complexity error
- [ ] Forgot Password → confirmation page shown (both registered and unregistered emails)
- [ ] Reset password via valid link → password changed, can log in
- [ ] Reset password via invalid/expired link → error shown
- [ ] Inactive account login → denied with message
- [ ] Unauthenticated access to protected route → redirect to Login with ReturnUrl
- [ ] All four auth pages render correctly on mobile (Bootstrap responsive)
- [ ] No stack traces visible to end users on any error path
