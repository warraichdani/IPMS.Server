using IPMS.Models.DTOs.Dashboard;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Queries.Dashboard
{
    public sealed class UserDashboardQuery : IUserDashboardQuery
    {
        private readonly string _connectionString;

        public UserDashboardQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public DashboardSummaryDto Get(Guid userId)
        {
            const string sql = @"
WITH InvestmentStats AS (
    SELECT
        InvestmentId,
        InvestmentName,
        Status,
        CostBasis,
        (TotalUnits * UnitPrice) AS CurrentValue,
        ((TotalUnits * UnitPrice) - CostBasis) AS GainLoss,
        CASE 
            WHEN CostBasis = 0 THEN 0
            ELSE (((TotalUnits * UnitPrice) - CostBasis) / CostBasis) * 100
        END AS GainLossPercent
    FROM Investments
    WHERE UserId = @UserId
      AND IsDeleted = 0
)
SELECT
    -- Portfolio totals
    SUM(CurrentValue) AS TotalCurrentValue,
    SUM(GainLoss) AS TotalGainLoss,
    CASE 
        WHEN SUM(CostBasis) = 0 THEN 0
        ELSE (SUM(GainLoss) / SUM(CostBasis)) * 100
    END AS TotalGainLossPercent,

    -- Active investments count
    SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END) AS ActiveCount,

    -- Best performer
    (SELECT TOP 1 InvestmentName
     FROM InvestmentStats
     ORDER BY GainLossPercent DESC) AS BestPerformerName,

    (SELECT TOP 1 GainLossPercent
     FROM InvestmentStats
     ORDER BY GainLossPercent DESC) AS BestPerformerGain,

    -- Worst performer
    (SELECT TOP 1 InvestmentName
     FROM InvestmentStats
     ORDER BY GainLossPercent ASC) AS WorstPerformerName,

    (SELECT TOP 1 GainLossPercent
     FROM InvestmentStats
     ORDER BY GainLossPercent ASC) AS WorstPerformerGain
FROM InvestmentStats;
";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return new DashboardSummaryDto(
                    0, 0, 0, 0, null, null, null, null);
            }

            return new DashboardSummaryDto(
                reader.GetDecimal(0),
                reader.GetDecimal(1),
                reader.GetDecimal(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetDecimal(7)
            );
        }
    }
}
