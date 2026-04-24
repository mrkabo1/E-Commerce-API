using E_Commerce.Application.Contracts;
using E_Commerce.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace E_Commerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// to get all categories
        /// </summary>

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 10)
        {
            var result = await _categoryService.GetCategoriesPaginatedAsync(pageNumber, pageSize);
            return Ok(result);
        }
        /// <summary>
        /// to get a category by id (Admin Only)
        /// </summary>

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound();
            return Ok(category);
        }
        /// <summary>
        /// to create a new category (Admin Only)
        /// </summary>

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdCategory = await _categoryService.CreateCategoryAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = createdCategory.Id }, createdCategory);
            }
            catch (InvalidOperationException ex)
            {
                // This catches the duplicate name exception
                return Conflict(ex.Message);
            }
        }

        /// <summary>
        /// to delete an exist category by id  (Admin Only)
        /// </summary>

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid category ID.");

            try
            {
                var deleted = await _categoryService.DeleteCategoryAsync(id);
                if (!deleted)
                    return NotFound($"Category with ID {id} not found.");

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                // This catches the "category has associated products" exception
                return Conflict(new { message = ex.Message });
            }
        }
        /// <summary>
        /// to update an existing Category (Admin Only)
        /// </summary>

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateCategoryDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updatedCategory = await _categoryService.UpdateCategoryAsync(id, dto);
                return Ok(updatedCategory);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Category with ID {id} not found.");
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }
    }
}
