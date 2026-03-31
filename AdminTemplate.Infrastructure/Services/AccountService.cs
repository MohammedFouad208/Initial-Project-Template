using System;
using System.Text;
using System.Threading.Tasks;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Application.Providers;
using AdminTemplate.Application.Services;
using AdminTemplate.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace AdminTemplate.Infrastructure.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AccountService(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }

        public async Task<LoginResult> LoginAsync(string email, string password, bool rememberMe)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return LoginResult.InvalidCredentials;

            if (!user.IsActive)
                return LoginResult.Inactive;

            // lockoutOnFailure: true — required for US4 lockout behaviour
            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: rememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
                return LoginResult.Success;
            if (result.IsLockedOut)
                return LoginResult.LockedOut;
            return LoginResult.InvalidCredentials;
        }

        public async Task<RegisterResult> RegisterAsync(string fullName, string email, string password)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            return result.Succeeded
                ? RegisterResult.Ok()
                : RegisterResult.Fail(result.Errors);
        }

        public async Task ForgotPasswordAsync(string email, string resetUrlBase)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return; // email enumeration guard (FR-011)

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = $"{resetUrlBase}/Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={encodedToken}";

            var htmlBody = $"<p>You requested a password reset. Click the link below to reset your password:</p>" +
                           $"<p><a href='{callbackUrl}'>Reset Password</a></p>" +
                           $"<p>If you did not request this, please ignore this email.</p>";

            await _emailSender.SendAsync(email, "Reset your password", htmlBody);
        }

        public async Task<ResetPasswordResult> ResetPasswordAsync(string email, string encodedToken, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return ResetPasswordResult.Fail("Invalid request.");

            var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded
                ? ResetPasswordResult.Ok()
                : ResetPasswordResult.Fail(result.Errors);
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
