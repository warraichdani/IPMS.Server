using IPMS.Core;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Infrastructure
{
    public sealed class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly SqlConnection _connection;
        private SqlTransaction _transaction;

        public SqlConnection Connection => _connection;
        public SqlTransaction Transaction => _transaction;

        public UnitOfWork(IConfiguration config)
        {
            _connection = new SqlConnection(
                config.GetConnectionString("DefaultConnection"));
            _connection.Open();
            _transaction = _connection.BeginTransaction();
        }

        public void Commit()
        {
            _transaction.Commit();
            Dispose();
        }

        public void Rollback()
        {
            _transaction.Rollback();
            Dispose();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }
    }
}
