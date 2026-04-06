using System.Threading.Tasks;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Application.Services;
using AdminTemplate.Domain.Entities;
using AdminTemplate.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AdminTemplate.Web.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            IAccountService accountService,
            SignInManager<ApplicationUser> signInManager)
        {
            _accountService = accountService;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
                return View(model);

            var result = await _accountService.LoginAsync(model.Email, model.Password, model.RememberMe);
            switch (result)
            {
                case AdminTemplate.Application.Services.LoginResult.Success:
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return LocalRedirect(returnUrl);
                    return RedirectToAction("Index", "Dashboard");
                case AdminTemplate.Application.Services.LoginResult.Inactive:
                    ModelState.AddModelError(string.Empty, "This account has been deactivated.");
                    break;
                case AdminTemplate.Application.Services.LoginResult.LockedOut:
                    ModelState.AddModelError(string.Empty, "Your account has been temporarily locked. Please try again later.");
                    break;
                default:
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    break;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // ── Registration ─────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _accountService.RegisterAsync(model.FullName, model.Email, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            TempData["SuccessMessage"] = "Account created successfully. Please sign in.";
            return RedirectToAction("Login");
        }

        // ── Forgot / Reset Password ───────────────────────────────────────────

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _accountService.ForgotPasswordAsync(model.Email, $"{Request.Scheme}://{Request.Host}");
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _accountService.ResetPasswordAsync(model.Email, model.Token, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            TempData["SuccessMessage"] = "Your password has been reset successfully. Please sign in.";
            return RedirectToAction("Login");
        }
    }
}
