
namespace IPMS.Commands
{
    //--------------Commands for CRUD Operations--------------

    public sealed record CreateInvestmentCommand(
    string InvestmentName,
    string InvestmentType,
    decimal InitialAmount,
    DateOnly PurchaseDate,
    string? Broker,
    string? Notes,
    decimal InitialUnitPrice = 1m
);



    //------------Commands for Behavioral Core-------------
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
