using IPMS.Models.DTOs.Reports;
using IPMS.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Reports
{
    public interface ITransactionHistoryReportQuery
    {
        PagedResult<TransactionHistoryRowDto> Get(
            Guid userId,
            ReportFiltersRequest filters
        );
    }
}
