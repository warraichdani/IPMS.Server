using IPMS.Commands;
using IPMS.DTOs;
namespace IPMS.Services
{
    public interface IUpdatePriceService
    {
        UpdatePriceResponse Execute(UpdatePriceCommand cmd);
    }
}
