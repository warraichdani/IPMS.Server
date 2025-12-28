using IPMS.Commands;
using IPMS.Core;
using IPMS.Core.Application.Activity;
using IPMS.Core.Configs;
using IPMS.Core.Entities;
using IPMS.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Services.Investments
{
    public class CreateInvestmentService : ICreateInvestmentService
    {
        private readonly IInvestmentRepository _investmentRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IPriceHistoryRepository _priceRepo;
        private readonly IUnitOfWork _uow;
        private readonly IActivityLogger _activity;

        public CreateInvestmentService(
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

        public Guid Execute(CreateInvestmentCommand cmd, Guid userId)
        {
            var investment = Investment.Create(
                userId,
                cmd.InvestmentName,
                InvestmentType.From(cmd.InvestmentType),
                cmd.InitialAmount,
                1,
                cmd.PurchaseDate,
                InvestmentStatus.From(cmd.Status),
                cmd.Broker,
                cmd.Notes
            );

            _investmentRepo.Add(investment);
            foreach (var tx in investment.Transactions)
                _transactionRepo.Add(tx);

            _investmentRepo.Update(investment);

            _priceRepo.Add(new PriceHistory(
                investment.InvestmentId,
                cmd.PurchaseDate,
                cmd.InitialUnitPrice));

            _uow.Commit();

            _activity.LogAsync(new ActivityEntry(
                ActorUserId: userId,
                Action: "NEW_INVESTMENT",
                EntityType: "Investment",
                EntityId: investment.InvestmentId.ToString(),
                Summary: $"User added a new investment {investment.InvestmentName} with inital amount of {cmd.InitialAmount}",
                Details: new { investment.TotalUnits, investment.CurrentUnitPrice },
                IPAddress: string.Empty,
                OccurredAt: DateTime.UtcNow
            ));

            return investment.InvestmentId;
        }
    }
}
