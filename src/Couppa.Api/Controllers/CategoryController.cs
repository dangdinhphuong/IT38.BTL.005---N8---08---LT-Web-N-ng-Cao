using Couppa.Api.Models.Responses;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>FR-CATEGORY-006/007 — public, không cần đăng nhập, chỉ trả category IsActive = true.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryResponse>>>> GetActiveList()
    {
        var categories = await _categoryService.GetActiveListAsync();
        return Ok(ApiResponse<IReadOnlyList<CategoryResponse>>.Ok(categories));
    }
}
