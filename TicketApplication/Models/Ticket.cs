using System.ComponentModel.DataAnnotations.Schema;

namespace TicketApplication.Models
{
    public class Ticket : AuditableEntity
    {
        public string Title { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public string? Image { get; set; }

        public int ZoneId { get; set; }

        [ForeignKey("ZoneId")]
        public virtual Zone? Zone { get; set; }

        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }
        public virtual ICollection<Cart>? Carts { get; set; }
    }
}

