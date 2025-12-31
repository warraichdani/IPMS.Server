using IPMS.Models.DTOs.Reports;
using IPMS.Shared;

namespace IPMS.Queries.Reports
{
    public interface IPerformanceSummaryReportQuery
    {
        Task<PagedResult<PerformanceSummaryDto>> GetAsync(
        Guid userId,
        ReportFiltersRequest request);
    }
}
