using IPMS.Commands;
using IPMS.Models.DTOs;
namespace IPMS.Services
{
    public interface IUpdatePriceService
    {
        UpdatePriceResponse Execute(UpdatePriceCommand cmd);
    }
}
