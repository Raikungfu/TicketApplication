
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        [NotMapped]
        public IFormFile ImageFile { get; set; }

        [NotMapped]
        public decimal MaxTicketPrice => Zones?.Any() == true ? Zones.Max(zone => zone.Price) : 0;

        [NotMapped]
        public decimal MinTicketPrice => Zones?.Any() == true ? Zones.Min(zone => zone.Price) : 0;

        public virtual ICollection<Ticket>? Tickets { get; set; }

        public virtual ICollection<Zone>? Zones { get; set; }

    }
}
