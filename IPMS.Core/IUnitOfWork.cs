
using Microsoft.Data.SqlClient;

namespace IPMS.Core;

public interface IUnitOfWork : IDisposable
{
    SqlConnection Connection { get; }
    SqlTransaction Transaction { get; }

    void Commit();
    void Rollback();
}

