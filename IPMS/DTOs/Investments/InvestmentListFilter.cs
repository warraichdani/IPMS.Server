namespace IPMS.DTOs.Investments
{
    public sealed record InvestmentListFilter(
    string? Search,
    string? Type,
    string? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    decimal? MinGainLossPercent,
    decimal? MaxGainLossPercent,
    string SortBy = "Date",     // Amount | CurrentValue | GainLoss | Date
    string SortDirection = "DESC",
    int Page = 1,
    int PageSize = 20
);
}
