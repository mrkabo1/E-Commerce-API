using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Domain.Entities
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;


        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}
