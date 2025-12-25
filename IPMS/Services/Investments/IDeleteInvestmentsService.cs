using IPMS.Commands;

namespace IPMS.Services.Investments
{
    public interface IDeleteInvestmentsService
    {
        void Execute(DeleteInvestmentsCommand command);
    }
}
