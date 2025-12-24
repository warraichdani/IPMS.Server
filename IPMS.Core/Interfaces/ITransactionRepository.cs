using IPMS.Core.Entities;

namespace IPMS.Core.Repositories;

public interface ITransactionRepository
{
    IReadOnlyList<Transaction> GetByInvestmentId(Guid investmentId);

    void Add(Transaction transaction);

    Transaction? GetById(Guid transactionId);
}
