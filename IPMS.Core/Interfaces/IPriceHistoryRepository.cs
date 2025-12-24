using IPMS.Core.Entities;

namespace IPMS.Core.Repositories;

public interface IPriceHistoryRepository
{
    void Add(PriceHistory priceHistory);

    decimal? GetLatestPrice(Guid investmentId);

    IReadOnlyList<PriceHistory> GetForPeriod(
        Guid investmentId,
        DateOnly from,
        DateOnly to);
}
