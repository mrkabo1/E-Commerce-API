using E_Commerce.Application.DTOs;

namespace E_Commerce.Application.Contracts
{
    public interface IOrderService
    {
        public Task<OrderResponseDTO> CreateOrderAsync(Guid UserId);
        public Task<List<OrderResponseDTO>> GetAllOrdersAsync(Guid UserId); 
        public Task<OrderResponseDTO> GetOrderByIdAsync(Guid UserId, int OrderId);
        public Task<OrderResponseDTO> UpdateOrderStatusAsync(int orderId, string newStatus);
    }
}
