using System.ComponentModel.DataAnnotations;

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
        public int EventId { get; set; }
        public virtual Event Event { get; set; }
    }

}