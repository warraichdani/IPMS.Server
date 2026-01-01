using IPMS.Models.DTOs.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Services.Reports
{
    public interface ITransactionHistoryReportExportService
    {
        FileExport ExportCsv(IEnumerable<TransactionHistoryRowDto> rows);
        FileExport ExportJson(IEnumerable<TransactionHistoryRowDto> rows);
        FileExport ExportPdf(IEnumerable<TransactionHistoryRowDto> rows);
    }
}
