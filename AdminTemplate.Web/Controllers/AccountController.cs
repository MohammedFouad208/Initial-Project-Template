using System.Threading.Tasks;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Application.Services;
using AdminTemplate.Domain.Entities;

using AdminTemplate.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AdminTemplate.Web.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AccountController(
            IAccountService accountService,
            SignInManager<ApplicationUser> signInManager,
            IStringLocalizer<SharedResource> localizer)
        {
            _accountService = accountService;
            _signInManager = signInManager;
            _localizer = localizer;
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
                case LoginResult.Success:
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return LocalRedirect(returnUrl);
                    return RedirectToAction("Index", "Dashboard");
                case LoginResult.Inactive:
                    ModelState.AddModelError(string.Empty, _localizer["Login_Error_Inactive"]);
                    break;
                case LoginResult.LockedOut:
                    ModelState.AddModelError(string.Empty, _localizer["Login_Error_LockedOut"]);
                    break;
                default:
                    ModelState.AddModelError(string.Empty, _localizer["Login_Error_InvalidCredentials"]);
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

            TempData["SuccessMessage"] = _localizer["Register_Success"];
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

            TempData["SuccessMessage"] = _localizer["ResetPassword_Success"];
            return RedirectToAction("Login");
        }
    }
}
