using Couppa.Api.Models.Requests;
using Couppa.Api.Models.ViewModels;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers;

/// <summary>
/// DD-01: /Cart/Index render View (SRS 15.4). Add/Update/Remove/Clear trả JsonResult, gọi qua
/// AJAX (Chương 12) — không reload trang giỏ hàng.
/// </summary>
public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cartResponse = await _cartService.GetCurrentCartAsync();
        return View(new CartViewModel { Cart = cartResponse });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
    {
        var cart = await _cartService.AddItemAsync(request);
        return Json(new { success = true, data = cart });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem(long id, [FromBody] UpdateCartItemRequest request)
    {
        var cart = await _cartService.UpdateItemQuantityAsync(id, request);
        return Json(new { success = true, data = cart });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(long id)
    {
        var cart = await _cartService.RemoveItemAsync(id);
        return Json(new { success = true, data = cart });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        await _cartService.ClearCartAsync();
        return Json(new { success = true });
    }
}
