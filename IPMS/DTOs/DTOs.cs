namespace IPMS.DTOs
{
    public record RegisterUserDto(string Email, string FirstName, string? LastName, string Password);
    public record LoginUserDto(string Email, string Password);
    public record UserDto(Guid UserId, string Email, string FirstName, string? LastName, bool IsActive);
    public record LoginRequestDto(string Email, string Password);
    public record TokenResponseDto(string AccessToken, string RefreshToken);
    //------------------------DTOs for Domain(investment) Events ------------------------

    public sealed record SellInvestmentRequest(
    Guid InvestmentId,
    decimal UnitsToSell,
    Guid UserId);

    public sealed record UpdatePriceRequest(
        Guid InvestmentId,
        decimal UnitPrice,
        DateOnly PriceDate,
        Guid UserId);

    public sealed record TransactionResponseDto(
    Guid TransactionId,
    Guid InvestmentId,
    decimal Units,
    decimal UnitPrice,
    DateOnly TransactionDate);

    public sealed record BuyInvestmentResponse(
    Guid TransactionId,
    Guid InvestmentId,
    decimal UnitsBought,
    decimal UnitPrice,
    DateOnly TransactionDate,
    decimal TotalUnitsAfter,
    decimal CostBasisAfter
    );

    public sealed record SellInvestmentResponse(
    Guid TransactionId,
    Guid InvestmentId,
    decimal UnitsSold,
    decimal UnitPrice,
    DateOnly TransactionDate,
    decimal TotalUnitsAfter,
    decimal CostBasisAfter
    );

    public sealed record UpdatePriceResponse(
    Guid InvestmentId,
    decimal UnitPrice,
    DateOnly PriceDate,
    decimal MarketValue // Units * UnitPrice
    );

    //------------------------DTOs for charts------------------------------
    public sealed record PerformancePointDto(
    DateOnly Date,
    decimal MarketValue);

    public sealed record AllocationDto(
        string InvestmentType,
        decimal Value);
}
