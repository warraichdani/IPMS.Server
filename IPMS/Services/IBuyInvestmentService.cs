using IPMS.Commands;
using IPMS.Models.DTOs;

namespace IPMS.Services
{
    public interface IBuyInvestmentService
    {
        BuyInvestmentResponse Execute(BuyInvestmentCommand command);
    }
}
