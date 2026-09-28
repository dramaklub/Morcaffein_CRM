using System;

namespace CRM_Morcaffein.domain.entities;

/// <summary>
/// A versioned Terms & Conditions document. One row per plan — tenants on
/// a plan agree to that plan's terms. Lives in the master DB (it's a
/// platform-level artifact, not tenant data).
///
/// Body is the full text. Editing is a future feature — for now this is
/// read-only reference material shown in the SuperAdmin console.
///
/// PlanId is nullable so a general platform-level T&C (not tied to any
/// plan) can be added later without a schema change. Not currently used,
/// but the column is here so we don't need another migration.
/// </summary>
public class TermsAndConditions
{
    public int TermsId { get; set; }

    /// <summary>The plan these terms belong to. Null = general/platform-level.</summary>
    public int? PlanId { get; set; }
    public Plan? Plan { get; set; }
    public bool IsSynced { get; set; } = false;

    public string Title { get; set; } = string.Empty;

    /// <summary>The full text of the terms. Multi-paragraph.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Semantic version, e.g. "1.0", "1.1". Free text.</summary>
    public string Version { get; set; } = "1.0";

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}