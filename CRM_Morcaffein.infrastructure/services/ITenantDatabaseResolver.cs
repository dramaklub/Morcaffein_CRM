using System;
using System.Collections.Generic;
using System.Text;

namespace CRM_Morcaffein.infrastructure.services
{
    public interface ITenantDatabaseResolver
    {
        Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId);


    }
}
