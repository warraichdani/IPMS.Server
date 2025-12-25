using IPMS.Models.DTOs.Transactions;
using IPMS.Models.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Queries.Transactions
{
    public sealed class TransactionQuery : ITransactionQuery
    {
        private readonly string _connectionString;

        public TransactionQuery(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public IReadOnlyList<TransactionListItemDto> GetTransactionsForInvestment(
            Guid investmentId,
            Guid userId,
            TransactionListFilter filter,
            out int totalCount)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            totalCount = GetTotalCount(conn, investmentId, userId, filter);
            return GetPagedTransactions(conn, investmentId, userId, filter);
        }

        private int GetTotalCount(
            SqlConnection conn,
            Guid investmentId,
            Guid userId,
            TransactionListFilter filter)
        {
            const string sql = @"
SELECT COUNT(1)
FROM Transactions t
JOIN Investments i ON i.InvestmentId = t.InvestmentId
WHERE t.InvestmentId = @InvestmentId
  AND i.UserId = @UserId
  AND t.IsDeleted = 0
  AND (@TransactionType IS NULL OR t.TransactionType = @TransactionType)
  AND (@FromDate IS NULL OR t.TransactionDate >= @FromDate)
  AND (@ToDate IS NULL OR t.TransactionDate <= @ToDate);";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@InvestmentId", investmentId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@TransactionType", (object?)filter.TransactionType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FromDate", (object?)filter.FromDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)filter.ToDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);

            return (int)cmd.ExecuteScalar()!;
        }

        private IReadOnlyList<TransactionListItemDto> GetPagedTransactions(
            SqlConnection conn,
            Guid investmentId,
            Guid userId,
            TransactionListFilter filter)
        {
            var sortColumn = filter.SortBy switch
            {
                "Units" => "Units",
                "Amount" => "Units * UnitPrice",
                _ => "TransactionDate"
            };

            var sortDirection = filter.SortDirection?.ToUpperInvariant() == "ASC" ? "ASC" : "DESC";

            var sql = $@"
SELECT t.TransactionId,
       t.TransactionType,
       t.Units,
       t.UnitPrice,
       t.Units * t.UnitPrice AS Amount,
       t.TransactionDate,
       t.Notes
FROM Transactions t
JOIN Investments i ON i.InvestmentId = t.InvestmentId
WHERE t.InvestmentId = @InvestmentId
  AND i.UserId = @UserId
  AND t.IsDeleted = 0
  AND (@TransactionType IS NULL OR t.TransactionType = @TransactionType)
  AND (@FromDate IS NULL OR t.TransactionDate >= @FromDate)
  AND (@ToDate IS NULL OR t.TransactionDate <= @ToDate)
ORDER BY {sortColumn} {sortDirection}
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@InvestmentId", investmentId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@TransactionType", (object?)filter.TransactionType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FromDate", (object?)filter.FromDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)filter.ToDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Offset", (filter.Page - 1) * filter.PageSize);
            cmd.Parameters.AddWithValue("@PageSize", filter.PageSize);

            var list = new List<TransactionListItemDto>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TransactionListItemDto(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetDecimal(2),
                    reader.GetDecimal(3),
                    reader.GetDecimal(4),
                    DateOnly.FromDateTime(reader.GetDateTime(5)),
                    reader.IsDBNull(6) ? null : reader.GetString(6)
                ));
            }

            return list;
        }
    }
}
