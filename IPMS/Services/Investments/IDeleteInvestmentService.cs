
namespace IPMS.Services.Investments
{
    public interface IDeleteInvestmentService
    {
        void Execute(Guid investmentId, Guid userId);
    }
}
