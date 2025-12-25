using IPMS.DTOs.Investments;

namespace IPMS.Queries.Investments
{
    public interface IInvestmentExportQuery
    {
        IReadOnlyList<InvestmentExportRowDto> Export(
            Guid userId,
            InvestmentListFilter filter);
    }
}
