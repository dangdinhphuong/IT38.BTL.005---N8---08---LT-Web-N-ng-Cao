using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>FR-BROWSE-002..006 — public, search/filter/sort/paging. Không cache (quá nhiều tổ hợp tham số).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductSummaryResponse>>>> GetList([FromQuery] ProductListQuery query)
    {
        var result = await _productService.GetPublicListAsync(query);
        return Ok(ApiResponse<PagedResult<ProductSummaryResponse>>.Ok(result));
    }

    /// <summary>FR-BROWSE-001 — sản phẩm nổi bật (trang chủ).</summary>
    [HttpGet("featured")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductSummaryResponse>>>> GetFeatured()
    {
        var result = await _productService.GetFeaturedAsync();
        return Ok(ApiResponse<IReadOnlyList<ProductSummaryResponse>>.Ok(result));
    }

    /// <summary>FR-BROWSE-001 — 10 sản phẩm mới nhất (trang chủ).</summary>
    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductSummaryResponse>>>> GetLatest()
    {
        var result = await _productService.GetLatestAsync();
        return Ok(ApiResponse<IReadOnlyList<ProductSummaryResponse>>.Ok(result));
    }

    /// <summary>FR-BROWSE-003 — 404 nếu không tồn tại, đã xóa, hoặc Inactive (Admin xem riêng qua /api/admin/products/{id}).</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetById(long id)
    {
        var product = await _productService.GetPublicDetailAsync(id);
        return Ok(ApiResponse<ProductResponse>.Ok(product));
    }
}
