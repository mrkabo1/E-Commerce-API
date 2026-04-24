using System.ComponentModel.DataAnnotations;


namespace E_Commerce.Application.DTOs
{
    public class UpdateCategoryDTO
    {
        [Required]
        public string Name { get; set; }
    }
}
