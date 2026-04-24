
namespace E_Commerce.Application.DTOs
{
    public class OrderResponseDTO
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemResponseDTO> Items { get; set; } = new();
    }
}
