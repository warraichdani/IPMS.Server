using IPMS.Core.Entities;

namespace IPMS.Core.Repositories;

public interface IInvestmentRepository
{
    Investment? GetById(Guid investmentId);

    IReadOnlyList<Investment> GetByUserId(Guid userId);

    void Add(Investment investment);

    void Update(Investment investment);

    bool Exists(Guid investmentId);
}
