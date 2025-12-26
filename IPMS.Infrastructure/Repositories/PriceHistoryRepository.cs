using IPMS.Core;
using IPMS.Core.Entities;
using IPMS.Core.Repositories;
using Microsoft.Data.SqlClient;

public sealed class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly SqlConnection _connection;
    private readonly SqlTransaction _transaction;

    public PriceHistoryRepository(IUnitOfWork uow)
    {
        _connection = uow.Connection;
        _transaction = uow.Transaction;
    }

    public void Add(PriceHistory priceHistory)
    {
        const string sql = @"
INSERT INTO PriceHistory (
    PriceHistoryId,
    InvestmentId,
    PriceDate,
    UnitPrice,
    CreatedAt
)
VALUES (
    @Id,
    @InvestmentId,
    @PriceDate,
    @UnitPrice,
    @CreatedAt
);";

        using var cmd = new SqlCommand(sql, _connection, _transaction);
        cmd.Parameters.AddWithValue("@Id", priceHistory.PriceHistoryId);
        cmd.Parameters.AddWithValue("@InvestmentId", priceHistory.InvestmentId);
        cmd.Parameters.AddWithValue(
            "@PriceDate",
            priceHistory.PriceDate.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@UnitPrice", priceHistory.UnitPrice);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        cmd.ExecuteNonQuery();
    }

    public decimal? GetLatestPrice(Guid investmentId)
    {
        const string sql = @"
SELECT TOP 1 UnitPrice
FROM PriceHistory
WHERE InvestmentId = @InvestmentId
ORDER BY PriceDate DESC;";

        using var cmd = new SqlCommand(sql, _connection, _transaction);
        cmd.Parameters.AddWithValue("@InvestmentId", investmentId);

        var result = cmd.ExecuteScalar();
        return result == null || result == DBNull.Value
            ? null
            : (decimal)result;
    }

    public IReadOnlyList<PriceHistory> GetForPeriod(
        Guid investmentId,
        DateOnly from,
        DateOnly to)
    {
        const string sql = @"
SELECT PriceDate, UnitPrice
FROM PriceHistory
WHERE InvestmentId = @InvestmentId
  AND PriceDate BETWEEN @FromDate AND @ToDate
ORDER BY PriceDate;";

        using var cmd = new SqlCommand(sql, _connection, _transaction);
        cmd.Parameters.AddWithValue("@InvestmentId", investmentId);
        cmd.Parameters.AddWithValue("@FromDate", from.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@ToDate", to.ToDateTime(TimeOnly.MinValue));

        var list = new List<PriceHistory>();

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new PriceHistory(
                investmentId,
                DateOnly.FromDateTime(reader.GetDateTime(0)),
                reader.GetDecimal(1)));
        }

        return list;
    }
}
