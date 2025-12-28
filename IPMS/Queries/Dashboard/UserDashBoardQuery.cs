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

        public async Task<DashboardSummaryDto> Get(Guid userId)
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
    SUM(CurrentValue) AS TotalCurrentValue,
    SUM(GainLoss) AS TotalGainLoss,
    CASE 
        WHEN SUM(CostBasis) = 0 THEN 0
        ELSE (SUM(GainLoss) / SUM(CostBasis)) * 100
    END AS TotalGainLossPercent,
    SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END) AS ActiveCount,
    (SELECT TOP 1 InvestmentName FROM InvestmentStats ORDER BY GainLossPercent DESC) AS BestPerformerName,
    (SELECT TOP 1 GainLossPercent FROM InvestmentStats ORDER BY GainLossPercent DESC) AS BestPerformerGain,
    (SELECT TOP 1 InvestmentName FROM InvestmentStats ORDER BY GainLossPercent ASC) AS WorstPerformerName,
    (SELECT TOP 1 GainLossPercent FROM InvestmentStats ORDER BY GainLossPercent ASC) AS WorstPerformerGain
FROM InvestmentStats;
";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                return DashboardSummaryDto.Empty();
            }

            int ordTotalValue = reader.GetOrdinal("TotalCurrentValue");
            int ordTotalGain = reader.GetOrdinal("TotalGainLoss");
            int ordTotalGainPct = reader.GetOrdinal("TotalGainLossPercent");
            int ordActiveCount = reader.GetOrdinal("ActiveCount");
            int ordBestName = reader.GetOrdinal("BestPerformerName");
            int ordBestGain = reader.GetOrdinal("BestPerformerGain");
            int ordWorstName = reader.GetOrdinal("WorstPerformerName");
            int ordWorstGain = reader.GetOrdinal("WorstPerformerGain");

            return new DashboardSummaryDto(
                reader.IsDBNull(ordTotalValue) ? 0m : reader.GetDecimal(ordTotalValue),
                reader.IsDBNull(ordTotalGain) ? 0m : reader.GetDecimal(ordTotalGain),
                reader.IsDBNull(ordTotalGainPct) ? 0m : reader.GetDecimal(ordTotalGainPct),
                reader.IsDBNull(ordActiveCount) ? 0 : reader.GetInt32(ordActiveCount),
                reader.IsDBNull(ordBestName) ? null : reader.GetString(ordBestName),
                reader.IsDBNull(ordBestGain) ? null : reader.GetDecimal(ordBestGain),
                reader.IsDBNull(ordWorstName) ? null : reader.GetString(ordWorstName),
                reader.IsDBNull(ordWorstGain) ? null : reader.GetDecimal(ordWorstGain)
            );
        }
    }
}
