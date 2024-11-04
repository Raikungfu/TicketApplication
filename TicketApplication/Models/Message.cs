using System;
using System.ComponentModel.DataAnnotations;

namespace TicketApplication.Models
{
    public class Message : AuditableEntity
    {
        [Required]
        public string Content { get; set; }

        [Required]
        public string FromUserId { get; set; }

        [Required]
        public string ToRoomId { get; set; }

        public DateTime Timestamp { get; set; }

        public virtual User FromUser { get; set; }

        public virtual Room ToRoom { get; set; }
    }
}