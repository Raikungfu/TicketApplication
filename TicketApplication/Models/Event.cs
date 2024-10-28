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
     
        [Required]
        public string Price { get; set; }

        
        public int NumberOfTickets { get; set; } 


        public string? Image { get; set; }


       public virtual ICollection<Ticket>? Tickets { get; set; }

    }
}
