using IPMS.Core.Application.DTOs;

namespace IPMS.Services.DashBoard
{
    public interface ISystemStatisticsService
    {
        Task<SystemStatisticsDto> GetAsync();
    }
}
