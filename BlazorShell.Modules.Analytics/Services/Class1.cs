using BlazorShell.Modules.Analytics.Domain.Models;

public class ReportRequest
{
    public string ReportType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public string TemplateId { get; set; }
}

public class ReportResult
{
    public string Id { get; set; }
    public string Title { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Dictionary<string, object> Data { get; set; }
    public List<ChartDataPoint> Charts { get; set; }
    public List<TableData> Tables { get; set; }
}

public class ReportTemplate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public List<ReportParameter> Parameters { get; set; }
}

public class ReportParameter
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Type { get; set; }
    public bool Required { get; set; }
    public object DefaultValue { get; set; }
}

public class ReportSchedule
{
    public string Id { get; set; }
    public string ReportTemplateId { get; set; }
    public string Schedule { get; set; } // Cron expression
    public string Recipients { get; set; }
    public ExportFormat Format { get; set; }
    public bool IsActive { get; set; }
}

public class TableData
{
    public string Title { get; set; }
    public List<string> Headers { get; set; }
    public List<List<object>> Rows { get; set; }
}

public enum ExportFormat
{
    PDF,
    Excel,
    CSV,
    HTML,
    JSON
}