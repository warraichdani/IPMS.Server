using IPMS.Models.DTOs;

namespace IPMS.Queries.Performance;

public interface IPerformanceQuery
{
    IReadOnlyList<PerformancePointDto> GetLast12Months(Guid investmentId, Guid userId);
}
