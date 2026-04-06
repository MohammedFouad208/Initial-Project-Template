using AdminTemplate.Application.Interfaces;
using AdminTemplate.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminTemplate.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;

    public DashboardController(IUserService userService, IRoleService roleService)
    {
        _userService = userService;
        _roleService = roleService;
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            TotalUsers = await _userService.GetTotalCountAsync(),
            TotalRoles = await _roleService.GetTotalCountAsync(),
            ActiveUsers = await _userService.GetActiveCountAsync()
        };

        return View(model);
    }
}
