namespace IPMS.Models.Filters
{
    public sealed record TransactionListFilter(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? TransactionType,
    string? SortBy = "TransactionDate", // TransactionDate | Units | Amount
    string? SortDirection = "DESC",
    int Page = 1,
    int PageSize = 20
);
}
