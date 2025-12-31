using IPMS.Models.DTOs;
using IPMS.Models.DTOs.Reports;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Queries.Reports
{
    public class MonthlyPerformanceTrendQuery : IMonthlyPerformanceTrendQuery
    {
        private readonly string _connectionString;

        public MonthlyPerformanceTrendQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }
        public IReadOnlyList<PerformancePointDto> GetMonthsPerformance(
            Guid userId,
            ReportFiltersRequest filters)
        {
            const string sql = @"
WITH Months AS (
    SELECT EOMONTH(@StartDate) AS ValuationDate
    UNION ALL
    SELECT EOMONTH(DATEADD(MONTH, 1, ValuationDate))
    FROM Months
    WHERE ValuationDate < EOMONTH(@EndDate)
),
UserInvestments AS (
    SELECT InvestmentId
    FROM Investments
    WHERE UserId = @UserId
      AND IsDeleted = 0
      AND (
            @HasTypes = 0
            OR InvestmentType IN (SELECT value FROM STRING_SPLIT(@Types, ','))
          )
),
UnitsAtDate AS (
    SELECT
        m.ValuationDate,
        t.InvestmentId,
        SUM(
            CASE t.TransactionType
                WHEN 'Buy'  THEN t.Units
                WHEN 'Sell' THEN -t.Units
            END
        ) AS UnitsOwned
    FROM Months m
    JOIN Transactions t
        ON t.TransactionDate <= m.ValuationDate
    JOIN UserInvestments ui
        ON ui.InvestmentId = t.InvestmentId
    WHERE t.IsDeleted = 0
    GROUP BY m.ValuationDate, t.InvestmentId
),
PriceAtDate AS (
    SELECT
        m.ValuationDate,
        ui.InvestmentId,
        (
            SELECT TOP 1 ph.UnitPrice
            FROM PriceHistory ph
            WHERE ph.InvestmentId = ui.InvestmentId
              AND ph.PriceDate <= m.ValuationDate
            ORDER BY ph.PriceDate DESC
        ) AS UnitPrice
    FROM Months m
    CROSS JOIN UserInvestments ui
)
SELECT
    u.ValuationDate,
    SUM(u.UnitsOwned * p.UnitPrice) AS TotalMarketValue
FROM UnitsAtDate u
JOIN PriceAtDate p
    ON p.ValuationDate = u.ValuationDate
   AND p.InvestmentId = u.InvestmentId
WHERE u.UnitsOwned > 0
  AND p.UnitPrice IS NOT NULL
GROUP BY u.ValuationDate
ORDER BY u.ValuationDate
OPTION (MAXRECURSION 400);
";

            // 🔹 Date handling
            DateOnly startDate;
            DateOnly endDate;

            if (!string.IsNullOrWhiteSpace(filters.From)
                && !string.IsNullOrWhiteSpace(filters.To))
            {
                startDate = filters.FromDate;
                endDate = filters.ToDate;
            }
            else
            {
                var now = DateTime.UtcNow;
                startDate = DateOnly.FromDateTime(now.AddMonths(-11));
                endDate = DateOnly.FromDateTime(now);
            }

            var result = new List<PerformancePointDto>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@StartDate", startDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@EndDate", endDate.ToDateTime(TimeOnly.MinValue));

            var hasTypes = filters.InvestmentTypes != null && filters.InvestmentTypes.Any();
            cmd.Parameters.AddWithValue("@HasTypes", hasTypes ? 1 : 0);
            cmd.Parameters.AddWithValue(
                "@Types",
                hasTypes ? string.Join(",", filters.InvestmentTypes!) : string.Empty
            );

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
