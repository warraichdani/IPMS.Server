namespace IPMS.Core.Application.DTOs
{
    public sealed record SystemStatisticsDto(
    int TotalUsers,
    int TotalPortfolios,
    decimal TotalInvestmentsCurrentValue,
    int TodaysTransactionsCount
);
}
