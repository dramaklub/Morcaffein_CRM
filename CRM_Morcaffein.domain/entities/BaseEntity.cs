using System;

namespace CRM_Morcaffein.domain.entities;

public abstract class BaseEntity
{
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// False until this row has been pushed to the cloud replica by the sync
    /// worker. New rows created by the API default to false; the worker flips
    /// to true after a successful push. Existing rows predating sync are
    /// marked true by the IsSynced migration's UPDATE statements.
    ///
    /// On non-synced entities (Product, ProductSize, ProductModifier), the
    /// column exists because they inherit BaseEntity, but it's always true
    /// and never read — the worker's queries target specific DbSets.
    /// </summary>
    public bool IsSynced { get; set; } = false;
}
