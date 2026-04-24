using E_Commerce.Domain.Identity;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Domain.Entities
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        public Guid UserId { get; set; } 

        public ApplicationUser? User { get; set; }
        public ICollection<CartItem>? CartItems { get; set; }
    }
}
