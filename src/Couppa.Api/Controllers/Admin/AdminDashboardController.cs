using Couppa.Api.Models.ViewModels;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers.Admin;

/// <summary>SRS 15.6 — /Admin/Dashboard/Index render View; Summary trả JsonResult (refresh không reload).</summary>
[Route("Admin/Dashboard/[action]")]
[Authorize(Policy = "RequireAdmin")]
public class AdminDashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public AdminDashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var summary = await _dashboardService.GetSummaryAsync();
        return View(new AdminDashboardViewModel { Summary = summary });
    }

    [HttpGet]
    public async Task<IActionResult> Summary()
    {
        var summary = await _dashboardService.GetSummaryAsync();
        return Json(new { success = true, data = summary });
    }
}
