using IPMS.Core.Configs;
using IPMS.Core.Entities;
using IPMS.Core.Repositories;
using Microsoft.Data.SqlClient;

namespace IPMS.Infrastructure.Repositories
{
    internal class TransactionRepository : ITransactionRepository
    {
        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;

        public TransactionRepository(SqlConnection connection, SqlTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }
        public void Add(Transaction transaction)
        {
            const string sql = @"
    INSERT INTO Transactions
    (TransactionId, InvestmentId, TransactionType,
     Units, UnitPrice, TransactionDate, CreatedByUserId)
    VALUES
    (@Id, @InvestmentId, @Type, @Units, @Price, @Date, @UserId)";

            using var cmd = new SqlCommand(sql, _connection, _transaction);

            cmd.Parameters.AddWithValue("@Id", transaction.TransactionId);
            cmd.Parameters.AddWithValue("@InvestmentId", transaction.InvestmentId);
            cmd.Parameters.AddWithValue("@Type", transaction.Type.Value);
            cmd.Parameters.AddWithValue("@Units", transaction.Units);
            cmd.Parameters.AddWithValue("@Price", transaction.UnitPrice);
            cmd.Parameters.AddWithValue("@Date", transaction.TransactionDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@UserId", transaction.CreatedByUserId);

            cmd.ExecuteNonQuery();
        }


        public Transaction? GetById(Guid transactionId)
        {
            const string sql = @"
        SELECT * FROM Transactions
        WHERE TransactionId = @TransactionId AND IsDeleted = 0";

            using var cmd = new SqlCommand(sql, _connection, _transaction);
            cmd.Parameters.AddWithValue("@TransactionId", transactionId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapTransaction(reader);
            }

            return null;
        }

        // Get all transactions for an investment
        public IReadOnlyList<Transaction> GetByInvestmentId(Guid investmentId)
        {
            const string sql = @"
        SELECT * FROM Transactions
        WHERE InvestmentId = @InvestmentId AND IsDeleted = 0
        ORDER BY TransactionDate ASC"; // maintain chronological order

            var result = new List<Transaction>();

            using var cmd = new SqlCommand(sql, _connection, _transaction);
            cmd.Parameters.AddWithValue("@InvestmentId", investmentId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapTransaction(reader));
            }

            return result;
        }

        private static Transaction MapTransaction(SqlDataReader reader)
        {
            return new Transaction(
                reader.GetGuid(reader.GetOrdinal("TransactionId")),
                reader.GetGuid(reader.GetOrdinal("InvestmentId")),
                TransactionType.From(reader.GetString(reader.GetOrdinal("TransactionType"))),
                reader.GetDecimal(reader.GetOrdinal("Units")),
                reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("TransactionDate"))),
                reader.GetGuid(reader.GetOrdinal("CreatedByUserId"))
            );
        }
    }
}
