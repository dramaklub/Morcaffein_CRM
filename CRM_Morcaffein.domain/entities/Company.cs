using System;
using System.Collections.Generic;
using System.Text;

namespace CRM_Morcaffein.domain.entities
{
    public class Company
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsSynced { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CurrentPlanId { get; set; }
        public Plan? CurrentPlan { get; set; }

        public ICollection<Device> Devices { get; set; } = new List<Device>();



    }
}
