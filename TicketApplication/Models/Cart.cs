using System.ComponentModel.DataAnnotations.Schema;

namespace TicketApplication.Models
{
    public class Cart
    {
        public string UserId { get; set; }

        public string ZoneId { get; set; }

        public int Quantity { get; set; }

        [ForeignKey("ZoneId")]
        public virtual Zone? Zone { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [NotMapped]
        public decimal TotalPrice => Zone.Price * Quantity;
    }
}
