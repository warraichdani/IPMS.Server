using IPMS.Commands;
using IPMS.Core;
using IPMS.Core.Entities;
using IPMS.Core.Repositories;
using IPMS.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Services
{
    public sealed class UpdatePriceService : IUpdatePriceService
    {
        private readonly IInvestmentRepository _investmentRepo;
        private readonly IPriceHistoryRepository _priceRepo;
        private readonly IUnitOfWork _uow;

        public UpdatePriceService(
            IInvestmentRepository investmentRepo,
            IPriceHistoryRepository priceRepo,
            IUnitOfWork uow)
        {
            _investmentRepo = investmentRepo;
            _priceRepo = priceRepo;
            _uow = uow;
        }

        public UpdatePriceResponse Execute(UpdatePriceCommand cmd)
        {
            var investment = _investmentRepo.GetById(cmd.InvestmentId)
                ?? throw new InvalidOperationException("Investment not found.");

            if (investment.UserId != cmd.UserId)
                throw new InvalidOperationException("Unauthorized investment access.");

            investment.UpdateCurrentPrice(cmd.Amount);

            _investmentRepo.Update(investment);

            _priceRepo.Add(new PriceHistory(
                cmd.InvestmentId,
                cmd.Date,
                investment.CurrentUnitPrice));

            _uow.Commit();

            return new UpdatePriceResponse(
                cmd.InvestmentId,
                investment.CurrentUnitPrice,
                cmd.Date,
                investment.TotalUnits * investment.CurrentUnitPrice
            );
        }
    }

}
