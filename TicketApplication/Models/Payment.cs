namespace TicketApplication.Models
{
    public class Payment : AuditableEntity
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }

        public virtual Order Order { get; set; }

    }
}
