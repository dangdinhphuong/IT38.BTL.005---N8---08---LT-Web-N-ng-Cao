using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/products")]
[Authorize(Policy = "RequireAdmin")]
public class AdminProductController : ControllerBase
{
    private readonly IProductService _productService;

    public AdminProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductSummaryResponse>>>> GetList([FromQuery] AdminProductListQuery query)
    {
        var result = await _productService.GetAdminListAsync(query);
        return Ok(ApiResponse<PagedResult<ProductSummaryResponse>>.Ok(result));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetById(long id)
    {
        var product = await _productService.GetAdminDetailAsync(id);
        return Ok(ApiResponse<ProductResponse>.Ok(product));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Create([FromBody] CreateProductRequest request)
    {
        var product = await _productService.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ProductResponse>.Ok(product));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Update(long id, [FromBody] UpdateProductRequest request)
    {
        var product = await _productService.UpdateAsync(id, request);
        return Ok(ApiResponse<ProductResponse>.Ok(product));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id)
    {
        await _productService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { success = true }));
    }

    [HttpPatch("{id:long}/status")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> ChangeStatus(long id, [FromBody] ChangeProductStatusRequest request)
    {
        var product = await _productService.ChangeStatusAsync(id, request.IsActive);
        return Ok(ApiResponse<ProductResponse>.Ok(product));
    }
}
