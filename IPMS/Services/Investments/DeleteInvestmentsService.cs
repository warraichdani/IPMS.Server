using IPMS.Commands;
using IPMS.Core;
using IPMS.Core.Repositories;

namespace IPMS.Services.Investments
{
    public sealed class DeleteInvestmentsService : IDeleteInvestmentsService
    {
        private readonly IInvestmentRepository _investmentRepo;
        private readonly IUnitOfWork _uow;

        public DeleteInvestmentsService(
            IInvestmentRepository investmentRepo,
            IUnitOfWork uow)
        {
            _investmentRepo = investmentRepo;
            _uow = uow;
        }

        public void Execute(DeleteInvestmentsCommand command)
        {
            if (command.InvestmentIds == null || command.InvestmentIds.Count == 0)
                throw new InvalidOperationException("No investments selected.");

            foreach (var investmentId in command.InvestmentIds)
            {
                var investment = _investmentRepo.GetById(investmentId)
                    ?? throw new InvalidOperationException($"Investment not found: {investmentId}");

                if (investment.UserId != command.UserId)
                    throw new UnauthorizedAccessException();

                investment.SoftDelete(command.UserId);
                _investmentRepo.Update(investment);
            }

            _uow.Commit();
        }
    }

}
