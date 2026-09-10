using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Couppa.Api.Models.ViewModels;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers.Admin;

/// <summary>SRS 15.2b — /Admin/Product/Index render View; Create nhận multipart/form-data
/// để upload ảnh, các thao tác còn lại trả JSON cho AJAX.</summary>
[Route("Admin/Product/[action]")]
[Authorize(Policy = "RequireAdmin")]
public class AdminProductController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public AdminProductController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, long? category, string? status, int page = 1)
    {
        var pagedResult = await _productService.GetAdminListAsync(new AdminProductListQuery
        {
            Search = search,
            Category = category,
            Status = status,
            Page = page,
            PageSize = 50
        });
        var categories = await _categoryService.GetActiveListAsync();

        return View(new AdminProductsViewModel
        {
            Products = pagedResult.Items.ToList(),
            Categories = categories.ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(long id)
    {
        var product = await _productService.GetAdminDetailAsync(id);
        return View("~/Views/Product/Detail.cshtml", new ProductDetailViewModel
        {
            Product = product,
            RelatedProducts = new List<ProductSummaryResponse>()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateProductRequest request)
    {
        var product = await _productService.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, new { success = true, data = product });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, [FromBody] UpdateProductRequest request)
    {
        var product = await _productService.UpdateAsync(id, request);
        return Json(new { success = true, data = product });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _productService.DeleteAsync(id);
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(long id, [FromBody] ChangeProductStatusRequest request)
    {
        var product = await _productService.ChangeStatusAsync(id, request.IsActive);
        return Json(new { success = true, data = product });
    }
}
