using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Application.DTOs
{
    public class CreateCategoryDTO
    {
        [Required]
        public string Name { get; set; }
    }
}
