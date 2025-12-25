using IPMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Queries.Allocation
{
    public sealed class AllocationQuery : IAllocationQuery
    {
        private readonly string _connectionString;

        public AllocationQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public IReadOnlyList<AllocationDto> GetByUser(Guid userId)
        {
            const string sql = @"
        WITH CurrentUnits AS (
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
            JOIN Transactions t ON t.InvestmentId = i.InvestmentId
            WHERE i.UserId = @UserId
              AND i.IsDeleted = 0
              AND t.IsDeleted = 0
            GROUP BY i.InvestmentType, i.InvestmentId
        ),
        LatestPrices AS (
            SELECT
                ph.InvestmentId,
                ph.UnitPrice
            FROM PriceHistory ph
            WHERE ph.PriceDate = (
                SELECT MAX(ph2.PriceDate)
                FROM PriceHistory ph2
                WHERE ph2.InvestmentId = ph.InvestmentId
            )
        )
        SELECT
            cu.InvestmentType,
            SUM(cu.UnitsOwned * lp.UnitPrice) AS TotalValue
        FROM CurrentUnits cu
        JOIN LatestPrices lp
            ON cu.InvestmentId = lp.InvestmentId
        GROUP BY cu.InvestmentType;";

            var result = new List<AllocationDto>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

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
