using IPMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Queries.Performance
{
    public sealed class PerformanceQuery : IPerformanceQuery
    {
        private readonly string _connectionString;

        public PerformanceQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public IReadOnlyList<PerformancePointDto> GetLast12Months(Guid investmentId, Guid userId)
        {
            const string sql = @"
        WITH Dates AS (
    SELECT CAST(DATEADD(MONTH, -11, EOMONTH(GETUTCDATE())) AS DATE) AS ValuationDate
    UNION ALL
    SELECT DATEADD(MONTH, 1, ValuationDate)
    FROM Dates
    WHERE ValuationDate < EOMONTH(GETUTCDATE())
),
UnitsAtDate AS (
    SELECT
        d.ValuationDate,
        SUM(
            CASE t.TransactionType
                WHEN 'Buy' THEN t.Units
                WHEN 'Sell' THEN -t.Units
            END
        ) AS UnitsOwned
    FROM Dates d
    JOIN Transactions t
        ON t.TransactionDate <= d.ValuationDate
    JOIN Investments i
        ON i.InvestmentId = t.InvestmentId
    WHERE t.InvestmentId = @InvestmentId
      AND i.UserId = @UserId
      AND t.IsDeleted = 0
    GROUP BY d.ValuationDate
),
PriceAtDate AS (
    SELECT
        d.ValuationDate,
        (
            SELECT TOP 1 ph.UnitPrice
            FROM PriceHistory ph
            JOIN Investments i2
                ON i2.InvestmentId = ph.InvestmentId
            WHERE ph.InvestmentId = @InvestmentId
              AND i2.UserId = @UserId
              AND ph.PriceDate <= d.ValuationDate
            ORDER BY ph.PriceDate DESC
        ) AS UnitPrice
    FROM Dates d
)
SELECT
    u.ValuationDate,
    u.UnitsOwned * p.UnitPrice AS MarketValue
FROM UnitsAtDate u
JOIN PriceAtDate p
    ON u.ValuationDate = p.ValuationDate
ORDER BY u.ValuationDate
OPTION (MAXRECURSION 12);";

            var result = new List<PerformancePointDto>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@InvestmentId", investmentId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new PerformancePointDto(
                    DateOnly.FromDateTime(reader.GetDateTime(0)),
                    reader.GetDecimal(1)
                ));
            }

            return result;
        }
    }

}
