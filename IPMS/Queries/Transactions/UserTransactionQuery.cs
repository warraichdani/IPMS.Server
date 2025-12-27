using IPMS.Models.DTOs.Transactions;
using IPMS.Models.Filters;
using IPMS.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Queries.Transactions
{
    public sealed class UserTransactionQuery : IUserTransactionQuery
    {
        private readonly string _connectionString;

        public UserTransactionQuery(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public PagedResult<AllTransactionListItemDto> Get(
            Guid userId,
            AllTransactionListFilter filter)
        {
            var offset = (filter.Page - 1) * filter.PageSize;

            var where = new List<string>
        {
            "i.UserId = @UserId",
            "i.IsDeleted = 0",
            "t.IsDeleted = 0"
        };

            if (!string.IsNullOrWhiteSpace(filter.InvestmentId))
                where.Add("t.InvestmentId = @InvestmentId");

            if (!string.IsNullOrWhiteSpace(filter.InvestmentName))
                where.Add("i.InvestmentName LIKE @InvestmentName");

            if (!string.IsNullOrWhiteSpace(filter.TransactionType))
                where.Add("t.TransactionType = @Type");

            if (filter.From.HasValue)
                where.Add("t.TransactionDate >= @From");

            if (filter.To.HasValue)
                where.Add("t.TransactionDate <= @To");

            var whereSql = string.Join(" AND ", where);

            var countSql = $@"
SELECT COUNT(*)
FROM Transactions t
JOIN Investments i ON i.InvestmentId = t.InvestmentId
WHERE {whereSql};
";

            var listSql = $@"
SELECT
    t.TransactionId,
    t.InvestmentId,
    i.InvestmentName,
    t.TransactionType,
    t.Units,
    t.UnitPrice,
    (t.Units * t.UnitPrice) AS Amount,
    CAST(t.TransactionDate AS date) AS Date
FROM Transactions t
JOIN Investments i ON i.InvestmentId = t.InvestmentId
WHERE {whereSql}
ORDER BY t.TransactionDate DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            int totalCount;
            using (var countCmd = new SqlCommand(countSql, conn))
            {
                AddParameters(countCmd, userId, filter);
                totalCount = (int)countCmd.ExecuteScalar()!;
            }

            var items = new List<AllTransactionListItemDto>();
            using (var listCmd = new SqlCommand(listSql, conn))
            {
                AddParameters(listCmd, userId, filter);
                listCmd.Parameters.AddWithValue("@Offset", offset);
                listCmd.Parameters.AddWithValue("@PageSize", filter.PageSize);

                using var reader = listCmd.ExecuteReader();
                while (reader.Read())
                {
                    items.Add(new AllTransactionListItemDto(
                        reader.GetGuid(0),
                        reader.GetGuid(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetDecimal(4),
                        reader.GetDecimal(5),
                        reader.GetDecimal(6),
                        DateOnly.FromDateTime(reader.GetDateTime(7))
                    ));
                }
            }

            return new PagedResult<AllTransactionListItemDto>(
                items,
                totalCount,
                filter.Page,
                filter.PageSize);
        }

        private static void AddParameters(
            SqlCommand cmd,
            Guid userId,
            AllTransactionListFilter filter)
        {
            cmd.Parameters.AddWithValue("@UserId", userId);

            if (!string.IsNullOrWhiteSpace(filter.InvestmentId))
                cmd.Parameters.AddWithValue("@InvestmentId", filter.InvestmentId);

            if (!string.IsNullOrWhiteSpace(filter.InvestmentName))
                cmd.Parameters.AddWithValue(
                    "@InvestmentName",
                    $"%{filter.InvestmentName.Trim()}%"   // ✅ partial match
                );

            if (!string.IsNullOrWhiteSpace(filter.TransactionType))
                cmd.Parameters.AddWithValue("@Type", filter.TransactionType);

            if (filter.From.HasValue)
                cmd.Parameters.AddWithValue("@From",
                    filter.From.Value.ToDateTime(TimeOnly.MinValue));

            if (filter.To.HasValue)
                cmd.Parameters.AddWithValue("@To",
                    filter.To.Value.ToDateTime(TimeOnly.MaxValue));
        }
    }
}
