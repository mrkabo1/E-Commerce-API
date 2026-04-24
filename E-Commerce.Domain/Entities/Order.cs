using E_Commerce.Domain.Identity;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Domain.Entities
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
