namespace IPMS.DTOs.Investments
{
    public sealed record InvestmentListItemDto(
    Guid InvestmentId,
    string Name,
    string Type,
    decimal Amount,
    decimal CurrentValue,
    decimal GainLossPercent,
    DateOnly PurchaseDate,
    string Status
);
}
