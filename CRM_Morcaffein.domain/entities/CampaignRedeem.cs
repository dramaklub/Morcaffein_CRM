using System;

namespace CRM_Morcaffein.domain.entities;

public class CampaignRedeem : BaseEntity
{
    public int RedeemId { get; set; }
    public int CampaignId { get; set; }
    public int OrderId { get; set; }
    public decimal DiscountUsed { get; set; }
    public DateTime RedeemDate { get; set; } = DateTime.UtcNow;

    public virtual Campaign? Campaign { get; set; }
    public virtual Order? Order { get; set; }
}
