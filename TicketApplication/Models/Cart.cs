using System.ComponentModel.DataAnnotations.Schema;

namespace TicketApplication.Models
{
    public class Cart
    {
        public string UserId { get; set; }

        public string TicketId { get; set; }

        public int Quantity { get; set; }

        public virtual Ticket? Ticket { get; set; }
        public virtual User? User { get; set; }

        [NotMapped]
        public decimal TotalPrice => Ticket.Zone.Price * Quantity;
    }
}
