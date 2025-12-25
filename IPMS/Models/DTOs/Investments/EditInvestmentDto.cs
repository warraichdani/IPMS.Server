
namespace IPMS.Models.DTOs.Investments
{
    public sealed record EditInvestmentDto(
    Guid InvestmentId,
    string InvestmentName,
    string InvestmentType,
    decimal InitialAmount,
    DateOnly PurchaseDate,
    string? Broker,
    string? Notes,
    string Status
);

}
