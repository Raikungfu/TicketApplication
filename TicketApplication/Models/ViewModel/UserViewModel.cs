using System.ComponentModel.DataAnnotations;

namespace TicketApplication.Models.ViewModel
{

    public class UserViewModel
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public string? CurrentRoom { get; set; }
        public string? Device { get; set; }
        public bool? IsOnline { get; set; }

    }

    public class MessageViewModel
    {
        public string? Id { get; set; }
        public string? Content { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? FromUserName { get; set; }
        public string? FromFullName { get; set; }
        public string? Room { get; set; }
    }
}
