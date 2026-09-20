namespace CRM_Morcaffein.api.DTOs;

/// <summary>
/// Standard shape for any paginated list endpoint. Reused by Feedback, Customers,
/// Products — anything that returns rows in pages.
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}