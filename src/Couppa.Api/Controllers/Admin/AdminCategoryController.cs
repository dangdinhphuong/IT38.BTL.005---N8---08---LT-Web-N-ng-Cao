using Couppa.Api.Models.Requests;
using Couppa.Api.Models.ViewModels;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers.Admin;

/// <summary>SRS 15.3b — /Admin/Category/Index render View; Create/Edit/Delete/ToggleStatus trả JsonResult (modal AJAX).</summary>
[Route("Admin/Category/[action]")]
[Authorize(Policy = "RequireAdmin")]
public class AdminCategoryController : Controller
{
    private readonly ICategoryService _categoryService;

    public AdminCategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var pagedResult = await _categoryService.GetAllForAdminAsync(search, page, 50);
        return View(new AdminCategoriesViewModel { Categories = pagedResult.Items.ToList() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var category = await _categoryService.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, new { success = true, data = category });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _categoryService.UpdateAsync(id, request);
        return Json(new { success = true, data = category });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _categoryService.DeleteAsync(id);
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(long id, [FromBody] ChangeCategoryStatusRequest request)
    {
        var category = await _categoryService.ChangeStatusAsync(id, request.IsActive);
        return Json(new { success = true, data = category });
    }
}
