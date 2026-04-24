using E_Commerce.Application.DTOs;


namespace E_Commerce.Application.Contracts
{
    public interface ICartService
    {
        public Task<CartResponseDTO> GetCartAsync(Guid id);
        public Task<CartResponseDTO> AddToCartAsync(Guid userId, int productId, int quantity);
        Task<CartResponseDTO> UpdateCartItemQuantityAsync(Guid userId, int productId, int quantity);
        Task<CartResponseDTO> RemoveFromCartAsync(Guid userId, int productId);
        Task ClearCartAsync(Guid userId);
    }
}
