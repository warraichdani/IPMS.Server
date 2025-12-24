using IPMS.Commands;
using IPMS.Core;
using IPMS.Core.Entities;
using IPMS.Core.Repositories;
using IPMS.DTOs;

namespace IPMS.Services
{
    public sealed class BuyInvestmentService : IBuyInvestmentService
    {
        private readonly IInvestmentRepository _investmentRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IPriceHistoryRepository _priceRepo;
        private readonly IUnitOfWork _uow;

        public BuyInvestmentService(
            IInvestmentRepository investmentRepo,
            ITransactionRepository transactionRepo,
            IPriceHistoryRepository priceRepo,
            IUnitOfWork uow)
        {
            _investmentRepo = investmentRepo;
            _transactionRepo = transactionRepo;
            _priceRepo = priceRepo;
            _uow = uow;
        }

        public BuyInvestmentResponse Execute(BuyInvestmentCommand cmd)
        {
            var investment = _investmentRepo.GetById(cmd.InvestmentId)
                ?? throw new InvalidOperationException("Investment not found.");

            investment.Buy(cmd.Amount, cmd.UnitPrice, cmd.Date, cmd.UserId);

            _investmentRepo.Update(investment);

            var transaction = investment.GetLastTransaction(_transactionRepo);
            _transactionRepo.Add(transaction);

            _priceRepo.Add(new PriceHistory(
                cmd.InvestmentId,
                cmd.Date,
                cmd.UnitPrice));

            _uow.Commit();

            return new BuyInvestmentResponse(
                transaction.TransactionId,
                transaction.InvestmentId,
                transaction.Units,
                transaction.UnitPrice,
                transaction.TransactionDate,
                investment.TotalUnits,
                investment.CostBasis
            );
        }
    }

}
