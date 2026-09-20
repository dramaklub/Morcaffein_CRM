namespace CRM_Morcaffein.winforms.Services.Reports
{
    /// <summary>
    /// Format-agnostic report model. The builder produces one of these; any
    /// renderer (MigraDoc today, CSV or HTML later) consumes one. No WinForms or
    /// PDF types leak into this file — it's pure data.
    /// </summary>
    public class ReportDocument
    {
        public string Title { get; init; } = "Morcaffein Report";
        public string Subtitle { get; init; } = string.Empty;
        public string PeriodLabel { get; init; } = string.Empty;
        public string GeneratedBy { get; init; } = string.Empty;
        public DateTime GeneratedAt { get; init; } = DateTime.Now;
        public List<ReportSection> Sections { get; init; } = new();
    }

    public class ReportSection
    {
        public string Heading { get; init; } = string.Empty;
        public string? Subtitle { get; init; }
        public List<ReportBlock> Blocks { get; init; } = new();
    }

    public abstract class ReportBlock { }

    /// <summary>One row of "Label: Value | Label: Value | ..." metrics.</summary>
    public class MetricStripBlock : ReportBlock
    {
        public List<(string Label, string Value)> Metrics { get; init; } = new();
    }

    /// <summary>A plain table with a header row and zero or more data rows.</summary>
    public class TableBlock : ReportBlock
    {
        public List<string> Columns { get; init; } = new();
        public List<string[]> Rows { get; init; } = new();
    }

    /// <summary>Free text (used for "no data" states or notes).</summary>
    public class ParagraphBlock : ReportBlock
    {
        public string Text { get; init; } = string.Empty;
        public bool Muted { get; init; } = true;
    }
}