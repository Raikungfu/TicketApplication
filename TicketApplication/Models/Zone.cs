using System.ComponentModel.DataAnnotations;

namespace TicketApplication.Models
{
    public class Zone : AuditableEntity
    {
      
        [Required]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        public int AvailableTickets { get; set; }
        public string EventId { get; set; }
        public virtual Event Event { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }
    }

}