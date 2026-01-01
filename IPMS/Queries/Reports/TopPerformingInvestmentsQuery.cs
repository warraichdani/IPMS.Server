
using IPMS.Models.DTOs.Reports;
using IPMS.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Queries.Reports
{
    public sealed class TopPerformingInvestmentsQuery
    : ITopPerformingInvestmentsQuery
    {
        private readonly string _connectionString;

        public TopPerformingInvestmentsQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public PagedResult<TopPerformingInvestmentRowDto> Get(
            Guid userId,
            ReportFiltersRequest filters)
        {
            const string sql = @"
WITH InvestmentUniverse AS (
    SELECT InvestmentId, InvestmentName, InvestmentType
    FROM Investments
    WHERE UserId = @UserId
      AND IsDeleted = 0
      AND (
          @HasTypes = 0 OR InvestmentType IN (SELECT value FROM STRING_SPLIT(@Types, ','))
      )
),
UnitsAndCost AS (
    SELECT
        i.InvestmentId,
        SUM(
            CASE t.TransactionType
                WHEN 'Buy' THEN t.Units
                WHEN 'Sell' THEN -t.Units
            END
        ) AS UnitsHeld,
        SUM(
            CASE t.TransactionType
                WHEN 'Buy' THEN t.Units * t.UnitPrice
                WHEN 'Sell' THEN -t.Units * t.UnitPrice
            END
        ) AS CostBasis
    FROM InvestmentUniverse i
    JOIN Transactions t ON t.InvestmentId = i.InvestmentId
    WHERE t.IsDeleted = 0
      AND t.TransactionDate <= @ToDate
    GROUP BY i.InvestmentId
),
LatestPrices AS (
    SELECT
        i.InvestmentId,
        (
            SELECT TOP 1 ph.UnitPrice
            FROM PriceHistory ph
            WHERE ph.InvestmentId = i.InvestmentId
              AND ph.PriceDate <= @ToDate
            ORDER BY ph.PriceDate DESC
        ) AS UnitPrice
    FROM InvestmentUniverse i
),
FinalData AS (
    SELECT
        i.InvestmentId,
        i.InvestmentName,
        i.InvestmentType,
        u.UnitsHeld,
        u.CostBasis,
        (u.UnitsHeld * p.UnitPrice) AS CurrentValue,
        ((u.UnitsHeld * p.UnitPrice) - u.CostBasis) AS GainLoss,
        CASE
            WHEN u.CostBasis = 0 THEN 0
            ELSE (((u.UnitsHeld * p.UnitPrice) - u.CostBasis) / u.CostBasis) * 100
        END AS GainLossPercent
    FROM InvestmentUniverse i
    JOIN UnitsAndCost u ON u.InvestmentId = i.InvestmentId
    JOIN LatestPrices p ON p.InvestmentId = i.InvestmentId
    WHERE u.UnitsHeld > 0
)
SELECT *,
       COUNT(*) OVER() AS TotalCount
FROM FinalData
ORDER BY GainLossPercent DESC
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;
";

            var items = new List<TopPerformingInvestmentRowDto>();
            int totalCount = 0;

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@ToDate", filters.ToDate.ToDateTime(TimeOnly.MinValue));

            var hasTypes = filters.InvestmentTypes != null && filters.InvestmentTypes.Count > 0;
            cmd.Parameters.AddWithValue("@HasTypes", hasTypes ? 1 : 0);
            cmd.Parameters.AddWithValue("@Types",
                hasTypes ? string.Join(",", filters.InvestmentTypes!) : string.Empty);

            var offset = (filters.Page - 1) * filters.PageSize;
            cmd.Parameters.AddWithValue("@Offset", filters.ExportAll ? 0 : offset);
            cmd.Parameters.AddWithValue("@PageSize", filters.ExportAll ? int.MaxValue : filters.PageSize);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (totalCount == 0)
                    totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));

                items.Add(new TopPerformingInvestmentRowDto(
                    reader.GetGuid(reader.GetOrdinal("InvestmentId")),
                    reader.GetString(reader.GetOrdinal("InvestmentName")),
                    reader.GetString(reader.GetOrdinal("InvestmentType")),
                    reader.GetDecimal(reader.GetOrdinal("UnitsHeld")),
                    reader.GetDecimal(reader.GetOrdinal("CostBasis")),
                    reader.GetDecimal(reader.GetOrdinal("CurrentValue")),
                    reader.GetDecimal(reader.GetOrdinal("GainLoss")),
                    reader.GetDecimal(reader.GetOrdinal("GainLossPercent"))
                ));
            }

            return new PagedResult<TopPerformingInvestmentRowDto>(
                items,
                totalCount,
                filters.Page,
                filters.PageSize
            );
        }
    }
}
