using System.ComponentModel.DataAnnotations.Schema;

namespace TicketApplication.Models
{
    public class OrderDetail
    {
        public string OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public string TicketId { get; set; }
        public virtual Ticket? Ticket { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
