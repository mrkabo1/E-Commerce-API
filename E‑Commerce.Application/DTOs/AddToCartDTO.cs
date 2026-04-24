
using System.ComponentModel.DataAnnotations;


namespace E_Commerce.Application.DTOs
{
    public class AddToCartDTO
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        public int Quantity { get; set; }
    }
}
