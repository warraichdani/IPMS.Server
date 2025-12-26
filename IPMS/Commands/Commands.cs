
using IPMS.Core.Configs;

namespace IPMS.Commands
{
    //--------------Commands for CRUD Operations--------------

    public sealed record CreateInvestmentCommand(
    string InvestmentName,
    string InvestmentType,
    decimal InitialAmount,
    DateOnly PurchaseDate,
    string Status,
    string? Broker,
    string? Notes,
    decimal InitialUnitPrice = 1m
);

    public sealed record UpdateInvestmentCommand(
    Guid InvestmentId,
    string InvestmentName,
    string InvestmentType,
    DateOnly PurchaseDate,
    string? Broker,
    string? Notes
);

    public sealed record DeleteInvestmentsCommand(
    IReadOnlyList<Guid> InvestmentIds,
    Guid UserId
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
     decimal Amount,
     DateOnly Date,
     Guid UserId);
}
