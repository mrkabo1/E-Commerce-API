using E_Commerce.Application.Contracts;
using E_Commerce.Application.DTOs;
using E_Commerce.Domain.Entities;
using E_Commerce.Infrastructure.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly E_CommerceDbContext _context;
        private readonly ILogger<OrderService> _logger;

        public OrderService(E_CommerceDbContext e_CommerceDbContext, ILogger<OrderService> logger)
        {
            _context = e_CommerceDbContext;
            _logger = logger;
        }

        public async Task<OrderResponseDTO> CreateOrderAsync(Guid userId)
        {
            _logger.LogInformation("Attempting to create order for user {UserId}", userId);

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("CreateOrderAsync called with empty UserId.");
                throw new ArgumentException("User ID must not be empty.", nameof(userId));
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                _logger.LogWarning("Cart is empty for user {UserId}. Cannot create order.", userId);
                throw new InvalidOperationException("Cart is empty.");
            }

            _logger.LogDebug("Cart found for user {UserId} with {ItemCount} items.", userId, cart.CartItems.Count);

            decimal totalPrice = 0;
            var orderItemsToAdd = new List<OrderItem>();

            foreach (var ci in cart.CartItems)
            {
                if (ci.Product == null)
                {
                    _logger.LogWarning("CartItem {CartItemId} has no associated product. Skipping.", ci.Id);
                    continue;
                }

                if (ci.Product.StockQuantity < ci.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for product '{ProductName}' (ID: {ProductId}). Available: {Available}, Requested: {Requested}",
                        ci.Product.Name, ci.Product.Id, ci.Product.StockQuantity, ci.Quantity);
                    throw new InvalidOperationException($"Insufficient stock for product '{ci.Product.Name}'. Available: {ci.Product.StockQuantity}");
                }

                totalPrice += ci.Quantity * ci.Product.Price;
                orderItemsToAdd.Add(new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price,
                    ProductName = ci.Product.Name
                });

                _logger.LogDebug("Product {ProductName} (Qty: {Quantity}) added to order. Running total: {TotalPrice}",
                    ci.Product.Name, ci.Quantity, totalPrice);
            }

            var order = new Order
            {
                UserId = userId,
                TotalPrice = totalPrice,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order created with ID {OrderId} for user {UserId}, total {TotalPrice:C}.",
                order.Id, userId, totalPrice);

            foreach (var item in orderItemsToAdd)
            {
                item.OrderId = order.Id;
            }
            await _context.OrderItems.AddRangeAsync(orderItemsToAdd);

            foreach (var ci in cart.CartItems)
            {
                if (ci.Product != null)
                {
                    ci.Product.StockQuantity -= ci.Quantity;
                    _logger.LogDebug("Stock for product {ProductName} reduced by {Quantity}. New stock: {NewStock}",
                        ci.Product.Name, ci.Quantity, ci.Product.StockQuantity);
                }
            }

            _context.CartItems.RemoveRange(cart.CartItems);
            _context.Carts.Remove(cart);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} completed. Cart cleared, stock updated.", order.Id);

            var itemDTOs = orderItemsToAdd.Select(oi => new OrderItemResponseDTO
            {
                ProductId = oi.ProductId,
                ProductName = oi.ProductName,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList();

            return new OrderResponseDTO
            {
                Id = order.Id,
                UserId = userId,
                TotalPrice = totalPrice,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
                Items = itemDTOs
            };
        }

        public async Task<List<OrderResponseDTO>> GetAllOrdersAsync(Guid userId)
        {
            _logger.LogInformation("Fetching all orders for user {UserId}", userId);

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("GetAllOrdersAsync called with empty UserId.");
                throw new ArgumentException("User ID must not be empty.", nameof(userId));
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .ToListAsync();

            _logger.LogDebug("Retrieved {OrderCount} orders for user {UserId}.", orders.Count, userId);

            return orders.Select(order => new OrderResponseDTO
            {
                Id = order.Id,
                UserId = order.UserId,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
                Items = order.OrderItems?.Select(item => new OrderItemResponseDTO
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList() ?? new List<OrderItemResponseDTO>()
            }).ToList();
        }

        public async Task<OrderResponseDTO> GetOrderByIdAsync(Guid userId, int orderId)
        {
            _logger.LogInformation("Fetching order {OrderId} for user {UserId}", orderId, userId);

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("GetOrderByIdAsync called with empty UserId.");
                throw new ArgumentException("User ID must not be empty.", nameof(userId));
            }

            if (orderId <= 0)
            {
                _logger.LogWarning("Invalid OrderId: {OrderId}.", orderId);
                throw new ArgumentException("Order ID must be valid.", nameof(orderId));
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for user {UserId}.", orderId, userId);
                throw new KeyNotFoundException("Order ID not found.");
            }

            _logger.LogDebug("Order {OrderId} found with {ItemCount} items.", orderId, order.OrderItems?.Count ?? 0);

            return new OrderResponseDTO
            {
                Id = order.Id,
                UserId = order.UserId,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
                Items = order.OrderItems?.Select(item => new OrderItemResponseDTO
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList() ?? new List<OrderItemResponseDTO>()
            };
        }

        public async Task<OrderResponseDTO> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            _logger.LogInformation("Updating order {OrderId} status to {NewStatus}.", orderId, newStatus);

            if (orderId <= 0)
            {
                _logger.LogWarning("Invalid OrderId: {OrderId}.", orderId);
                throw new ArgumentException("Order id must be valid.");
            }

            if (string.IsNullOrWhiteSpace(newStatus))
            {
                _logger.LogWarning("UpdateOrderStatusAsync called with empty newStatus.");
                throw new ArgumentException("Status must be valid.");
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for status update.", orderId);
                throw new KeyNotFoundException("Order not found.");
            }

            if (!Enum.TryParse<OrderStatus>(newStatus, true, out var status))
            {
                _logger.LogWarning("Invalid status value '{NewStatus}' for order {OrderId}.", newStatus, orderId);
                throw new ArgumentException("Invalid status value.");
            }

            if (status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                _logger.LogInformation("Order {OrderId} is being cancelled. Restoring product stocks.", orderId);
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        int originalStock = product.StockQuantity;
                        product.StockQuantity += item.Quantity;
                        _logger.LogDebug("Restored {Quantity} units of product {ProductName} (ID: {ProductId}). Stock from {OriginalStock} to {NewStock}.",
                            item.Quantity, product.Name, product.Id, originalStock, product.StockQuantity);
                    }
                    else
                    {
                        _logger.LogWarning("Product {ProductId} referenced in order {OrderId} does not exist. Cannot restore stock.", item.ProductId, orderId);
                    }
                }
            }

            var oldStatus = order.Status;
            order.Status = status;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} status changed from {OldStatus} to {NewStatus}.", orderId, oldStatus, status);

            return new OrderResponseDTO
            {
                Id = order.Id,
                UserId = order.UserId,
                CreatedAt = order.CreatedAt,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                Items = order.OrderItems?.Select(item => new OrderItemResponseDTO
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList() ?? new List<OrderItemResponseDTO>()
            };
        }
    }
}