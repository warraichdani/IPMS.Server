using IPMS.Models.DTOs.Reports;
using IPMS.Shared;

namespace IPMS.Queries.Reports
{
    public interface ITopPerformingInvestmentsQuery
    {
        PagedResult<TopPerformingInvestmentRowDto> Get(
            Guid userId,
            ReportFiltersRequest filters
        );
    }
}
