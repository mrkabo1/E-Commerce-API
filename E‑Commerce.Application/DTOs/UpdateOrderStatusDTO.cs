using System.ComponentModel.DataAnnotations;


namespace E_Commerce.Application.DTOs
{
    public class UpdateOrderStatusDTO
    {
        [Required]
        public string Status { get; set; }
    }
}
