namespace IPMS.Models.Filters
{
    public sealed class AllTransactionListFilter
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;

        public string? InvestmentId { get; init; }
        public string? InvestmentName { get; init; }
        public string? TransactionType { get; init; }

        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
    }
}
