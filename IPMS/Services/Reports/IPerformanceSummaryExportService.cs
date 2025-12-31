using IPMS.Models.DTOs.Reports;

namespace IPMS.Services.Reports
{
    public interface IPerformanceSummaryExportService
    {
        FileExport ExportCsv(IEnumerable<PerformanceSummaryDto> rows);
        FileExport ExportJson(IEnumerable<PerformanceSummaryDto> rows);
        FileExport ExportPdfSimulation(IEnumerable<PerformanceSummaryDto> rows);
    }
}
