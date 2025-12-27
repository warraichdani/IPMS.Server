
namespace IPMS.Models.DTOs.Transactions
{
    public sealed record AllTransactionListItemDto(
    Guid TransactionId,
    Guid InvestmentId,
    string InvestmentName,
    string TransactionType,
    decimal Units,
    decimal UnitPrice,
    decimal Amount,
    DateOnly Date
);

}
