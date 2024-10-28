namespace TicketApplication.Models
{
    public class CartItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
        public decimal Total => Price * Quantity;
    }
}
