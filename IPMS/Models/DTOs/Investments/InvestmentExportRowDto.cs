namespace IPMS.Models.DTOs.Investments
{
    public sealed record InvestmentExportRowDto(
    string InvestmentName,
    string InvestmentType,
    decimal Amount,           // CostBasis
    decimal CurrentValue,
    decimal GainLossPercent,
    DateOnly PurchaseDate,
    string Status
);
}
