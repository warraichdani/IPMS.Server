using IPMS.Commands;
using IPMS.DTOs;

namespace IPMS.Services
{
    public interface IBuyInvestmentService
    {
        BuyInvestmentResponse Execute(BuyInvestmentCommand command);
    }
}
