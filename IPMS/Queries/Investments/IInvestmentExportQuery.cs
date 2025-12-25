using IPMS.Models.DTOs.Investments;
using IPMS.Models.Filters;

namespace IPMS.Queries.Investments
{
    public interface IInvestmentExportQuery
    {
        IReadOnlyList<InvestmentExportRowDto> Export(
            Guid userId,
            InvestmentListFilter filter);
    }
}
