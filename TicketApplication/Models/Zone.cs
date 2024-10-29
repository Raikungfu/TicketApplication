using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketApplication.Models
{
    public class Zone : AuditableEntity
    {
        [Key]
        public int ZoneId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        public int AvailableTickets { get; set; }

        public string EventId { get; set; }

        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        public virtual ICollection<Ticket>? Tickets { get; set; }
    }

}