using IPMS.Core.Application.DTOs;
using IPMS.Core.Application.Repositories;

namespace IPMS.Services.DashBoard
{
    public sealed class SystemStatisticsService : ISystemStatisticsService
    {
        private readonly ISystemStatisticsRepository _repo;

        public SystemStatisticsService(ISystemStatisticsRepository repo)
        {
            _repo = repo;
        }

        public async Task<SystemStatisticsDto> GetAsync()
        {
            return await _repo.GetAsync();
        }
    }
}
