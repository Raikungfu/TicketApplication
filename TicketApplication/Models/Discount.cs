namespace TicketApplication.Models
{
    public class Discount : AuditableEntity
    {
        public string Code { get; set; }
        public string? Description { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public int? UsageLimit { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
