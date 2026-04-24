using E_Commerce.Application.Contracts;
using E_Commerce.Application.DTOs;
using E_Commerce.Domain.Entities;
using E_Commerce.Infrastructure.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Infrastructure.Services
{
    public class CartService : ICartService
    {
        private readonly E_CommerceDbContext _context;
        private readonly ILogger<CartService> _logger;

        public CartService(E_CommerceDbContext e_CommerceDbContext, ILogger<CartService> logger)
        {
            _context = e_CommerceDbContext;
            _logger = logger;
        }

        // ---------- Private Helpers ----------
        private async Task<Cart> GetOrCreateCartAsync(Guid userId)
        {
            _logger.LogDebug("Getting or creating cart for user {UserId}.", userId);
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new cart for user {UserId} with CartId {CartId}.", userId, cart.Id);
            }
            return cart;
        }

        private async Task<CartResponseDTO> MapToCartResponseAsync(Cart cart)
        {
            _logger.LogDebug("Mapping cart {CartId} to response DTO.", cart.Id);
            var cartItems = await _context.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .Include(ci => ci.Product)
                .ToListAsync();

            var itemDTOs = new List<CartItemResponseDTO>();
            int totalItems = 0;
            decimal subtotal = 0;

            foreach (var ci in cartItems)
            {
                if (ci.Product == null)
                {
                    _logger.LogWarning("CartItem {CartItemId} has no associated product. Skipping.", ci.Id);
                    continue;
                }
                var lineSubtotal = ci.Quantity * ci.Product.Price;
                itemDTOs.Add(new CartItemResponseDTO
                {
                    ProductId = ci.ProductId,
                    Name = ci.Product.Name,
                    UnitPrice = ci.Product.Price,
                    Quantity = ci.Quantity,
                    Subtotal = lineSubtotal
                });
                totalItems += ci.Quantity;
                subtotal += lineSubtotal;
            }

            const decimal taxRate = 0.08m;
            decimal tax = subtotal * taxRate;
            decimal shippingCost = subtotal > 50 ? 0 : 5;
            decimal grandTotal = subtotal + tax + shippingCost;

            _logger.LogDebug("Cart {CartId} mapped: {TotalItems} items, subtotal {Subtotal:C}, grand total {GrandTotal:C}.",
                cart.Id, totalItems, subtotal, grandTotal);

            return new CartResponseDTO
            {
                CartId = cart.Id,
                Items = itemDTOs,
                TotalItems = totalItems,
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                Tax = tax,
                GrandTotal = grandTotal
            };
        }

        // ---------- Public Methods ----------
        public async Task<CartResponseDTO?> GetCartAsync(Guid userId)
        {
            _logger.LogInformation("Fetching cart for user {UserId}.", userId);
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("GetCartAsync called with empty UserId.");
                throw new ArgumentException("User ID must not be empty.", nameof(userId));
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                _logger.LogInformation("No cart found for user {UserId}. Returning null.", userId);
                return null;
            }

            return await MapToCartResponseAsync(cart);
        }

        public async Task<CartResponseDTO> AddToCartAsync(Guid userId, int productId, int quantity)
        {
            _logger.LogInformation("Adding to cart: User {UserId}, Product {ProductId}, Quantity {Quantity}.", userId, productId, quantity);

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("AddToCartAsync called with empty UserId.");
                throw new ArgumentException("User ID must not be empty.", nameof(userId));
            }
            if (productId <= 0)
            {
                _logger.LogWarning("Invalid ProductId: {ProductId}.", productId);
                throw new ArgumentException("Product ID must be valid.", nameof(productId));
            }
            if (quantity <= 0)
            {
                _logger.LogWarning("Invalid quantity: {Quantity}.", quantity);
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found.", productId);
                throw new KeyNotFoundException($"Product with ID {productId} not found.");
            }

            var cart = await GetOrCreateCartAsync(userId);
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);

            if (existingItem != null)
            {
                int newTotal = existingItem.Quantity + quantity;
                _logger.LogDebug("Product already in cart. Current quantity {Current}, new total would be {NewTotal}.", existingItem.Quantity, newTotal);
                if (product.StockQuantity < newTotal)
                {
                    _logger.LogWarning("Stock insufficient for product {ProductId}. Stock: {Stock}, requested total: {NewTotal}",
                        productId, product.StockQuantity, newTotal);
                    throw new InvalidOperationException($"Cannot add {quantity}. Only {product.StockQuantity} in stock, you already have {existingItem.Quantity} in cart.");
                }
                existingItem.Quantity = newTotal;
                _logger.LogInformation("Updated quantity for product {ProductId} in cart to {NewTotal}.", productId, newTotal);
            }
            else
            {
                if (product.StockQuantity < quantity)
                {
                    _logger.LogWarning("Stock insufficient for new product {ProductId}. Stock: {Stock}, requested: {Quantity}",
                        productId, product.StockQuantity, quantity);
                    throw new InvalidOperationException($"Only {product.StockQuantity} in stock.");
                }
                await _context.CartItems.AddAsync(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                });
                _logger.LogInformation("Added product {ProductId} to cart with quantity {Quantity}.", productId, quantity);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cart updated for user {UserId}.", userId);
            return await MapToCartResponseAsync(cart);
        }

        public async Task<CartResponseDTO> UpdateCartItemQuantityAsync(Guid userId, int productId, int quantity)
        {
            _logger.LogInformation("Updating cart item quantity: User {UserId}, Product {ProductId}, New Quantity {Quantity}.", userId, productId, quantity);

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("UpdateCartItemQuantityAsync called with empty UserId.");
                throw new ArgumentException("User ID must not be empty.", nameof(userId));
            }
            if (productId <= 0)
            {
                _logger.LogWarning("Invalid ProductId: {ProductId}.", productId);
                throw new ArgumentException("Product ID must be valid.", nameof(productId));
            }
            if (quantity <= 0)
            {
                _logger.LogWarning("Invalid quantity: {Quantity}.", quantity);
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                _logger.LogWarning("Cart not found for user {UserId}.", userId);
                throw new KeyNotFoundException("Cart not found.");
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);
            if (cartItem == null)
            {
                _logger.LogWarning("Product {ProductId} not found in cart {CartId}.", productId, cart.Id);
                throw new KeyNotFoundException("Product not in cart.");
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} does not exist.", productId);
                throw new KeyNotFoundException($"Product {productId} not found.");
            }
            if (product.StockQuantity < quantity)
            {
                _logger.LogWarning("Stock insufficient for product {ProductId}. Stock: {Stock}, requested: {Quantity}",
                    productId, product.StockQuantity, quantity);
                throw new InvalidOperationException($"Only {product.StockQuantity} in stock.");
            }

            cartItem.Quantity = quantity;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated quantity for product {ProductId} in cart to {Quantity}.", productId, quantity);
            return await MapToCartResponseAsync(cart);
        }

        public async Task<CartResponseDTO> RemoveFromCartAsync(Guid userId, int productId)
        {
            _logger.LogInformation("Removing product {ProductId} from cart for user {UserId}.", productId, userId);

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("RemoveFromCartAsync called with empty UserId.");
                throw new ArgumentException("User ID must not be empty.", nameof(userId));
            }
            if (productId <= 0)
            {
                _logger.LogWarning("Invalid ProductId: {ProductId}.", productId);
                throw new ArgumentException("Product ID must be valid.", nameof(productId));
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                _logger.LogWarning("Cart not found for user {UserId}.", userId);
                throw new KeyNotFoundException("Cart not found.");
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} does not exist.", productId);
                throw new KeyNotFoundException($"Product {productId} not found.");
            }

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);
            if (cartItem == null)
            {
                _logger.LogWarning("Product {ProductId} not found in cart {CartId}.", productId, cart.Id);
                throw new KeyNotFoundException($"Cart Item with product id: {productId}, and Cart id: {cart.Id}, not found.");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed product {ProductId} from cart {CartId}.", productId, cart.Id);
            return await MapToCartResponseAsync(cart);
        }

        public async Task ClearCartAsync(Guid userId)
        {
            _logger.LogInformation("Clearing cart for user {UserId}.", userId);

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("ClearCartAsync called with empty UserId.");
                throw new ArgumentException("User ID must not be empty.", nameof(userId));
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                _logger.LogInformation("No cart found for user {UserId}. Nothing to clear.", userId);
                return;
            }

            var cartItems = _context.CartItems.Where(ci => ci.CartId == cart.Id);
            int itemCount = await cartItems.CountAsync();
            _context.CartItems.RemoveRange(cartItems);
            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleared cart {CartId} for user {UserId}, removed {ItemCount} items.", cart.Id, userId, itemCount);
        }
    }
}