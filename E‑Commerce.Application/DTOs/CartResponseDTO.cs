

namespace E_Commerce.Application.DTOs
{
    public class CartResponseDTO
    {
        public int CartId { get; set; }
        public List<CartItemResponseDTO> Items { get; set; }
        public int TotalItems { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Tax {  get; set; }
        public decimal GrandTotal { get; set; }
    }
}
