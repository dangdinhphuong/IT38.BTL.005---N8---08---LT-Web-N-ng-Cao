using Couppa.Api.Models.Responses;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Policy = "RequireAdmin")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// FR-REPORT-004 — Top 10 sản phẩm được thêm giỏ nhiều nhất. Query from/to mặc định 30 ngày gần nhất.
    /// FR-REPORT-001/002/003 không có endpoint riêng - dùng chung dữ liệu đã lộ ra ở
    /// GET /api/admin/dashboard/summary (productsByCategory, outOfStockProducts, totalUsers) theo đúng
    /// ghi chú tái sử dụng ở task 11.
    /// </summary>
    [HttpGet("cart-top-products")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CartTopProductResponse>>>> GetCartTopProducts(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        var result = await _reportService.GetCartTopProductsAsync(from, to);
        return Ok(ApiResponse<IReadOnlyList<CartTopProductResponse>>.Ok(result));
    }
}
