using System.ComponentModel.DataAnnotations;

namespace TicketApplication.Models
{
    public class User : AuditableEntity
    {
        [Required]
        public string Name { get; set; }
        [EmailAddress]
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string Role { get; set; }

        public virtual ICollection<Cart>? Carts { get; set; }
        public virtual ICollection<Order>? Orders { get; set; }
        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    }
}
