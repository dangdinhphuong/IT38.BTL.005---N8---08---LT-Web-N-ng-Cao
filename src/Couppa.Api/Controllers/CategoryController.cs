using Couppa.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers;

/// <summary>FR-CATEGORY-006/007 — public, chỉ trả category IsActive=true, dùng cho menu/dropdown filter (AJAX).</summary>
public class CategoryController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        var categories = await _categoryService.GetActiveListAsync();
        return Json(new { success = true, data = categories });
    }
}
