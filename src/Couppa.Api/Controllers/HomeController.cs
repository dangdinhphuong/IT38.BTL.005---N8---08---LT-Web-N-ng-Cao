using Couppa.Api.Models.ViewModels;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers;

/// <summary>SRS 15.1b — trang chủ, đọc cache products:featured / products:latest (Chương 11, FR-BROWSE-001).</summary>
public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public HomeController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetActiveListAsync();
        var featuredProducts = await _productService.GetFeaturedAsync();
        var latestProducts = await _productService.GetLatestAsync();

        var viewModel = new HomeViewModel
        {
            Categories = categories.ToList(),
            FeaturedProducts = featuredProducts.ToList(),
            LatestProducts = latestProducts.ToList()
        };

        return View(viewModel);
    }
}
