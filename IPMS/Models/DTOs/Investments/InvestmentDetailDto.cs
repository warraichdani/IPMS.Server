
namespace IPMS.Models.DTOs.Investments
{
    public sealed record InvestmentDetailDto(
    Guid InvestmentId,
    string InvestmentName,
    string InvestmentType,
    decimal CurrentValue,
    decimal GainLossPercent,
    string Status,
    DateOnly PurchaseDate,
    decimal CostBasis,
    decimal TotalUnits,
    decimal UnitPrice,
    string? Broker,
    string? Notes
);
}
