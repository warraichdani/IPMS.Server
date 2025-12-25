using IPMS.Models.DTOs.Investments;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Queries.Investments
{
    public sealed class InvestmentEditQuery : IInvestmentEditQuery
    {
        private readonly string _connectionString;

        public InvestmentEditQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public EditInvestmentDto? Get(Guid investmentId, Guid userId)
        {
            const string sql = @"
SELECT
    InvestmentId,
    InvestmentName,
    InvestmentType,
    CostBasis       AS InitialAmount,
    PurchaseDate,
    Broker,
    Notes,
    Status
FROM Investments
WHERE InvestmentId = @InvestmentId
  AND UserId = @UserId
  AND IsDeleted = 0;
";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@InvestmentId", investmentId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new EditInvestmentDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDecimal(3),
                DateOnly.FromDateTime(reader.GetDateTime(4)),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetString(7)
            );
        }
    }

}
