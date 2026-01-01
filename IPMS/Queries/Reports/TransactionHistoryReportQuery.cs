using IPMS.Models.DTOs.Reports;
using IPMS.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Reports
{
    public sealed class TransactionHistoryReportQuery
    : ITransactionHistoryReportQuery
    {
        private readonly string _connectionString;

        public TransactionHistoryReportQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public PagedResult<TransactionHistoryRowDto> Get(
            Guid userId,
            ReportFiltersRequest filters)
        {
            var offset = (filters.Page - 1) * filters.PageSize;

            const string countSql = @"
SELECT COUNT(*)
FROM Transactions t
JOIN Investments i ON i.InvestmentId = t.InvestmentId
WHERE i.UserId = @UserId
  AND i.IsDeleted = 0
  AND t.IsDeleted = 0
  AND t.TransactionDate BETWEEN @From AND @To
  AND (
      @HasTypes = 0 OR i.InvestmentType IN (SELECT value FROM STRING_SPLIT(@Types, ','))
  );
";

           string listSql = @"
WITH Tx AS (
    SELECT
        t.TransactionId,
        t.TransactionDate,
        t.TransactionType,
        t.Units,
        t.UnitPrice,
        i.InvestmentId,
        i.InvestmentName,
        i.Status,
        i.CostBasis
    FROM Transactions t
    JOIN Investments i ON i.InvestmentId = t.InvestmentId
    WHERE i.UserId = @UserId
      AND i.IsDeleted = 0
      AND t.IsDeleted = 0
      AND t.TransactionDate BETWEEN @From AND @To
      AND (
          @HasTypes = 0 OR i.InvestmentType IN (SELECT value FROM STRING_SPLIT(@Types, ','))
      )
),
UnitsAtTxDate AS (
    SELECT
        tx.TransactionId,
        SUM(
            CASE t2.TransactionType
                WHEN 'Buy' THEN t2.Units
                WHEN 'Sell' THEN -t2.Units
            END
        ) AS UnitsHeld
    FROM Tx tx
    JOIN Transactions t2
        ON t2.InvestmentId = tx.InvestmentId
       AND t2.TransactionDate <= tx.TransactionDate
       AND t2.IsDeleted = 0
    GROUP BY tx.TransactionId
),
PriceAtTxDate AS (
    SELECT
        tx.TransactionId,
        (
            SELECT TOP 1 ph.UnitPrice
            FROM PriceHistory ph
            WHERE ph.InvestmentId = tx.InvestmentId
              AND ph.PriceDate <= tx.TransactionDate
            ORDER BY ph.PriceDate DESC
        ) AS UnitPrice
    FROM Tx tx
)
SELECT
    tx.TransactionId,
    tx.TransactionDate,
    tx.InvestmentName,
    tx.Status,
    tx.TransactionType,
    tx.Units,
    tx.UnitPrice,
    (tx.Units * tx.UnitPrice) AS Amount,
    (u.UnitsHeld * p.UnitPrice) AS TotalValueAtDate,
    ((u.UnitsHeld * p.UnitPrice) - tx.CostBasis) AS GainLossAtDate
FROM Tx tx
JOIN UnitsAtTxDate u ON u.TransactionId = tx.TransactionId
JOIN PriceAtTxDate p ON p.TransactionId = tx.TransactionId
ORDER BY tx.TransactionDate DESC" + (filters.ExportAll ? "" : " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");



            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            int totalCount;
            using (var countCmd = new SqlCommand(countSql, conn))
            {
                AddParams(countCmd, userId, filters);
                totalCount = (int)countCmd.ExecuteScalar()!;
            }

            var items = new List<TransactionHistoryRowDto>();
            using (var listCmd = new SqlCommand(listSql, conn))
            {
                AddParams(listCmd, userId, filters);
                listCmd.Parameters.AddWithValue("@Offset", offset);
                listCmd.Parameters.AddWithValue("@PageSize", filters.PageSize);

                using var reader = listCmd.ExecuteReader();
                while (reader.Read())
                {
                    items.Add(new TransactionHistoryRowDto(
                        reader.GetGuid(0),
                        DateOnly.FromDateTime(reader.GetDateTime(1)),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetString(4),
                        reader.GetDecimal(5),
                        reader.GetDecimal(6),
                        reader.GetDecimal(7),
                        reader.GetDecimal(8),
                        reader.GetDecimal(9)
                    ));
                }
            }

            return new PagedResult<TransactionHistoryRowDto>(
                items,
                totalCount,
                filters.Page,
                filters.PageSize
            );
        }

        private static void AddParams(
            SqlCommand cmd,
            Guid userId,
            ReportFiltersRequest filters)
        {
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@From", filters.FromDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@To", filters.ToDate.ToDateTime(TimeOnly.MaxValue));

            var hasTypes = filters.InvestmentTypes?.Any() == true;
            cmd.Parameters.AddWithValue("@HasTypes", hasTypes ? 1 : 0);
            cmd.Parameters.AddWithValue("@Types",
                hasTypes ? string.Join(",", filters.InvestmentTypes!) : "");
        }
    }
}
