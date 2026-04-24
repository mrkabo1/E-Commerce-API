using E_Commerce.Application.Contracts;
using E_Commerce.Application.DTOs;
using E_Commerce.Domain.Entities;
using E_Commerce.Infrastructure.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly E_CommerceDbContext _e_CommerceDbContext;
        private readonly ILogger<ProductService> _logger;

        public ProductService(E_CommerceDbContext context, ILogger<ProductService> logger)
        {
            _e_CommerceDbContext = context;
            _logger = logger;
        }

        public async Task<PaginatedResult<ProductResponseDTO>> GetAllProducts(int pageNumber, int pageSize)
        {
            _logger.LogInformation("Getting all products: Page {PageNumber}, Size {PageSize}.", pageNumber, pageSize);

            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var totalCount = await _e_CommerceDbContext.Products.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var products = await _e_CommerceDbContext.Products
                .Select(p => new ProductResponseDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    CategoryId = p.CategoryId,
                    ImageUrl = p.ImageUrl,
                    CategoryName = p.Category != null ? p.Category.Name : null
                })
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogDebug("Retrieved {ProductCount} products (page {PageNumber} of {TotalPages}).", products.Count, pageNumber, totalPages);

            return new PaginatedResult<ProductResponseDTO>
            {
                Data = products,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<ProductResponseDTO> GetProductById(int id)
        {
            _logger.LogInformation("Fetching product by ID {ProductId}.", id);

            if (id <= 0)
            {
                _logger.LogWarning("GetProductById called with invalid ID {ProductId}.", id);
                return null;
            }

            var product = await _e_CommerceDbContext.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found.", id);
                return null;
            }

            _logger.LogDebug("Found product: {ProductName} (ID {ProductId}), Category: {CategoryName}.",
                product.Name, product.Id, product.Category?.Name ?? "None");

            return new ProductResponseDTO
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                ImageUrl = product.ImageUrl,
                CategoryName = product.Category?.Name
            };
        }

        public async Task<ProductResponseDTO> AddNewProduct(CreateProductDTO dto)
        {
            _logger.LogInformation("Adding new product with name '{ProductName}'.", dto.Name);

            var existingProduct = await _e_CommerceDbContext.Products
                .FirstOrDefaultAsync(p => p.Name.ToLower() == dto.Name.ToLower());
            if (existingProduct != null)
            {
                _logger.LogWarning("Product with name '{ProductName}' already exists (ID {ExistingId}).", dto.Name, existingProduct.Id);
                throw new InvalidOperationException("A Product with this name already exists.");
            }

            var category = await _e_CommerceDbContext.Categories.FindAsync(dto.CategoryId);
            if (category == null)
            {
                _logger.LogWarning("Category with ID {CategoryId} does not exist for new product.", dto.CategoryId);
                throw new InvalidOperationException($"Category with ID {dto.CategoryId} does not exist.");
            }

            var newProduct = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                CategoryId = dto.CategoryId,
                StockQuantity = dto.StockQuantity
            };

            await _e_CommerceDbContext.Products.AddAsync(newProduct);
            await _e_CommerceDbContext.SaveChangesAsync();

            _logger.LogInformation("Product '{ProductName}' created with ID {ProductId}, Category '{CategoryName}'.",
                newProduct.Name, newProduct.Id, category.Name);

            return new ProductResponseDTO
            {
                Id = newProduct.Id,
                Name = newProduct.Name,
                Description = newProduct.Description,
                Price = newProduct.Price,
                StockQuantity = newProduct.StockQuantity,
                CategoryId = newProduct.CategoryId,
                ImageUrl = newProduct.ImageUrl,
                CategoryName = category.Name
            };
        }

        public async Task<ProductResponseDTO> UpdateProduct(int id, UpdateProductDTO dto)
        {
            _logger.LogInformation("Updating product ID {ProductId} with new name '{NewName}'.", id, dto.Name);

            if (id <= 0)
            {
                _logger.LogWarning("UpdateProduct called with invalid ID {ProductId}.", id);
                throw new ArgumentException("Enter valid id", nameof(id));
            }

            var existingProduct = await _e_CommerceDbContext.Products.FindAsync(id);
            if (existingProduct == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for update.", id);
                throw new KeyNotFoundException($"Product with ID {id} not found.");
            }

            var category = await _e_CommerceDbContext.Categories.FindAsync(dto.CategoryId);
            if (category == null)
            {
                _logger.LogWarning("Category with ID {CategoryId} does not exist for product update.", dto.CategoryId);
                throw new ArgumentException($"Category with ID {dto.CategoryId} does not exist.", nameof(dto.CategoryId));
            }

            var duplicateProduct = await _e_CommerceDbContext.Products
                .FirstOrDefaultAsync(p => p.Name.ToLower() == dto.Name.ToLower() && p.Id != id);
            if (duplicateProduct != null)
            {
                _logger.LogWarning("Update failed: product name '{NewName}' already used by product ID {DuplicateId}.", dto.Name, duplicateProduct.Id);
                throw new InvalidOperationException("A product with this name already exists.");
            }

            string oldName = existingProduct.Name;
            existingProduct.Name = dto.Name;
            existingProduct.Price = dto.Price;
            existingProduct.StockQuantity = dto.StockQuantity;
            existingProduct.ImageUrl = dto.ImageUrl;
            existingProduct.CategoryId = dto.CategoryId;
            existingProduct.Description = dto.Description;

            await _e_CommerceDbContext.SaveChangesAsync();

            _logger.LogInformation("Product ID {ProductId} updated: name from '{OldName}' to '{NewName}', category to '{CategoryName}'.",
                id, oldName, dto.Name, category.Name);

            return new ProductResponseDTO
            {
                Id = existingProduct.Id,
                Name = existingProduct.Name,
                Description = existingProduct.Description,
                Price = existingProduct.Price,
                StockQuantity = existingProduct.StockQuantity,
                CategoryId = existingProduct.CategoryId,
                ImageUrl = existingProduct.ImageUrl,
                CategoryName = category.Name
            };
        }

        public async Task<bool> DeleteProduct(int id)
        {
            _logger.LogInformation("Deleting product ID {ProductId}.", id);

            if (id <= 0)
            {
                _logger.LogWarning("DeleteProduct called with invalid ID {ProductId}.", id);
                throw new ArgumentException("Enter Valid id", nameof(id));
            }

            var product = await _e_CommerceDbContext.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for deletion.", id);
                throw new KeyNotFoundException($"Product with ID {id} not found.");
            }

            _e_CommerceDbContext.Products.Remove(product);
            await _e_CommerceDbContext.SaveChangesAsync();

            _logger.LogInformation("Product '{ProductName}' (ID {ProductId}) deleted successfully.", product.Name, id);
            return true;
        }
    }
}