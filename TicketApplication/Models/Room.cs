using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using System.Collections.Generic;

namespace TicketApplication.Models
{
    public class Room : AuditableEntity
    {
        public string Name { get; set; }

        public string AdminId { get; set; }

        public string UserId { get; set; }

        public string Description { get; set; }

        public virtual User Admin { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
