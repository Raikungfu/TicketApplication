using System.ComponentModel.DataAnnotations.Schema;

namespace TicketApplication.Models
{
    public class OrderDetail : AuditableEntity
    {
        public string OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        public string ZoneId { get; set; }

        [ForeignKey("ZoneId")]
        public virtual Zone Zone { get; set; }

        public virtual ICollection<Ticket>? Tickets { get; set; }
    }
}
