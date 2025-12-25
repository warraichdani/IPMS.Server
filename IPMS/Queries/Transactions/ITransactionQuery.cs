using IPMS.Models.DTOs.Transactions;
using IPMS.Models.Filters;

namespace IPMS.Queries.Transactions
{
    public interface ITransactionQuery
    {
        IReadOnlyList<TransactionListItemDto> GetTransactionsForInvestment(
            Guid investmentId,
            Guid userId,
            TransactionListFilter filter,
            out int totalCount);
    }

}
