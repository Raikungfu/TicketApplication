using System.ComponentModel.DataAnnotations;

namespace TicketApplication.Models
{
    public class Zone : AuditableEntity
    {
      
        [Required]
        public string Name { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int AvailableTickets { get; set; }

        public virtual ICollection<Ticket>? Tickets { get; set; }
    }

}