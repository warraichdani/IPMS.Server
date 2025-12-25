using IPMS.Commands;
using IPMS.Core;
using IPMS.Core.Configs;
using IPMS.Core.Entities;
using IPMS.Core.Repositories;
using System;
using System.Collections.Generic;
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

        public CreateInvestmentService(
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

            return investment.InvestmentId;
        }
    }
}
