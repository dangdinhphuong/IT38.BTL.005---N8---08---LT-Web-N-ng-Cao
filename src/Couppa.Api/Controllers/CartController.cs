using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers;

/// <summary>
/// DD-02: KHÔNG [Authorize] ở mức class - Guest chưa đăng nhập vẫn thao tác giỏ hàng được qua
/// Session cookie (couppa.session), ICartService tự phân biệt Guest/User qua ICurrentUserService.
/// </summary>
[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>FR-CART-002.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CartResponse>>> GetCurrentCart()
    {
        var cart = await _cartService.GetCurrentCartAsync();
        return Ok(ApiResponse<CartResponse>.Ok(cart));
    }

    /// <summary>FR-CART-001, UC-02.</summary>
    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<CartResponse>>> AddItem([FromBody] AddCartItemRequest request)
    {
        var cart = await _cartService.AddItemAsync(request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CartResponse>.Ok(cart));
    }

    /// <summary>FR-CART-003/004.</summary>
    [HttpPut("items/{id:long}")]
    public async Task<ActionResult<ApiResponse<CartResponse>>> UpdateItemQuantity(
        long id, [FromBody] UpdateCartItemRequest request)
    {
        var cart = await _cartService.UpdateItemQuantityAsync(id, request);
        return Ok(ApiResponse<CartResponse>.Ok(cart));
    }

    /// <summary>FR-CART-005.</summary>
    [HttpDelete("items/{id:long}")]
    public async Task<ActionResult<ApiResponse<CartResponse>>> RemoveItem(long id)
    {
        var cart = await _cartService.RemoveItemAsync(id);
        return Ok(ApiResponse<CartResponse>.Ok(cart));
    }

    /// <summary>FR-CART-006.</summary>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<object>>> ClearCart()
    {
        await _cartService.ClearCartAsync();
        return Ok(ApiResponse<object>.Ok(new { success = true }));
    }
}
