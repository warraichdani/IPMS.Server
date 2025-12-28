using IPMS.Core.Application.DTOs;

namespace IPMS.Core.Application.Repositories
{
    public interface ISystemStatisticsRepository
    {
        Task<SystemStatisticsDto> GetAsync();
    }
}
