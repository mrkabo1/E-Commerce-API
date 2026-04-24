using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Domain.Entities
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public Cart? Cart { get; set; }
        public Product? Product { get; set; }
    }
}
