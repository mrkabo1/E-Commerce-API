
using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Application.DTOs
{
    public class CreateProductDTO
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int StockQuantity { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public string ImageUrl { get; set; }
    }
}
