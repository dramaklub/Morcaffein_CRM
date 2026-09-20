using System;
using System.Collections.Generic;

namespace CRM_Morcaffein.domain.entities;

public class Order : BaseEntity
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int CreatedBy { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalAmount { get; set; } // Net spend after discounts
    public int PointsEarned { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public bool HasPwdSeniorDiscount { get; set; }
    public decimal PwdSeniorDiscountAmount { get; set; }

    public virtual Customer? Customer { get; set; }
    public virtual Employee? Employee { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<CampaignRedeem> CampaignRedeems { get; set; } = new List<CampaignRedeem>();
}
