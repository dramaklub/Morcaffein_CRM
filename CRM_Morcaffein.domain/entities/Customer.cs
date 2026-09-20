using System;
using System.Collections.Generic;

namespace CRM_Morcaffein.domain.entities;

public class Customer : BasePerson
{
    public int CustomerId { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime LastVisitDate { get; set; } = DateTime.UtcNow;
    public int PointsBalance { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public virtual ICollection<PointLedger> PointLedgers { get; set; } = new List<PointLedger>();
    public virtual ICollection<PointAdjustmentRequest> PointAdjustmentRequests { get; set; } = new List<PointAdjustmentRequest>();
}
