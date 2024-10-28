namespace TicketApplication.Models
{
    public class ShoppingCart
    {
        public List<Cart> Items { get; set; } = new List<Cart>();
        public decimal Total => Items.Sum(item => item.Ticket.Price * item.Quantity);
    }

}
