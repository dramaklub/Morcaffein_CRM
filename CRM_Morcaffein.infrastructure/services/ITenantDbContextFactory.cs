using CRM_Morcaffein.infrastructure.data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM_Morcaffein.infrastructure.services
{
    public interface ITenantDbContextFactory
    {
        Task<TenantCrmDbContext> CreateAsync(int companyId);

    }
}
