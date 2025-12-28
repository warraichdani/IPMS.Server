
using IPMS.Models.DTOs.Activity;

namespace IPMS.Queries.Activity
{
    public interface IRecentActivityQuery
    {
        Task<IReadOnlyList<RecentActivityDto>> GetRecent(int limit = 10);
    }
}
