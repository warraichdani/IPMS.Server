using IPMS.Commands;
using IPMS.Core;
using IPMS.Core.Application.Activity;
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
        private readonly IActivityLogger _activity;

        public UpdatePriceService(
            IInvestmentRepository investmentRepo,
            IPriceHistoryRepository priceRepo,
            IUnitOfWork uow,
            IActivityLogger activity)
        {
            _investmentRepo = investmentRepo;
            _priceRepo = priceRepo;
            _uow = uow;
            _activity = activity;
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

            _activity.LogAsync(new ActivityEntry(
                ActorUserId: cmd.UserId,
                Action: "Update_INVESTMENT",
                EntityType: "Investment",
                EntityId: cmd.InvestmentId.ToString(),
                Summary: $"User Updated the investment amount to {cmd.Amount} for his investment {investment.InvestmentName}",
                Details: new { investment.TotalUnits, investment.CurrentUnitPrice },
                IPAddress: string.Empty,
                OccurredAt: DateTime.UtcNow
            ));

            return new UpdatePriceResponse(
                cmd.InvestmentId,
                investment.CurrentUnitPrice,
                cmd.Date,
                investment.TotalUnits * investment.CurrentUnitPrice
            );
        }
    }

}
