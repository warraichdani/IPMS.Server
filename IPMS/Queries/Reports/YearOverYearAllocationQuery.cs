using IPMS.Models.DTOs.Reports;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Reports
{
    public sealed class YearOverYearAllocationQuery : IYearOverYearAllocationQuery
    {
        private readonly string _connectionString;

        public YearOverYearAllocationQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public IReadOnlyList<YoYAllocationRowDto> GetByUser(Guid userId)
        {
            const string sql = @"
WITH Years AS (
    SELECT DISTINCT YEAR(t.TransactionDate) AS [Year]
    FROM Transactions t
    JOIN Investments i ON i.InvestmentId = t.InvestmentId
    WHERE i.UserId = @UserId
      AND i.IsDeleted = 0
      AND t.IsDeleted = 0
),
UnitsAsOfYearEnd AS (
    SELECT
        y.[Year],
        i.InvestmentType,
        i.InvestmentId,
        SUM(
            CASE t.TransactionType
                WHEN 'Buy' THEN t.Units
                WHEN 'Sell' THEN -t.Units
            END
        ) AS UnitsHeld
    FROM Years y
    JOIN Investments i ON i.UserId = @UserId AND i.IsDeleted = 0
    JOIN Transactions t
        ON t.InvestmentId = i.InvestmentId
       AND t.TransactionDate <= DATEFROMPARTS(y.[Year], 12, 31)
       AND t.IsDeleted = 0
    GROUP BY y.[Year], i.InvestmentType, i.InvestmentId
),
PriceAsOfYearEnd AS (
    SELECT
        y.[Year],
        i.InvestmentId,
        (
            SELECT TOP 1 ph.UnitPrice
            FROM PriceHistory ph
            WHERE ph.InvestmentId = i.InvestmentId
              AND ph.PriceDate <= DATEFROMPARTS(y.[Year], 12, 31)
            ORDER BY ph.PriceDate DESC
        ) AS UnitPrice
    FROM Years y
    JOIN Investments i ON i.UserId = @UserId AND i.IsDeleted = 0
),
Valuation AS (
    SELECT
        u.[Year],
        u.InvestmentType,
        (u.UnitsHeld * p.UnitPrice) AS MarketValue
    FROM UnitsAsOfYearEnd u
    JOIN PriceAsOfYearEnd p
        ON p.[Year] = u.[Year]
       AND p.InvestmentId = u.InvestmentId
    WHERE u.UnitsHeld > 0
),
Totals AS (
    SELECT
        [Year],
        SUM(MarketValue) AS TotalPortfolioValue
    FROM Valuation
    GROUP BY [Year]
)
SELECT
    v.[Year],
    v.InvestmentType,
    SUM(v.MarketValue) AS TotalValue,
    (SUM(v.MarketValue) / t.TotalPortfolioValue) * 100 AS AllocationPercent
FROM Valuation v
JOIN Totals t ON t.[Year] = v.[Year]
GROUP BY v.[Year], v.InvestmentType, t.TotalPortfolioValue
ORDER BY v.[Year], v.InvestmentType;
";

            var result = new List<YoYAllocationRowDto>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new YoYAllocationRowDto(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetDecimal(2),
                    reader.GetDecimal(3)
                ));
            }

            return result;
        }
    }

}
