public interface IReportingService
{
    Task<ReportResult> GenerateReportAsync(ReportRequest request);
    Task<List<ReportTemplate>> GetAvailableTemplatesAsync();
    Task<byte[]> ExportReportAsync(ReportResult report, ExportFormat format);
    Task<bool> ScheduleReportAsync(ReportSchedule schedule);
    Task<List<ReportSchedule>> GetScheduledReportsAsync();
}
