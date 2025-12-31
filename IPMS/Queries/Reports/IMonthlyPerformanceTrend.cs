using IPMS.Models.DTOs;
using IPMS.Models.DTOs.Reports;

namespace IPMS.Queries.Reports
{
    public interface IMonthlyPerformanceTrendQuery
    {
        public IReadOnlyList<PerformancePointDto> GetMonthsPerformance(
            Guid userId,
            ReportFiltersRequest filters);
    }
}
