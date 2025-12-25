using IPMS.Commands;

namespace IPMS.Services.Investments
{
    public interface IUpdateInvestmentService
    {
        void Execute(UpdateInvestmentCommand command, Guid userId);
    }
}
