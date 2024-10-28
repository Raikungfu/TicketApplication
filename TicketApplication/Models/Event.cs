
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


        public string? Image { get; set; }


        public virtual ICollection<Zone>? Zones { get; set; }

    }
}
