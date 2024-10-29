using System.ComponentModel.DataAnnotations.Schema;

namespace TicketApplication.Models
{
    public class Ticket : AuditableEntity
    {
        public string Title { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public string? Image { get; set; }

        public string ZoneId { get; set; }

        [ForeignKey("ZoneId")]
        public virtual Zone? Zone { get; set; }

        public string OrderDetailId { get; set; }

        [ForeignKey("OrderDetailId")]
        public virtual OrderDetail? OrderDetail { get; set; }
    }
}

