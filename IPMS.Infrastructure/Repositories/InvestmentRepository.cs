using IPMS.Core.Configs;
using IPMS.Core.Entities;
using IPMS.Core.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IPMS.Infrastructure.Repositories
{
    public sealed class InvestmentRepository : IInvestmentRepository
    {
        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;

        public InvestmentRepository(SqlConnection connection, SqlTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        public void Add(Investment investment)
        {
            const string sql = @"
    INSERT INTO Investments
    (InvestmentId, UserId, InvestmentName, InvestmentType, Status,
     PurchaseDate, TotalUnits, CostBasis)
    VALUES
    (@Id, @UserId, @Name, @Type, @Status, @Date, @Units, @CostBasis)";

            using var cmd = new SqlCommand(sql, _connection, _transaction);

            cmd.Parameters.AddWithValue("@Id", investment.InvestmentId);
            cmd.Parameters.AddWithValue("@UserId", investment.UserId);
            cmd.Parameters.AddWithValue("@Name", investment.InvestmentName);
            cmd.Parameters.AddWithValue("@Type", investment.InvestmentType.Value);
            cmd.Parameters.AddWithValue("@Status", investment.Status.Value);
            cmd.Parameters.AddWithValue("@Date", investment.PurchaseDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@Units", investment.TotalUnits);
            cmd.Parameters.AddWithValue("@CostBasis", investment.CostBasis);

            cmd.ExecuteNonQuery();
        }


        public bool Exists(Guid investmentId)
        {
            const string sql = @"
        SELECT COUNT(1)
        FROM Investments
        WHERE InvestmentId = @InvestmentId AND IsDeleted = 0";

            using var cmd = new SqlCommand(sql, _connection, _transaction);
            cmd.Parameters.AddWithValue("@InvestmentId", investmentId);
            return (int)cmd.ExecuteScalar() > 0;
        }

        public Investment? GetById(Guid investmentId)
        {
            const string sql = @"
        SELECT i.*, t.*
        FROM Investments i
        LEFT JOIN Transactions t ON t.InvestmentId = i.InvestmentId
        WHERE i.InvestmentId = @InvestmentId AND i.IsDeleted = 0";

            using var cmd = new SqlCommand(sql, _connection, _transaction);
            cmd.Parameters.AddWithValue("@InvestmentId", investmentId);

            using var reader = cmd.ExecuteReader();

            Investment? investment = null;

            while (reader.Read())
            {
                if (investment == null)
                {
                    investment = MapInvestment(reader);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("TransactionId")))
                {
                    investment.AddTransaction(MapTransaction(reader));
                }
            }

            return investment;
        }

        public IReadOnlyList<Investment> GetByUserId(Guid userId)
        {
            const string sql = @"
        SELECT * FROM Investments
        WHERE UserId = @UserId AND IsDeleted = 0";

            var result = new List<Investment>();

            using var cmd = new SqlCommand(sql, _connection, _transaction);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapInvestment(reader));
            }

            return result;
        }

        // Soft delete an investment
        public void SoftDelete(Guid investmentId)
        {
            const string sql = @"
        UPDATE Investments
        SET IsDeleted = 1
        WHERE InvestmentId = @InvestmentId";

            using var cmd = new SqlCommand(sql, _connection, _transaction);
            cmd.Parameters.AddWithValue("@InvestmentId", investmentId);
            cmd.ExecuteNonQuery();
        }

        // Update an investment (e.g., TotalUnits, CostBasis, LastTransactionId)
        public void Update(Investment investment)
        {
            const string sql = @"
        UPDATE Investments
        SET InvestmentName = @Name,
            InvestmentType = @Type,
            Status = @Status,
            PurchaseDate = @PurchaseDate,
            TotalUnits = @TotalUnits,
            CostBasis = @CostBasis,
            LastTransactionId = @LastTransactionId
        WHERE InvestmentId = @InvestmentId";

            using var cmd = new SqlCommand(sql, _connection, _transaction);
            cmd.Parameters.AddWithValue("@Name", investment.InvestmentName);
            cmd.Parameters.AddWithValue("@Type", investment.InvestmentType.Value);
            cmd.Parameters.AddWithValue("@Status", investment.Status.Value);
            cmd.Parameters.AddWithValue("@PurchaseDate", investment.PurchaseDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@TotalUnits", investment.TotalUnits);
            cmd.Parameters.AddWithValue("@CostBasis", investment.CostBasis);
            cmd.Parameters.AddWithValue("@LastTransactionId", (object?)investment.LastTransactionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InvestmentId", investment.InvestmentId);

            cmd.ExecuteNonQuery();
        }

        private static Investment MapInvestment(SqlDataReader reader)
        {
            return new Investment(
                reader.GetGuid(reader.GetOrdinal("InvestmentId")),
                reader.GetGuid(reader.GetOrdinal("UserId")),
                reader.GetString(reader.GetOrdinal("InvestmentName")),
                InvestmentType.From(reader.GetString(reader.GetOrdinal("InvestmentType"))),
                InvestmentStatus.From(reader.GetString(reader.GetOrdinal("Status"))),
                DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("PurchaseDate"))),
                reader.GetDecimal(reader.GetOrdinal("TotalUnits")),
                reader.GetDecimal(reader.GetOrdinal("CostBasis")),
                reader.GetGuid(reader.GetOrdinal("LastTransactionId"))
            );
        }

        private static Transaction MapTransaction(SqlDataReader reader)
        {
            return new Transaction(
                reader.GetGuid("TransactionId"),
                reader.GetGuid("InvestmentId"),
                TransactionType.From(reader.GetString("TransactionType")),
                reader.GetDecimal("Units"),
                reader.GetDecimal("UnitPrice"),
                DateOnly.FromDateTime(reader.GetDateTime("TransactionDate")),
                reader.GetGuid("CreatedByUserId")
            );
        }
    }
}
