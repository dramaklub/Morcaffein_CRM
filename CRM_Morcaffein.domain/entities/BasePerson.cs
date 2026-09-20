namespace CRM_Morcaffein.domain.entities;

public abstract class BasePerson : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Plain formatting, not a business rule — kept as a computed property rather than
    // moved to a Controller since it has no validation and no external dependency.
    public string FullName => $"{FirstName} {LastName}".Trim();
}
