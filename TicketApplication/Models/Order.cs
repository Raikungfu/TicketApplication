namespace TicketApplication.Models
{
    public class Order : AuditableEntity
    {
        public string UserId { get; set; }
        public string? Status { get; set; }
        public decimal TotalAmount { get; set; }

        public virtual List<OrderDetail>? OrderDetails { get; set; }
        public virtual User? User { get; set; }
        public virtual Payment Payments { get; set; }
    }
}
