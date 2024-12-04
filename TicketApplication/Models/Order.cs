using System.ComponentModel.DataAnnotations.Schema;

namespace TicketApplication.Models
{
    public class Order : AuditableEntity
    {
        public string UserId { get; set; }
        public long OrderCode { get; set; } = new Random().Next(100000000, 999999999);
        public string? Status { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }

        public virtual List<OrderDetail>? OrderDetails { get; set; }
        public virtual User? User { get; set; }

        public string? DiscountId { get; set; }

        [ForeignKey("DiscountId")]
        public virtual Discount? Discount { get; set; }
        public virtual Payment Payments { get; set; }
    }
}
