
namespace IPMS.Core;

public interface IUnitOfWork : IDisposable
{
    void Commit();
    void Rollback();
}
