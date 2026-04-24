using E_Commerce.Application.Contracts;
using E_Commerce.Application.DTOs;
using E_Commerce.Domain.Entities;
using E_Commerce.Infrastructure.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly E_CommerceDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(E_CommerceDbContext e_CommerceDbContext, ILogger<CategoryService> logger)
        {
            _context = e_CommerceDbContext;
            _logger = logger;
        }

        public async Task<PaginatedResult<Category>> GetCategoriesPaginatedAsync(int pageNumber, int pageSize)
        {
            _logger.LogInformation("Getting paginated categories: Page {PageNumber}, Size {PageSize}.", pageNumber, pageSize);

            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var totalCount = await _context.Categories.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var categories = await _context.Categories
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new Category
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            _logger.LogDebug("Retrieved {CategoryCount} categories (page {PageNumber} of {TotalPages}).", categories.Count, pageNumber, totalPages);

            return new PaginatedResult<Category>
            {
                Data = categories,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            _logger.LogInformation("Fetching category by ID {CategoryId}.", id);

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category with ID {CategoryId} not found.", id);
                return null;
            }

            _logger.LogDebug("Found category: {CategoryName} (ID {CategoryId}).", category.Name, id);
            return new Category
            {
                Id = category.Id,
                Name = category.Name
            };
        }

        public async Task<Category> CreateCategoryAsync(CreateCategoryDTO dto)
        {
            _logger.LogInformation("Creating new category with name '{CategoryName}'.", dto.Name);

            var existing = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower());
            if (existing != null)
            {
                _logger.LogWarning("Category with name '{CategoryName}' already exists (ID {ExistingId}).", dto.Name, existing.Id);
                throw new InvalidOperationException("A category with this name already exists.");
            }

            var category = new Category { Name = dto.Name };
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category created with ID {CategoryId}, Name '{CategoryName}'.", category.Id, category.Name);
            return new Category
            {
                Id = category.Id,
                Name = category.Name
            };
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            _logger.LogInformation("Attempting to delete category ID {CategoryId}.", id);

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category ID {CategoryId} not found for deletion.", id);
                return false;
            }

            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                _logger.LogWarning("Cannot delete category ID {CategoryId} because it has associated products.", id);
                throw new InvalidOperationException("Cannot delete category because it has associated products. Please delete or reassign those products first.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category ID {CategoryId} ('{CategoryName}') deleted successfully.", id, category.Name);
            return true;
        }

        public async Task<Category> UpdateCategoryAsync(int id, UpdateCategoryDTO dto)
        {
            _logger.LogInformation("Updating category ID {CategoryId} to new name '{NewName}'.", id, dto.Name);

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category ID {CategoryId} not found for update.", id);
                throw new KeyNotFoundException($"Category with ID {id} not found.");
            }

            var duplicate = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.Id != id);
            if (duplicate)
            {
                _logger.LogWarning("Update failed: name '{NewName}' already used by another category.", dto.Name);
                throw new InvalidOperationException("A category with this name already exists.");
            }

            string oldName = category.Name;
            category.Name = dto.Name;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category ID {CategoryId} renamed from '{OldName}' to '{NewName}'.", id, oldName, dto.Name);
            return new Category
            {
                Id = category.Id,
                Name = category.Name
            };
        }
    }
}