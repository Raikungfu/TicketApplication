
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace TicketApplication.Models
{
    public class Event : AuditableEntity
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string Date { get; set; }

        public string? Status { get; set; } = "Visible";

        public string? Image { get; set; }

        public decimal MaxTicketPrice => Tickets?.Any() == true ? Tickets.Max(ticket => ticket.Price) : 0;

        public decimal MinTicketPrice => Tickets?.Any() == true ? Tickets.Min(ticket => ticket.Price) : 0;

        public virtual ICollection<Ticket>? Tickets { get; set; }

        public virtual ICollection<Zone>? Zones { get; set; }

    }
}
