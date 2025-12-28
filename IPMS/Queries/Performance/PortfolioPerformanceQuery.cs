using IPMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Performance
{
    public sealed class PortfolioPerformanceQuery : IPortfolioPerformanceQuery
    {
        private readonly string _connectionString;

        public PortfolioPerformanceQuery(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public IReadOnlyList<PerformancePointDto> GetLast12Months(Guid userId)
        {
            const string sql = @"WITH Months AS (
                SELECT CAST(DATEADD(MONTH, -11, EOMONTH(GETUTCDATE())) AS DATE) AS ValuationDate
                UNION ALL
                SELECT DATEADD(MONTH, 1, ValuationDate)
                FROM Months
                WHERE ValuationDate < EOMONTH(GETUTCDATE())
            ),
            UserInvestments AS (
                SELECT InvestmentId
                FROM Investments
                WHERE UserId = @UserId
                  AND IsDeleted = 0
            ),
            UnitsAtDate AS (
                SELECT
                    m.ValuationDate,
                    t.InvestmentId,
                    SUM(
                        CASE t.TransactionType
                            WHEN 'Buy' THEN t.Units
                            WHEN 'Sell' THEN -t.Units
                        END
                    ) AS UnitsOwned
                FROM Months m
                JOIN Transactions t
                    ON t.TransactionDate <= m.ValuationDate
                JOIN UserInvestments ui
                    ON ui.InvestmentId = t.InvestmentId
                WHERE t.IsDeleted = 0
                GROUP BY m.ValuationDate, t.InvestmentId
            ),
            PriceAtDate AS (
                SELECT
                    m.ValuationDate,
                    ui.InvestmentId,
                    (
                        SELECT TOP 1 ph.UnitPrice
                        FROM PriceHistory ph
                        WHERE ph.InvestmentId = ui.InvestmentId
                          AND ph.PriceDate <= m.ValuationDate
                        ORDER BY ph.PriceDate DESC
                    ) AS UnitPrice
                FROM Months m
                CROSS JOIN UserInvestments ui
            )
            SELECT
                u.ValuationDate,
                SUM(u.UnitsOwned * p.UnitPrice) AS TotalMarketValue
            FROM UnitsAtDate u
            JOIN PriceAtDate p
                ON p.ValuationDate = u.ValuationDate
               AND p.InvestmentId = u.InvestmentId
            GROUP BY u.ValuationDate
            ORDER BY u.ValuationDate
            OPTION (MAXRECURSION 12);
            ";

            var result = new List<PerformancePointDto>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new PerformancePointDto(
                    DateOnly.FromDateTime(reader.GetDateTime(0)),
                    reader.GetDecimal(1)
                ));
            }

            return result;
        }
    }

}
