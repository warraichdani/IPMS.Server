using IPMS.Commands;

namespace IPMS.Services.Investments
{
    public interface IDeleteInvestmentsService
    {
        Task Execute(DeleteInvestmentsCommand command);
    }
}
