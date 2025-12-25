using IPMS.Models.DTOs.Investments;
using IPMS.Models.Filters;
using Microsoft.Data.SqlClient;

namespace IPMS.Queries.Investments
{
    public sealed class InvestmentListQuery : IInvestmentListQuery
    {
        private readonly string _connectionString;

        public InvestmentListQuery(string connectionString)
        {
            _connectionString = connectionString;
        }

        public InvestmentListResultDto Get(Guid userId, InvestmentListFilter filter)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // 1️⃣ Get total count
            int totalCount = ExecuteCountQuery(conn, userId, filter);

            // 2️⃣ Get paged data
            var items = ExecuteListQuery(conn, userId, filter);

            return new InvestmentListResultDto(
                items,
                totalCount,
                filter.Page,
                filter.PageSize
            );
        }

        private static int ExecuteCountQuery(
    SqlConnection conn,
    Guid userId,
    InvestmentListFilter filter)
        {
            const string sql = @"
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
        i.InvestmentId,
        i.InvestmentName,
        i.InvestmentType,
        i.PurchaseDate,
        i.Status,
        i.CostBasis,
        i.TotalUnits,
        lp.UnitPrice,
        (i.TotalUnits * lp.UnitPrice) AS CurrentValue,
        CASE 
            WHEN i.CostBasis = 0 THEN 0
            ELSE ((i.TotalUnits * lp.UnitPrice - i.CostBasis) / i.CostBasis) * 100
        END AS GainLossPercent
    FROM Investments i
    JOIN LatestPrice lp
        ON lp.InvestmentId = i.InvestmentId
       AND lp.rn = 1
    WHERE i.UserId = @UserId
      AND i.IsDeleted = 0
)
SELECT COUNT(1)
FROM BaseData
WHERE
    (@Search IS NULL OR InvestmentName LIKE '%' + @Search + '%')
    AND (@Type IS NULL OR InvestmentType = @Type)
    AND (@Status IS NULL OR Status = @Status)
    AND (@FromDate IS NULL OR PurchaseDate >= @FromDate)
    AND (@ToDate IS NULL OR PurchaseDate <= @ToDate)
    AND (@MinGainLoss IS NULL OR GainLossPercent >= @MinGainLoss)
    AND (@MaxGainLoss IS NULL OR GainLossPercent <= @MaxGainLoss);
";

            using var cmd = new SqlCommand(sql, conn);

            AddFilterParameters(cmd, userId, filter);

            return (int)cmd.ExecuteScalar()!;
        }

        private static IReadOnlyList<InvestmentListItemDto> ExecuteListQuery(
        SqlConnection conn,
        Guid userId,
        InvestmentListFilter filter)
        {
            string sql = @"
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
        i.InvestmentId,
        i.InvestmentName,
        i.InvestmentType,
        i.PurchaseDate,
        i.Status,
        i.CostBasis,
        i.TotalUnits,
        lp.UnitPrice,
        (i.TotalUnits * lp.UnitPrice) AS CurrentValue,
        CASE 
            WHEN i.CostBasis = 0 THEN 0
            ELSE ((i.TotalUnits * lp.UnitPrice - i.CostBasis) / i.CostBasis) * 100
        END AS GainLossPercent
    FROM Investments i
    JOIN LatestPrice lp
        ON lp.InvestmentId = i.InvestmentId
       AND lp.rn = 1
    WHERE i.UserId = @UserId
      AND i.IsDeleted = 0
)
SELECT
    InvestmentId,
    InvestmentName,
    InvestmentType,
    CostBasis,
    CurrentValue,
    GainLossPercent,
    PurchaseDate,
    Status
FROM BaseData
WHERE
    (@Search IS NULL OR InvestmentName LIKE '%' + @Search + '%')
    AND (@Type IS NULL OR InvestmentType = @Type)
    AND (@Status IS NULL OR Status = @Status)
    AND (@FromDate IS NULL OR PurchaseDate >= @FromDate)
    AND (@ToDate IS NULL OR PurchaseDate <= @ToDate)
    AND (@MinGainLoss IS NULL OR GainLossPercent >= @MinGainLoss)
    AND (@MaxGainLoss IS NULL OR GainLossPercent <= @MaxGainLoss)
ORDER BY
    CASE WHEN @SortBy = 'Amount' THEN CostBasis END,
    CASE WHEN @SortBy = 'CurrentValue' THEN CurrentValue END,
    CASE WHEN @SortBy = 'GainLoss' THEN GainLossPercent END,
    CASE WHEN @SortBy = 'Date' THEN PurchaseDate END
    " + (filter.SortDirection == "ASC" ? "ASC" : "DESC") + @"
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;
";

            using var cmd = new SqlCommand(sql, conn);

            AddFilterParameters(cmd, userId, filter);

            cmd.Parameters.AddWithValue("@Offset", (filter.Page - 1) * filter.PageSize);
            cmd.Parameters.AddWithValue("@PageSize", filter.PageSize);
            cmd.Parameters.AddWithValue("@SortBy", filter.SortBy);

            var result = new List<InvestmentListItemDto>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new InvestmentListItemDto(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDecimal(3),
                    reader.GetDecimal(4),
                    reader.GetDecimal(5),
                    DateOnly.FromDateTime(reader.GetDateTime(6)),
                    reader.GetString(7)
                ));
            }

            return result;
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
