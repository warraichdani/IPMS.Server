using IPMS.Models.DTOs.Investments;
using IPMS.Models.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Investments
{
    public sealed class InvestmentExportQuery : IInvestmentExportQuery
    {
        private readonly string _connectionString;

        public InvestmentExportQuery(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public IReadOnlyList<InvestmentExportRowDto> Export(
            Guid userId,
            InvestmentListFilter filter)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            return ExecuteExportQuery(conn, userId, filter);
        }

        private static IReadOnlyList<InvestmentExportRowDto> ExecuteExportQuery(
            SqlConnection conn,
            Guid userId,
            InvestmentListFilter filter)
        {
            var sql = @"
WITH LatestPrice AS (
    SELECT
        ph.InvestmentId,
        ph.UnitPrice,
        ROW_NUMBER() OVER (
            PARTITION BY ph.InvestmentId
            ORDER BY ph.PriceDate DESC
        ) AS rn
    FROM PriceHistory ph
),
BaseData AS (
    SELECT
        i.InvestmentName,
        i.InvestmentType,
        i.CostBasis,
        (i.TotalUnits * lp.UnitPrice) AS CurrentValue,
        CASE 
            WHEN i.CostBasis = 0 THEN 0
            ELSE ((i.TotalUnits * lp.UnitPrice - i.CostBasis) / i.CostBasis) * 100
        END AS GainLossPercent,
        i.PurchaseDate,
        i.Status
    FROM Investments i
    JOIN LatestPrice lp
        ON lp.InvestmentId = i.InvestmentId
       AND lp.rn = 1
    WHERE i.UserId = @UserId
      AND i.IsDeleted = 0
)
SELECT *
FROM BaseData
WHERE
    (@Search IS NULL OR InvestmentName LIKE '%' + @Search + '%')
    AND (@Type IS NULL OR InvestmentType = @Type)
    AND (@Status IS NULL OR Status = @Status)
    AND (@FromDate IS NULL OR PurchaseDate >= @FromDate)
    AND (@ToDate IS NULL OR PurchaseDate <= @ToDate)
    AND (@MinGainLoss IS NULL OR GainLossPercent >= @MinGainLoss)
    AND (@MaxGainLoss IS NULL OR GainLossPercent <= @MaxGainLoss)
ORDER BY PurchaseDate DESC;
";

            using var cmd = new SqlCommand(sql, conn);
            AddFilterParameters(cmd, userId, filter);

            var rows = new List<InvestmentExportRowDto>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new InvestmentExportRowDto(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetDecimal(2),
                    reader.GetDecimal(3),
                    reader.GetDecimal(4),
                    DateOnly.FromDateTime(reader.GetDateTime(5)),
                    reader.GetString(6)
                ));
            }

            return rows;
        }

        private static void AddFilterParameters(
            SqlCommand cmd,
            Guid userId,
            InvestmentListFilter filter)
        {
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Search", (object?)filter.Search ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Type", (object?)filter.Type ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", (object?)filter.Status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FromDate", (object?)filter.FromDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)filter.ToDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MinGainLoss", (object?)filter.MinGainLossPercent ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MaxGainLoss", (object?)filter.MaxGainLossPercent ?? DBNull.Value);
        }
    }

}
