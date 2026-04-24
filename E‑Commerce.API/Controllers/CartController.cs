using E_Commerce.Application.Contracts;
using E_Commerce.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        /// <summary>
        /// To Get The cart of current user.
        /// </summary>
        
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var cart = await _cartService.GetCartAsync(Guid.Parse(userId));
                if (cart == null)
                {
                    return Ok(new CartResponseDTO
                    {
                        CartId = 0,
                        Items = new List<CartItemResponseDTO>(),
                        TotalItems = 0,
                        Subtotal = 0,
                        ShippingCost = 0,
                        Tax = 0,
                        GrandTotal = 0
                    });
                }
                return Ok(cart);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// To Add to cart of Current user.
        /// </summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var cart = await _cartService.AddToCartAsync(Guid.Parse(userId), dto.ProductId, dto.Quantity);
                return Ok(cart);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }
        
        /// <summary>
        /// To update product quantity from Cart items of the current user.
        /// </summary>
        [HttpPut("items")]
        public async Task<IActionResult> UpdateCartItemQuantity([FromBody] AddToCartDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var cart = await _cartService.UpdateCartItemQuantityAsync(Guid.Parse(userId), dto.ProductId, dto.Quantity);
                return Ok(cart);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
        /// <summary>
        /// To remove product from Cart items of the current user.
        /// </summary>
        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> RemoveProductCartItem(int productId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var cart = await _cartService.RemoveFromCartAsync(Guid.Parse(userId), productId);
                return Ok(cart);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        /// <summary>
        /// To clear Cart of the current user.
        /// </summary>
        [HttpDelete()]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                await _cartService.ClearCartAsync(Guid.Parse(userId));
                return NoContent();
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}
