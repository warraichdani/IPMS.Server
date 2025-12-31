using IPMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Reports
{
    public sealed class InvestmentDistributionQuery : IInvestmentDistributionQuery
    {
        private readonly string _connectionString;

        public InvestmentDistributionQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public IReadOnlyList<AllocationDto> GetDistribution(
            Guid userId,
            DateOnly from,
            DateOnly to)
        {
            const string sql = @"
WITH UnitsAsOfDate AS (
    SELECT
        i.InvestmentType,
        i.InvestmentId,
        SUM(
            CASE t.TransactionType
                WHEN 'Buy' THEN t.Units
                WHEN 'Sell' THEN -t.Units
            END
        ) AS UnitsOwned
    FROM Investments i
    JOIN Transactions t
        ON t.InvestmentId = i.InvestmentId
       AND t.TransactionDate <= @ToDate
       AND t.IsDeleted = 0
    WHERE i.UserId = @UserId
      AND i.IsDeleted = 0
    GROUP BY i.InvestmentType, i.InvestmentId
    HAVING SUM(
        CASE t.TransactionType
            WHEN 'Buy' THEN t.Units
            WHEN 'Sell' THEN -t.Units
        END
    ) > 0
),
PriceAsOfDate AS (
    SELECT
        u.InvestmentId,
        (
            SELECT TOP 1 ph.UnitPrice
            FROM PriceHistory ph
            WHERE ph.InvestmentId = u.InvestmentId
              AND ph.PriceDate <= @ToDate
            ORDER BY ph.PriceDate DESC
        ) AS UnitPrice
    FROM UnitsAsOfDate u
)
SELECT
    u.InvestmentType,
    SUM(u.UnitsOwned * p.UnitPrice) AS TotalValue
FROM UnitsAsOfDate u
JOIN PriceAsOfDate p
    ON p.InvestmentId = u.InvestmentId
WHERE p.UnitPrice IS NOT NULL
GROUP BY u.InvestmentType
ORDER BY TotalValue DESC;
";

            var result = new List<AllocationDto>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@ToDate", to.ToDateTime(TimeOnly.MaxValue));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new AllocationDto(
                    reader.GetString(0),
                    reader.GetDecimal(1)
                ));
            }

            return result;
        }
    }

}
