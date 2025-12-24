
namespace IPMS.Commands
{
    public sealed record BuyInvestmentCommand(
    Guid InvestmentId,
    decimal Amount,
    decimal UnitPrice,
    DateOnly Date,
    Guid UserId);

    public sealed record SellInvestmentCommand(
    Guid InvestmentId,
    decimal UnitsToSell,
    decimal UnitPrice,
    DateOnly Date,
    Guid UserId);

    public sealed record UpdatePriceCommand(
     Guid InvestmentId,
     decimal UnitPrice,
     DateOnly Date,
     Guid UserId);
}
