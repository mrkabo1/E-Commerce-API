using E_Commerce.Application.DTOs;
using E_Commerce.Domain.Entities;

namespace E_Commerce.Application.Contracts
{
    public interface ICategoryService
    {
        public Task<PaginatedResult<Category>> GetCategoriesPaginatedAsync(int pageNumber, int pageSize);
        public Task<Category?> GetCategoryByIdAsync(int id);

        public Task<Category> CreateCategoryAsync(CreateCategoryDTO dto);

        public Task<bool> DeleteCategoryAsync(int id);
        public Task<Category> UpdateCategoryAsync(int id, UpdateCategoryDTO dto);
    }
}
