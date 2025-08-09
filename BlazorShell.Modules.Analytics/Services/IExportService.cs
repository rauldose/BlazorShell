using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorShell.Modules.Analytics.Services
{

    public interface IExportService
    {
        Task<byte[]> ExportToPdfAsync<T>(T data, string templateName = null);
        Task<byte[]> ExportToExcelAsync<T>(T data, string sheetName = "Data");
        Task<byte[]> ExportToCsvAsync<T>(T data);
        Task<string> ExportToJsonAsync<T>(T data);
        Task<string> ExportToHtmlAsync<T>(T data, string templateName = null);
    }
}
