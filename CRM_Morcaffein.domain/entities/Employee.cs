using System.Collections.Generic;

namespace CRM_Morcaffein.domain.entities;

public class Employee : BasePerson
{
    public int EmployeeId { get; set; }
    public string Role { get; set; } = "Barista";

    public virtual Credential? Credential { get; set; }
    public virtual ICollection<Order> OrdersCreated { get; set; } = new List<Order>();
    public virtual ICollection<Campaign> CampaignsCreated { get; set; } = new List<Campaign>();
    public virtual ICollection<Feedback> FeedbacksLogged { get; set; } = new List<Feedback>();
}
