using Couppa.Api.Models.ViewModels;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers.Admin;

/// <summary>SRS 15.6 — /Admin/Report/CartTopProducts render View kèm dữ liệu ban đầu, hỗ trợ đổi khoảng thời gian qua AJAX (JSON).</summary>
[Route("Admin/Report/[action]")]
[Authorize(Policy = "RequireAdmin")]
public class AdminReportController : Controller
{
    private readonly IReportService _reportService;

    public AdminReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> CartTopProducts(DateTimeOffset? from, DateTimeOffset? to)
    {
        var topProducts = await _reportService.GetCartTopProductsAsync(from, to);

        if (Request.Headers.Accept.Any(h => h != null && h.Contains("application/json")))
        {
            return Json(new { success = true, data = topProducts });
        }

        return View(new AdminReportsViewModel { TopProducts = topProducts });
    }
}
