using IPMS.Models.DTOs.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Services.Reports
{
    public interface IYoYAllocationReportExportService
    {
        FileExport ExportCsv(IEnumerable<YoYAllocationRowDto> rows);
        FileExport ExportJson(IEnumerable<YoYAllocationRowDto> rows);
        FileExport ExportPdf(IEnumerable<YoYAllocationRowDto> rows);
    }
}
