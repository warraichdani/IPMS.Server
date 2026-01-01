
namespace IPMS.Models.DTOs.Reports
{
    public sealed record TransactionHistoryRowDto(
    Guid TransactionId,
    DateOnly TransactionDate,
    string InvestmentName,
    string InvestmentStatus,
    string TransactionType,
    decimal Units,
    decimal UnitPrice,
    decimal Amount,
    decimal TotalInvestmentValueAtDate,
    decimal GainLossAtDate
);
}
