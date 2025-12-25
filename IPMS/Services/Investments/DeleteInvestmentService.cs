using IPMS.Core;
using IPMS.Core.Repositories;

namespace IPMS.Services.Investments
{
    public sealed class DeleteInvestmentService : IDeleteInvestmentService
    {
        private readonly IInvestmentRepository _investmentRepo;
        private readonly IUnitOfWork _uow;

        public DeleteInvestmentService(
            IInvestmentRepository investmentRepo,
            IUnitOfWork uow)
        {
            _investmentRepo = investmentRepo;
            _uow = uow;
        }

        public void Execute(Guid investmentId, Guid userId)
        {
            var investment = _investmentRepo.GetById(investmentId)
                ?? throw new InvalidOperationException("Investment not found.");

            if (investment.UserId != userId)
                throw new UnauthorizedAccessException();

            investment.SoftDelete(userId);

            _investmentRepo.Update(investment);
            _uow.Commit();
        }
    }

}
