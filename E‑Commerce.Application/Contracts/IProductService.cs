using E_Commerce.Application.DTOs;

namespace E_Commerce.Application.Contracts
{
    public interface IProductService
    {
        public Task<PaginatedResult<ProductResponseDTO>> GetAllProducts(int pageNumber, int pageSize);
        public Task<ProductResponseDTO> GetProductById(int id);
        public Task<ProductResponseDTO> AddNewProduct(CreateProductDTO dto);
        public Task<ProductResponseDTO> UpdateProduct(int i, UpdateProductDTO dto);
        public Task<bool> DeleteProduct(int id);
    }
}
