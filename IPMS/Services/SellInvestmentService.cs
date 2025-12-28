using IPMS.Commands;
using IPMS.Core;
using IPMS.Core.Application.Activity;
using IPMS.Core.Entities;
using IPMS.Core.Repositories;
using IPMS.Models.DTOs;
namespace IPMS.Services
{
    public sealed class SellInvestmentService : ISellInvestmentService
    {
        private readonly IInvestmentRepository _investmentRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IPriceHistoryRepository _priceRepo;
        private readonly IUnitOfWork _uow;
        private readonly IActivityLogger _activity;

        public SellInvestmentService(
            IInvestmentRepository investmentRepo,
            ITransactionRepository transactionRepo,
            IPriceHistoryRepository priceRepo,
            IUnitOfWork uow,
            IActivityLogger activity)
        {
            _investmentRepo = investmentRepo;
            _transactionRepo = transactionRepo;
            _priceRepo = priceRepo;
            _uow = uow;
            _activity = activity;
        }

        public SellInvestmentResponse Execute(SellInvestmentCommand cmd)
        {
            var investment = _investmentRepo.GetById(cmd.InvestmentId)
                ?? throw new InvalidOperationException("Investment not found.");

            var transaction = investment.Sell(cmd.Amount, investment.CurrentUnitPrice, cmd.Date, cmd.UserId);
            investment.UpdateLastTransaction(_transactionRepo.Add(transaction));
            _investmentRepo.Update(investment);

            _priceRepo.Add(new PriceHistory(
                cmd.InvestmentId,
                cmd.Date,
                investment.CurrentUnitPrice));

            _uow.Commit();

            _activity.LogAsync(new ActivityEntry(
                ActorUserId: cmd.UserId,
                Action: "Sell_INVESTMENT",
                EntityType: "Transaction",
                EntityId: cmd.InvestmentId.ToString(),
                Summary: $"User sold units of amount {cmd.Amount} for his investment {investment.InvestmentName}",
                Details: new { investment.TotalUnits, investment.CurrentUnitPrice },
                IPAddress: string.Empty,
                OccurredAt: DateTime.UtcNow
            ));

            return new SellInvestmentResponse(
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
