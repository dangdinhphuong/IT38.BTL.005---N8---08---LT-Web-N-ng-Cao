using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Policy = "RequireAdmin")]
public class AdminCategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public AdminCategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CategoryResponse>>>> GetAll(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _categoryService.GetAllForAdminAsync(search, page, pageSize);
        return Ok(ApiResponse<PagedResult<CategoryResponse>>.Ok(result));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetById(long id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        return Ok(ApiResponse<CategoryResponse>.Ok(category));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Create([FromBody] CreateCategoryRequest request)
    {
        var category = await _categoryService.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CategoryResponse>.Ok(category));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Update(long id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _categoryService.UpdateAsync(id, request);
        return Ok(ApiResponse<CategoryResponse>.Ok(category));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id)
    {
        await _categoryService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { success = true }));
    }

    [HttpPatch("{id:long}/status")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> ChangeStatus(long id, [FromBody] ChangeCategoryStatusRequest request)
    {
        var category = await _categoryService.ChangeStatusAsync(id, request.IsActive);
        return Ok(ApiResponse<CategoryResponse>.Ok(category));
    }
}
