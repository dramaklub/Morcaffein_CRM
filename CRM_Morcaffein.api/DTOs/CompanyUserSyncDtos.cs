namespace CRM_Morcaffein.api.DTOs;

public class CompanyUserSyncDtos
{
    /// <summary>Total credentials read across all tenants.</summary>
    public int TotalScanned { get; set; }

    /// <summary>Rows that were newly inserted into master.CompanyUsers.</summary>
    public int Inserted { get; set; }

    /// <summary>Rows skipped because the username was already in master.CompanyUsers.</summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Usernames that appear in more than one tenant's Credentials table.
    /// These are NOT inserted (the unique index would reject them) and are
    /// reported so they can be manually resolved.
    /// </summary>
    public List<CompanyUserConflict> Conflicts { get; set; } = new();
}

public class CompanyUserConflict
{
    public string Username { get; set; } = string.Empty;
    /// <summary>CompanyIds where this username appears in Credentials.</summary>
    public List<int> CompanyIds { get; set; } = new();
}