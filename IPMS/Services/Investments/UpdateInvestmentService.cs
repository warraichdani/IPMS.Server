using IPMS.Commands;
using IPMS.Core;
using IPMS.Core.Configs;
using IPMS.Core.Repositories;

namespace IPMS.Services.Investments
{
    internal sealed class UpdateInvestmentService : IUpdateInvestmentService
    {
        private readonly IInvestmentRepository _investmentRepo;
        private readonly IUnitOfWork _uow;

        public UpdateInvestmentService(
            IInvestmentRepository investmentRepo,
            IUnitOfWork uow)
        {
            _investmentRepo = investmentRepo;
            _uow = uow;
        }

        public void Execute(UpdateInvestmentCommand cmd, Guid userId)
        {
            var investment = _investmentRepo.GetById(cmd.InvestmentId)
                ?? throw new InvalidOperationException("Investment not found.");

            if (investment.UserId != userId)
                throw new UnauthorizedAccessException("Access denied.");

            investment.UpdateDetails(
                cmd.InvestmentName,
                InvestmentType.From(cmd.InvestmentType),
                cmd.PurchaseDate,
                cmd.Broker,
                cmd.Notes);

            _investmentRepo.Update(investment);
            _uow.Commit();
        }
    }
}
