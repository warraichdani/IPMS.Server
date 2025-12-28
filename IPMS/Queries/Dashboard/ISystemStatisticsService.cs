using IPMS.Core.Application.DTOs;
using IPMS.Models.DTOs.Dashboard;

namespace IPMS.Queries.Dashboard
{
    public interface ISystemStatisticsService
    {
        Task<SystemStatisticsDto> GetAsync();
    }
}
