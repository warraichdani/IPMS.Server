using IPMS.Models.DTOs.Transactions;
using IPMS.Models.Filters;
using IPMS.Shared;

namespace IPMS.Queries.Transactions
{
    public interface IUserTransactionQuery
    {
        PagedResult<AllTransactionListItemDto> Get(
            Guid userId,
            AllTransactionListFilter filter);
    }
}
