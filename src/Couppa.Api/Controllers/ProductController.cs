using Couppa.Api.Models.Requests;
using Couppa.Api.Models.ViewModels;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers;

/// <summary>
/// DD-01: /Product/Index và /Product/Detail render Razor View (full page).
/// Search/Filter/Sort/Page/Featured/Latest trả JsonResult cho AJAX (Chương 12 SRS) — dùng để
/// cập nhật lại grid sản phẩm không reload trang.
/// </summary>
public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public ProductController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] long? category,
        [FromQuery] string? search,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string sort = "newest",
        [FromQuery] int page = 1)
    {
        var query = new ProductListQuery
        {
            Category = category,
            Search = search,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Sort = sort,
            Page = page,
            PageSize = 12
        };

        var pagedResult = await _productService.GetPublicListAsync(query);
        var categories = await _categoryService.GetActiveListAsync();

        var viewModel = new ProductListViewModel
        {
            Products = pagedResult.Items.ToList(),
            Categories = categories.ToList(),
            SelectedCategoryId = category,
            SearchKeyword = search,
            SortBy = sort ?? "newest",
            Page = pagedResult.Page,
            TotalPages = pagedResult.TotalPages,
            TotalItems = pagedResult.TotalItems
        };

        return View(viewModel);
    }

    /// <summary>AJAX: tìm kiếm/lọc/sắp xếp/phân trang không reload — trả cùng shape với Index (JSON).</summary>
    [HttpGet]
    public async Task<IActionResult> Search(string? keyword, long? category, string sort = "newest", int page = 1)
        => await FilteredJsonAsync(keyword, category, null, null, sort, page);

    [HttpGet]
    public async Task<IActionResult> Filter(long? category, decimal? minPrice, decimal? maxPrice, string sort = "newest", int page = 1)
        => await FilteredJsonAsync(null, category, minPrice, maxPrice, sort, page);

    [HttpGet]
    public async Task<IActionResult> Sort(string sort, long? category = null, string? search = null, int page = 1)
        => await FilteredJsonAsync(search, category, null, null, sort, page);

    [HttpGet]
    public async Task<IActionResult> Page(int page, long? category = null, string? search = null, string sort = "newest")
        => await FilteredJsonAsync(search, category, null, null, sort, page);

    private async Task<IActionResult> FilteredJsonAsync(
        string? search, long? category, decimal? minPrice, decimal? maxPrice, string? sort, int page)
    {
        var result = await _productService.GetPublicListAsync(new ProductListQuery
        {
            Search = search,
            Category = category,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Sort = sort,
            Page = page,
            PageSize = 12
        });

        return Json(new { success = true, data = result });
    }

    [HttpGet]
    public async Task<IActionResult> Featured()
    {
        var result = await _productService.GetFeaturedAsync();
        return Json(new { success = true, data = result });
    }

    [HttpGet]
    public async Task<IActionResult> Latest()
    {
        var result = await _productService.GetLatestAsync();
        return Json(new { success = true, data = result });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(long id)
    {
        try
        {
            var product = await _productService.GetPublicDetailAsync(id);
            var featured = await _productService.GetFeaturedAsync();

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = featured.Where(p => p.Id != id).Take(4).ToList()
            };

            return View(viewModel);
        }
        catch (Middleware.AppException)
        {
            TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc đã bị gỡ bỏ.";
            return RedirectToAction(nameof(Index));
        }
    }
}
