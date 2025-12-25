using IPMS.Models.DTOs.Investments;
using IPMS.Models.Filters;

namespace IPMS.Queries.Investments
{
    public interface IInvestmentListQuery
    {
        InvestmentListResultDto Get(Guid userId, InvestmentListFilter filter);
    }
}
