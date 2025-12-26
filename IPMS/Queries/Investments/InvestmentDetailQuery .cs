using IPMS.Models.DTOs.Investments;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Queries.Investments
{
    public sealed class InvestmentDetailQuery : IInvestmentDetailQuery
    {
        private readonly string _connectionString;

        public InvestmentDetailQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public InvestmentDetailDto Get(Guid investmentId, Guid userId)
        {
            const string sql = @"
SELECT
    InvestmentId,
    InvestmentName,
    InvestmentType,
    Status,
    PurchaseDate,
    CostBasis,
    TotalUnits,
    UnitPrice,
    Broker,
    Notes,
    InitialAmount
FROM Investments
WHERE InvestmentId = @InvestmentId
  AND UserId = @UserId
  AND IsDeleted = 0";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@InvestmentId", investmentId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            conn.Open();

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                throw new InvalidOperationException("Investment not found.");

            var costBasis = reader.GetDecimal(5);
            var totalUnits = reader.GetDecimal(6);
            var unitPrice = reader.GetDecimal(7);

            var currentValue = totalUnits * unitPrice;

            var gainLossPercent =
                costBasis == 0
                    ? 0
                    : ((currentValue - costBasis) / costBasis) * 100;

            return new InvestmentDetailDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                currentValue,
                gainLossPercent,
                reader.GetString(3),
                DateOnly.FromDateTime(reader.GetDateTime(4)),
                costBasis,
                totalUnits,
                unitPrice,
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetDecimal(10)
            );
        }
    }
}
