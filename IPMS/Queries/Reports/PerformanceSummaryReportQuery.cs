using IPMS.Models.DTOs.Reports;
using IPMS.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Reflection.PortableExecutable;

namespace IPMS.Queries.Reports
{
    public sealed class PerformanceSummaryReportQuery : IPerformanceSummaryReportQuery
    {
        private readonly string _connectionString;

        public PerformanceSummaryReportQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<PagedResult<PerformanceSummaryDto>> GetAsync(
            Guid userId,
            PerformanceSummaryRangeRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sqlText = @"WITH DateRange AS (
    SELECT @FromDate AS [Date]
    UNION ALL
    SELECT DATEADD(DAY, 1, [Date])
    FROM DateRange
    WHERE [Date] < @ToDate
),
InvestmentUniverse AS (
    SELECT InvestmentId, InvestmentName, InvestmentType, CostBasis
    FROM Investments
    WHERE UserId = @UserId
      AND IsDeleted = 0
      AND (
          @HasTypes = 0 OR InvestmentType IN (SELECT value FROM STRING_SPLIT(@Types, ','))
      )
),
UnitsByDay AS (
    SELECT
        d.[Date],
        u.InvestmentId,
        u.TotalUnits
    FROM DateRange d
    CROSS APPLY dbo.fn_UnitsHeldAsOf(@UserId, d.[Date], NULL) u
),
PriceByDay AS (
    SELECT
        d.[Date],
        i.InvestmentId,
        ph.UnitPrice
    FROM DateRange d
    JOIN InvestmentUniverse i ON 1 = 1
    OUTER APPLY (
        SELECT TOP 1 UnitPrice
        FROM PriceHistory ph
        WHERE ph.InvestmentId = i.InvestmentId
          AND ph.PriceDate <= d.[Date]
        ORDER BY ph.PriceDate DESC
    ) ph
),
DailyInvestmentStats AS (
    SELECT
        d.[Date],
        i.InvestmentId,
        i.InvestmentName,
        u.TotalUnits,
        p.UnitPrice,
        (u.TotalUnits * p.UnitPrice) AS CurrentValue,
        ((u.TotalUnits * p.UnitPrice) - i.CostBasis) AS GainLoss,
        CASE
            WHEN i.CostBasis = 0 THEN 0
            ELSE (((u.TotalUnits * p.UnitPrice) - i.CostBasis) / i.CostBasis) * 100
        END AS GainLossPercent,
        i.CostBasis
    FROM DateRange d
    JOIN InvestmentUniverse i ON 1 = 1
    JOIN UnitsByDay u
        ON u.[Date] = d.[Date]
       AND u.InvestmentId = i.InvestmentId
    JOIN PriceByDay p
        ON p.[Date] = d.[Date]
       AND p.InvestmentId = i.InvestmentId
    WHERE
        u.TotalUnits > 0         -- investment held
        AND p.UnitPrice IS NOT NULL -- investment priced
),
DailyPortfolio AS (
    SELECT
        [Date],
        SUM(CurrentValue) AS TotalCurrentValue,
        SUM(GainLoss) AS TotalGainLoss,
        CASE
            WHEN SUM(i.CostBasis) = 0 THEN 0
            ELSE SUM(GainLoss) / SUM(i.CostBasis) * 100
        END AS TotalGainLossPercent,
        COUNT(*) AS ActiveCount
    FROM DailyInvestmentStats i
    GROUP BY [Date]
),
FinalResult AS (
    SELECT
        p.[Date],
        p.TotalCurrentValue,
        p.TotalGainLoss,
        p.TotalGainLossPercent,
        p.ActiveCount,
        bp.InvestmentName AS BestPerformerName,
        bp.GainLossPercent AS BestPerformerGain,
        wp.InvestmentName AS WorstPerformerName,
        wp.GainLossPercent AS WorstPerformerGain,
		COUNT(*) OVER() AS TotalCount
    FROM DailyPortfolio p
    OUTER APPLY (
        SELECT TOP 1 InvestmentName, GainLossPercent
        FROM DailyInvestmentStats
        WHERE [Date] = p.[Date]
        ORDER BY GainLossPercent DESC
    ) bp
    OUTER APPLY (
        SELECT TOP 1 InvestmentName, GainLossPercent
        FROM DailyInvestmentStats
        WHERE [Date] = p.[Date]
        ORDER BY GainLossPercent ASC
    ) wp
)" + (request.ExportAll ? "SELECT * FROM FinalResult ORDER BY [Date];" : "SELECT * FROM FinalResult ORDER BY [Date] OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");


            using var cmd = new SqlCommand(sqlText, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@FromDate", request.FromDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@ToDate", request.ToDate.ToDateTime(TimeOnly.MinValue));

            var types = request.InvestmentTypes is { Count: > 0 }
                ? string.Join(",", request.InvestmentTypes)
                : string.Empty;

            cmd.Parameters.AddWithValue("@Types", types);
            cmd.Parameters.AddWithValue("@HasTypes", request.InvestmentTypes?.Count > 0 ? 1 : 0);

            int page = request.ExportAll ? 1 : request.Page;
            int pageSize = request.ExportAll ? int.MaxValue : request.PageSize;
            int offset = (page - 1) * pageSize;

            cmd.Parameters.AddWithValue("@Offset", offset);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            int totalCount;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                totalCount = 0;
                var items = new List<PerformanceSummaryDto>();

                while (reader.Read())
                {
                    if (totalCount == 0)
                        totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));

                    items.Add(new PerformanceSummaryDto(
                        DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("Date"))),
                        reader.GetDecimal(reader.GetOrdinal("TotalCurrentValue")),
                        reader.GetDecimal(reader.GetOrdinal("TotalGainLoss")),
                        reader.GetDecimal(reader.GetOrdinal("TotalGainLossPercent")),
                        reader.GetInt32(reader.GetOrdinal("ActiveCount")),
                        reader.IsDBNull(reader.GetOrdinal("BestPerformerName")) ? null : reader.GetString(reader.GetOrdinal("BestPerformerName")),
                        reader.IsDBNull(reader.GetOrdinal("BestPerformerGain")) ? null : reader.GetDecimal(reader.GetOrdinal("BestPerformerGain")),
                        reader.IsDBNull(reader.GetOrdinal("WorstPerformerName")) ? null : reader.GetString(reader.GetOrdinal("WorstPerformerName")),
                        reader.IsDBNull(reader.GetOrdinal("WorstPerformerGain")) ? null : reader.GetDecimal(reader.GetOrdinal("WorstPerformerGain"))
                    ));
                }

                return new PagedResult<PerformanceSummaryDto>(
                    items,
                    totalCount,
                    page,
                    pageSize
                );
            }
        }
    }
}
