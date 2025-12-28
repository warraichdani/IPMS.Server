using IPMS.Commands;
using IPMS.Core;
using IPMS.Core.Application.Activity;
using IPMS.Core.Entities;
using IPMS.Core.Repositories;
using System.Diagnostics;

namespace IPMS.Services.Investments
{
    public sealed class DeleteInvestmentsService : IDeleteInvestmentsService
    {
        private readonly IInvestmentRepository _investmentRepo;
        private readonly IUnitOfWork _uow;
        private readonly IActivityLogger _activity;

        public DeleteInvestmentsService(
            IInvestmentRepository investmentRepo,
            IUnitOfWork uow,
            IActivityLogger activity)
        {
            _investmentRepo = investmentRepo;
            _uow = uow;
            _activity = activity;
        }

        public async Task Execute(DeleteInvestmentsCommand command)
        {
            if (command.InvestmentIds == null || command.InvestmentIds.Count == 0)
                throw new InvalidOperationException("No investments selected.");
            
            List<ActivityEntry> list = new List<ActivityEntry>();

            foreach (var investmentId in command.InvestmentIds)
            {
                var investment = _investmentRepo.GetById(investmentId)
                    ?? throw new InvalidOperationException($"Investment not found: {investmentId}");

                if (investment.UserId != command.UserId)
                    throw new UnauthorizedAccessException();

                investment.SoftDelete(command.UserId);
                _investmentRepo.Update(investment);

                list.Add(new ActivityEntry(
                ActorUserId: command.UserId,
                Action: "Deleted_INVESTMENT",
                EntityType: "Investment",
                EntityId: investmentId.ToString(),
                Summary: $"User has deleted investment {investment.InvestmentName} of current value: {investment.CurrentValue}",
                Details: new { investment.TotalUnits, investment.CurrentUnitPrice },
                IPAddress: string.Empty,
                OccurredAt: DateTime.UtcNow
            ));
            }

            _uow.Commit();

            _activity.LogAsync(list);
        }
    }

}
