using System;
using System.ComponentModel.DataAnnotations;

namespace TicketApplication.Models
{
    public abstract class AuditableEntity
    {
        private static readonly Random random = new Random();

        [Key]
        public string Id { get; set; } = GenerateShortId();

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; } 

        public DateTime? LastModified { get; set; } = DateTime.Now;

        public string? LastModifiedBy { get; set; }

        private static string GenerateShortId()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

   
}
