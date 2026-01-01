
namespace IPMS.Models.DTOs.Reports
{
    public sealed record TopPerformingInvestmentRowDto(
    Guid InvestmentId,
    string InvestmentName,
    string InvestmentType,
    decimal UnitsHeld,
    decimal CostBasis,
    decimal CurrentValue,
    decimal GainLoss,
    decimal GainLossPercent
);
}
