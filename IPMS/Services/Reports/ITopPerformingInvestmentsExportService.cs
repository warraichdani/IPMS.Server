using IPMS.Helpers.Utilities;
using IPMS.Models.DTOs.Reports;
using IPMS.Services.Reports;
using System.Text;

namespace IPMS.Services.Reports
{
    public interface ITopPerformingInvestmentsExportService
    {
        FileExport ExportCsv(IEnumerable<TopPerformingInvestmentRowDto> rows);
        FileExport ExportJson(IEnumerable<TopPerformingInvestmentRowDto> rows);
        FileExport ExportPdf(IEnumerable<TopPerformingInvestmentRowDto> rows);
    }
}